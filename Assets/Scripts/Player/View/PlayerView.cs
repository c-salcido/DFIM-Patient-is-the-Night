using UnityEngine;

// VIEW - how the player looks: Animator parameters, particles and the sprite.
// It never moves the Rigidbody and never changes the model. It only:
//   - reads the model every frame for "looping" animation states (running, falling, wall sliding)
//   - listens to model events for one-shot effects (jump, land, hit, death)
[DisallowMultipleComponent]
public class PlayerView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Trail that follows the player when jumping (child 'SmokeTrail').")]
    [SerializeField] private ParticleSystem jumpTrailParticles;
    [Tooltip("Puff of dust on take-off and landing (child 'SmokeGround').")]
    [SerializeField] private ParticleSystem landingDustParticles;

    // Hashing the parameter names once is faster than passing strings every frame.
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
    private static readonly int JumpUpParam = Animator.StringToHash("JumpUp");
    private static readonly int IsDoubleJumpingParam = Animator.StringToHash("IsDoubleJumping");
    private static readonly int IsWallSlidingParam = Animator.StringToHash("IsWallSliding");
    private static readonly int IsDashingParam = Animator.StringToHash("IsDashing");
    private static readonly int IsAttackingParam = Animator.StringToHash("IsAttacking");
    private static readonly int HitParam = Animator.StringToHash("Hit");
    private static readonly int IsDeadParam = Animator.StringToHash("IsDead");

    private PlayerModel model;

    // Fills the references automatically when the component is added (or Reset) in the editor.
    private void Reset()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        jumpTrailParticles = FindChildParticles("SmokeTrail");
        landingDustParticles = FindChildParticles("SmokeGround");
    }

    public void Bind(PlayerModel newModel)
    {
        Unbind();
        model = newModel;

        model.Jumped += HandleJumped;
        model.AirJumped += HandleAirJumped;
        model.WallJumped += HandleWallJumped;
        model.Landed += HandleLanded;
        model.DashStarted += HandleDashStarted;
        model.MeleeAttacked += HandleMeleeAttacked;
        model.Damaged += HandleDamaged;
        model.Died += HandleDied;
        model.Respawned += HandleRespawned;
    }

    private void Unbind()
    {
        if (model == null)
            return;

        model.Jumped -= HandleJumped;
        model.AirJumped -= HandleAirJumped;
        model.WallJumped -= HandleWallJumped;
        model.Landed -= HandleLanded;
        model.DashStarted -= HandleDashStarted;
        model.MeleeAttacked -= HandleMeleeAttacked;
        model.Damaged -= HandleDamaged;
        model.Died -= HandleDied;
        model.Respawned -= HandleRespawned;
        model = null;
    }

    private void OnDestroy() => Unbind();

    private void Update()
    {
        if (model == null || animator == null)
            return;

        animator.SetFloat(SpeedParam, Mathf.Abs(model.HorizontalInput));
        animator.SetBool(IsJumpingParam, !model.IsGrounded);
        animator.SetBool(IsWallSlidingParam, model.IsWallSliding);

        // The wall-slide art faces away from the wall. The body keeps facing the wall
        // (so the wall check stays on it), so we only mirror the sprite while sliding.
        if (spriteRenderer != null)
            spriteRenderer.flipX = model.IsWallSliding;
    }

    // ---------------- Model event handlers ----------------

    private void HandleJumped()
    {
        animator.SetBool(IsJumpingParam, true);
        animator.SetBool(JumpUpParam, true);
        Play(landingDustParticles);
        Play(jumpTrailParticles);
    }

    private void HandleAirJumped() => animator.SetBool(IsDoubleJumpingParam, true);

    private void HandleWallJumped()
    {
        animator.SetBool(IsJumpingParam, true);
        animator.SetBool(JumpUpParam, true);
    }

    private void HandleLanded()
    {
        animator.SetBool(IsJumpingParam, false);
        if (!model.IsWallSliding && !model.IsDashing)
            Play(landingDustParticles);
    }

    private void HandleDashStarted() => animator.SetBool(IsDashingParam, true);
    private void HandleMeleeAttacked() => animator.SetBool(IsAttackingParam, true);
    private void HandleDamaged(float amount) => animator.SetBool(HitParam, true);
    private void HandleDied() => animator.SetBool(IsDeadParam, true);

    private void HandleRespawned()
    {
        animator.SetBool(IsDeadParam, false);
        animator.SetBool(HitParam, false);
        animator.SetBool(IsJumpingParam, false);
    }

    // ---------------- Helpers ----------------

    private static void Play(ParticleSystem particles)
    {
        if (particles != null)
            particles.Play();
    }

    private ParticleSystem FindChildParticles(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }
}