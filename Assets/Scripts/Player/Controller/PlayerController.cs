using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// CONTROLLER - turns input + physics into changes on the PlayerModel.
// It owns the physical body (Rigidbody2D, collider, facing direction) and asks the model
// whether an action is allowed ("can I air jump?"). It never touches the Animator or particles;
// PlayerView does that by watching the model.
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerView view;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [Tooltip("Layers that count as floor/walls. Trigger colliders are always ignored.")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float checkRadius = 0.2f;

    [Header("Input")]
    [SerializeField] private PlayerInputReader input = new PlayerInputReader();

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [Range(0, .3f)] [SerializeField] private float movementSmoothing = .05f;
    [SerializeField] private bool airControl = true;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 850f;
    [Tooltip("Double jump strength compared to the normal jump.")]
    [SerializeField] private float airJumpMultiplier = 0.83f;
    [Tooltip("How many extra jumps the DoubleJump ability gives.")]
    [SerializeField] private int maxAirJumps = 1;
    [Tooltip("You can still jump this long after walking off a ledge.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("A jump pressed this long before landing still happens.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Wall slide / wall jump")]
    [SerializeField] private float wallSlideSpeed = 5f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(1020f, 850f);
    [Tooltip("Horizontal input is ignored this long after a wall jump, so you get pushed away from the wall.")]
    [SerializeField] private float wallJumpControlLock = 0.15f;
    [Tooltip("Touching a wall gives the double jump back.")]
    [SerializeField] private bool wallRefillsAirJump = true;

    [Header("Dash / roll")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashCooldown = 0.5f;
    [Tooltip("Collider height while rolling, so you fit under low gaps.")]
    [Range(0.3f, 1f)] [SerializeField] private float rollHeightFactor = 0.5f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float knockbackForce = 400f;
    [SerializeField] private float hurtStunTime = 0.25f;
    [SerializeField] private float hurtInvincibleTime = 1f;

    [Header("Abilities unlocked from the start")]
    [SerializeField] private List<PlayerAbility> startingAbilities = new List<PlayerAbility> { PlayerAbility.WallJump, PlayerAbility.Dash };

    private const float GroundIgnoreAfterJump = 0.1f;
    private const float ClimbLockAfterJump = 0.25f;
    private const float WallStickSpeed = 2f;

    public PlayerModel Model { get; private set; }

    private Rigidbody2D rb;
    private CapsuleCollider2D bodyCollider;
    private Collider2D[] ownColliders;
    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private ContactFilter2D solidFilter;
    private float defaultGravity;
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private bool isRolled;

    private float xSmoothVelocity;
    private bool touchingWall;
    private float wallJumpDirection;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastWallTime = float.NegativeInfinity;
    private float jumpPressedTime = float.NegativeInfinity;
    private float ignoreGroundUntil;
    private float controlLockedUntil;
    private float climbLockedUntil;
    private float stunnedUntil;
    private bool dashRequested;
    private float dashEndTime;
    private float dashReadyTime;
    private Ladder currentLadder;
    private Vector3 respawnPosition;
    private bool respawnPending;

    private float FacingSign => Model.FacingRight ? 1f : -1f;
    private bool CanAct => Model.ControlEnabled && Time.time >= stunnedUntil;

    // ======================= Unity lifecycle =======================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        defaultGravity = rb.gravityScale;
        if (bodyCollider != null)
        {
            standingColliderSize = bodyCollider.size;
            standingColliderOffset = bodyCollider.offset;
        }

        // Only solid colliders on the ground layers count. Ignoring triggers is the fix for the
        // "flying" bug: the camera-room trigger volumes were being treated as floor.
        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(whatIsGround);
        solidFilter.useTriggers = false;

        Model = new PlayerModel(maxHealth, maxAirJumps, wallRefillsAirJump, startingAbilities);
        Model.FacingRight = transform.localScale.x >= 0f;
        Model.Died += HandleDied;

        if (view == null) view = GetComponent<PlayerView>();
        if (attack == null) attack = GetComponent<PlayerAttack>();
        if (view != null) view.Bind(Model);

        respawnPosition = transform.position;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;


        DialoguePresenter.DialogueStarted += HandleDialogueStarted;
        DialoguePresenter.DialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        DialoguePresenter.DialogueStarted -= HandleDialogueStarted;
        DialoguePresenter.DialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueStarted() => SetControlEnabled(false);
    private void HandleDialogueEnded() => SetControlEnabled(true);


    private void OnDestroy()
    {
        if (Model != null)
            Model.Died -= HandleDied;
    }

    // Input is read every frame (so no key press is missed); physics happens in FixedUpdate.
    private void Update()
    {
        if (Model.IsDead || !Model.ControlEnabled)
        {
            input.Clear();
            Model.HorizontalInput = 0f;
            return;
        }

        input.Read();
        Model.HorizontalInput = Time.time >= stunnedUntil ? input.Horizontal : 0f;

        if (input.JumpPressed) jumpPressedTime = Time.time;
        if (input.DashPressed) dashRequested = true;

        if (attack != null)
        {
            if (input.AttackPressed && attack.TryStartMelee())
                Model.NotifyMeleeAttack();
            if (input.ThrowPressed)
                attack.ThrowWeapon();
        }
    }

    private void FixedUpdate()
    {
        UpdateGroundCheck();
        UpdateWallCheck();
        UpdateRollCollider();

        if (!Model.IsDead)
        {
            if (Model.IsDashing)
            {
                UpdateDash();
            }
            else if (CanAct)
            {
                UpdateLadder();
                UpdateDropThrough();
                UpdateWallSlide();
                UpdateJump();
                UpdateHorizontalMovement();
                TryStartDash();
            }
        }

        dashRequested = false;
        rb.gravityScale = (Model.IsDashing || Model.IsClimbing) ? 0f : defaultGravity;
        LimitFallSpeed();
    }

    // ======================= Sensing =======================

    private void UpdateGroundCheck()
    {
        // Right after a jump the check circle still overlaps the floor for a couple of frames;
        // ignoring it stops that from counting as "landed" and refilling jumps.
        bool grounded = Time.time >= ignoreGroundUntil && IsSolidAt(groundCheck.position, false);

        if (grounded)
            lastGroundedTime = Time.time;

        Model.SetGrounded(grounded);
    }

    private void UpdateWallCheck()
    {
        touchingWall = !Model.IsGrounded && IsSolidAt(wallCheck.position, true);
    }

    // Is there a real (non-trigger, non-player) collider on a ground layer at this point?
    private bool IsSolidAt(Vector2 point, bool lookingForWall)
    {
        int count = Physics2D.OverlapCircle(point, checkRadius, solidFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapBuffer[i];
            if (IsOwnCollider(hit))
                continue;

            // One-way platforms: never a wall, and not floor while we're jumping up through them.
            if (hit.usedByEffector && (lookingForWall || rb.velocity.y > 0.1f))
                continue;

            if (hit.TryGetComponent(out DropThroughPlatform platform) && platform.IsLettingThrough)
                continue;

            return true;
        }
        return false;
    }

    private bool IsOwnCollider(Collider2D col) => col.attachedRigidbody == rb;

    // ======================= Jumping =======================

    private void UpdateJump()
    {
        bool jumpBuffered = Time.time - jumpPressedTime <= jumpBufferTime;
        if (!jumpBuffered)
            return;

        bool canGroundJump = Model.IsGrounded || Model.IsClimbing || Time.time - lastGroundedTime <= coyoteTime;
        bool canWallJump = Model.HasAbility(PlayerAbility.WallJump) && Time.time - lastWallTime <= coyoteTime;

        // Priority: ground > wall > air. The air jump only exists once DoubleJump is unlocked.
        if (canGroundJump)
            GroundJump();
        else if (canWallJump)
            WallJump();
        else if (Model.TryUseAirJump())
            AirJump();
        else
            return; // not allowed right now - keep the press buffered in case we land in the next few frames

        jumpPressedTime = float.NegativeInfinity;
    }

    private void GroundJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(new Vector2(0f, jumpForce));

        // Use up the ground jump so coyote time can't give a second one.
        lastGroundedTime = float.NegativeInfinity;
        ignoreGroundUntil = Time.time + GroundIgnoreAfterJump;
        climbLockedUntil = Time.time + ClimbLockAfterJump;
        Model.RegisterGroundJump();
    }

    private void AirJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(new Vector2(0f, jumpForce * airJumpMultiplier));
    }

    private void WallJump()
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y));
        SetFacing(wallJumpDirection > 0f);

        controlLockedUntil = Time.time + wallJumpControlLock;
        lastWallTime = float.NegativeInfinity;
        ignoreGroundUntil = Time.time + GroundIgnoreAfterJump;
        Model.RegisterWallJump();
    }

    // ======================= Walls =======================

    private void UpdateWallSlide()
    {
        bool hasWallJump = Model.HasAbility(PlayerAbility.WallJump);

        // Remember the last time we touched a wall so a wall jump pressed a moment later still works.
        if (hasWallJump && touchingWall)
        {
            lastWallTime = Time.time;
            wallJumpDirection = -FacingSign;
        }

        bool pressingAway = Model.HorizontalInput * FacingSign < -0.1f;
        bool shouldSlide = hasWallJump && touchingWall && !Model.IsClimbing && rb.velocity.y <= 0f && !pressingAway;

        if (shouldSlide)
        {
            Model.SetWallSliding(true);
            // Push gently into the wall and cap the fall speed.
            rb.velocity = new Vector2(FacingSign * WallStickSpeed, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
        }
        else if (Model.IsWallSliding)
        {
            Model.SetWallSliding(false);
        }
    }

    // ======================= Running =======================

    private void UpdateHorizontalMovement()
    {
        if (Model.IsWallSliding || Time.time < controlLockedUntil)
            return;
        if (!Model.IsGrounded && !airControl)
            return;

        float targetX = Model.HorizontalInput * moveSpeed;
        float newX = Mathf.SmoothDamp(rb.velocity.x, targetX, ref xSmoothVelocity, movementSmoothing);
        rb.velocity = new Vector2(newX, rb.velocity.y);

        if (Model.HorizontalInput > 0.01f)
            SetFacing(true);
        else if (Model.HorizontalInput < -0.01f)
            SetFacing(false);
    }

    // The facing direction is part of the physical body (it moves the wall/attack checks),
    // so the controller owns it, not the view.
    private void SetFacing(bool faceRight)
    {
        if (Model.FacingRight == faceRight)
            return;

        Model.FacingRight = faceRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private void LimitFallSpeed()
    {
        if (rb.velocity.y < -maxFallSpeed)
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
    }

    // ======================= Dash / roll =======================

    private void TryStartDash()
    {
        if (!dashRequested || !Model.HasAbility(PlayerAbility.Dash) || Time.time < dashReadyTime)
            return;

        // Dashing off a wall goes away from it.
        if (Model.IsWallSliding)
            SetFacing(wallJumpDirection > 0f);

        Model.IsClimbing = false;
        Model.StartDash();
        dashEndTime = Time.time + dashDuration;
        SetRolled(true);
    }

    private void UpdateDash()
    {
        bool hitWallInAir = touchingWall && !Model.IsGrounded;
        if (Time.time >= dashEndTime || hitWallInAir || !CanAct)
        {
            Model.EndDash();
            dashReadyTime = Time.time + dashCooldown;
            return;
        }

        rb.velocity = new Vector2(FacingSign * dashSpeed, 0f);
    }

    // Stay small after the roll until there's room to stand, so we never get pushed into a ceiling.
    private void UpdateRollCollider()
    {
        if (isRolled && !Model.IsDashing && CanStandUp())
            SetRolled(false);
    }

    // Shrinks the collider from the top so the feet stay on the floor.
    private void SetRolled(bool rolled)
    {
        if (bodyCollider == null || isRolled == rolled)
            return;

        isRolled = rolled;
        if (rolled)
        {
            float lostHeight = standingColliderSize.y * (1f - rollHeightFactor);
            bodyCollider.size = new Vector2(standingColliderSize.x, standingColliderSize.y * rollHeightFactor);
            bodyCollider.offset = new Vector2(standingColliderOffset.x, standingColliderOffset.y - lostHeight / 2f);
        }
        else
        {
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;
        }
    }

    private bool CanStandUp()
    {
        Vector2 center = transform.TransformPoint(standingColliderOffset);
        Vector3 lossy = transform.lossyScale;
        // Slightly smaller than the real collider so the floor and side walls don't count.
        Vector2 size = new Vector2(standingColliderSize.x * Mathf.Abs(lossy.x), standingColliderSize.y * Mathf.Abs(lossy.y)) * 0.9f;

        int count = Physics2D.OverlapCapsule(center, size, bodyCollider.direction, 0f, solidFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (!IsOwnCollider(overlapBuffer[i]) && !overlapBuffer[i].usedByEffector)
                return false;
        }
        return true;
    }

    // ======================= Ladders & platforms =======================

    public void EnterLadder(Ladder ladder) => currentLadder = ladder;

    public void ExitLadder(Ladder ladder)
    {
        if (currentLadder != ladder)
            return;

        currentLadder = null;
        Model.IsClimbing = false;
    }

    private void UpdateLadder()
    {
        if (currentLadder == null)
        {
            Model.IsClimbing = false;
            return;
        }
        if (Time.time < climbLockedUntil)
            return;

        // Pressing up/down grabs the ladder; once on it you hang there until you jump off or leave it.
        if (Mathf.Abs(input.Vertical) > 0.1f)
            Model.IsClimbing = true;

        if (Model.IsClimbing)
        {
            rb.velocity = new Vector2(rb.velocity.x, input.Vertical * currentLadder.ClimbSpeed);
            Model.RefillAirJumps();
        }
    }

    // Holding down while standing on a DropThroughPlatform falls through it.
    private void UpdateDropThrough()
    {
        if (!Model.IsGrounded || input.Vertical > -0.5f)
            return;

        int count = Physics2D.OverlapCircle(groundCheck.position, checkRadius, solidFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].TryGetComponent(out DropThroughPlatform platform))
                platform.LetThrough(ownColliders);
        }
    }

    // ======================= Public API (used by other scripts) =======================

    // Called by enemies and projectiles.
    public void ApplyDamage(float damage, Vector3 sourcePosition)
    {
        if (!Model.TakeDamage(damage))
            return;

        Vector2 knockbackDirection = ((Vector2)(transform.position - sourcePosition)).normalized;
        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce);

        if (Model.IsDead)
            return; // HandleDied takes over from here

        stunnedUntil = Time.time + hurtStunTime;
        StartCoroutine(InvincibilityRoutine());
    }

    // Instant death (kill zones, spikes...). Goes through the normal death + respawn.
    public void Kill() => Model.Kill();

    public bool UnlockAbility(PlayerAbility ability) => Model.UnlockAbility(ability);

    // For cutscenes / Timeline signals: stops the player from moving without disabling the object.
    public void SetControlEnabled(bool enabled)
    {
        Model.ControlEnabled = enabled;
        if (!enabled)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    // ======================= Death & respawn =======================

    private IEnumerator InvincibilityRoutine()
    {
        Model.IsInvincible = true;
        yield return new WaitForSeconds(hurtInvincibleTime);
        if (!Model.IsDead)
            Model.IsInvincible = false;
    }

    private void HandleDied() => StartCoroutine(DeathRoutine());

    private IEnumerator DeathRoutine()
    {
        Model.IsInvincible = true;
        if (Model.IsDashing)
            Model.EndDash();

        yield return new WaitForSeconds(0.4f);
        rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(1.1f);

        // The player survives scene loads (DontDestroy), so we reload the room and
        // put it back where it entered, instead of leaving a dead player in a fresh scene.
        respawnPending = true;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (respawnPending)
        {
            respawnPending = false;
            transform.position = respawnPosition;
            rb.velocity = Vector2.zero;
            stunnedUntil = 0f;
            currentLadder = null;
            Model.Respawn();
        }
        else
        {
            // Entering a new room: this is where we'll come back to if we die in it.
            respawnPosition = transform.position;
        }
    }

    // ======================= Editor helpers =======================

    [ContextMenu("Debug/Unlock Double Jump")]
    private void DebugUnlockDoubleJump()
    {
        if (Application.isPlaying)
            UnlockAbility(PlayerAbility.DoubleJump);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.cyan;
        if (wallCheck != null) Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    }
}