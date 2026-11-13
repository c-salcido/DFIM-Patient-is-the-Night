using System;
using System.Collections.Generic;

// MODEL - the player's state and the rules about that state.
// Plain C# on purpose: no MonoBehaviour, Rigidbody, Animator or Input in here.
// The controller changes it through the methods below, and the view listens to the events
// (or reads the properties) to decide what to show.
public class PlayerModel
{
    // One-shot things that happened. The view uses these for animations, particles and sounds.
    public event Action Jumped;
    public event Action AirJumped;
    public event Action WallJumped;
    public event Action Landed;
    public event Action DashStarted;
    public event Action MeleeAttacked;
    public event Action<float> Damaged;
    public event Action<float, float> HealthChanged; // (current, max) - ready for a health bar
    public event Action Died;
    public event Action Respawned;
    public event Action<PlayerAbility> AbilityUnlocked;

    private readonly HashSet<PlayerAbility> abilities = new HashSet<PlayerAbility>();
    private readonly bool wallRefillsAirJumps;

    public PlayerModel(float maxHealth, int maxAirJumps, bool wallRefillsAirJumps, IEnumerable<PlayerAbility> startingAbilities)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
        MaxAirJumps = maxAirJumps;
        this.wallRefillsAirJumps = wallRefillsAirJumps;

        if (startingAbilities != null)
        {
            foreach (PlayerAbility ability in startingAbilities)
                abilities.Add(ability);
        }
    }

    // ---------------- State ----------------

    public float MaxHealth { get; }
    public float Health { get; private set; }
    public bool IsDead => Health <= 0f;
    public bool IsInvincible { get; set; }

    public bool IsGrounded { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsClimbing { get; set; }
    public bool FacingRight { get; set; } = true;
    public float HorizontalInput { get; set; }

    // Turned off by cutscenes/timeline (see PlayerController.SetControlEnabled).
    public bool ControlEnabled { get; set; } = true;

    // ---------------- Abilities ----------------

    public bool HasAbility(PlayerAbility ability) => abilities.Contains(ability);

    // Returns false if the player already had it, so pickups can tell the difference.
    public bool UnlockAbility(PlayerAbility ability)
    {
        if (!abilities.Add(ability))
            return false;

        AbilityUnlocked?.Invoke(ability);
        return true;
    }

    // ---------------- Jump rules ----------------

    public int MaxAirJumps { get; }
    public int AirJumpsUsed { get; private set; }

    // Without the DoubleJump ability there are no air jumps at all: one jump from the ground, that's it.
    public bool CanAirJump => HasAbility(PlayerAbility.DoubleJump) && AirJumpsUsed < MaxAirJumps;

    public void SetGrounded(bool grounded)
    {
        bool justLanded = grounded && !IsGrounded;
        IsGrounded = grounded;

        if (justLanded)
        {
            RefillAirJumps();
            Landed?.Invoke();
        }
    }

    public void RefillAirJumps() => AirJumpsUsed = 0;

    public void RegisterGroundJump()
    {
        IsGrounded = false;
        IsClimbing = false;
        Jumped?.Invoke();
    }

    public bool TryUseAirJump()
    {
        if (!CanAirJump)
            return false;

        AirJumpsUsed++;
        AirJumped?.Invoke();
        return true;
    }

    public void SetWallSliding(bool sliding)
    {
        // Grabbing a wall gives the double jump back (Hollow Knight style), if that option is on.
        if (sliding && !IsWallSliding && wallRefillsAirJumps)
            RefillAirJumps();

        IsWallSliding = sliding;
    }

    public void RegisterWallJump()
    {
        IsWallSliding = false;
        if (wallRefillsAirJumps)
            RefillAirJumps();
        WallJumped?.Invoke();
    }

    // ---------------- Dash / attack ----------------

    public void StartDash()
    {
        IsDashing = true;
        IsWallSliding = false;
        DashStarted?.Invoke();
    }

    public void EndDash() => IsDashing = false;

    public void NotifyMeleeAttack() => MeleeAttacked?.Invoke();

    // ---------------- Health ----------------

    // Returns true only if the damage actually landed (not invincible, not already dead).
    public bool TakeDamage(float amount)
    {
        if (IsDead || IsInvincible)
            return false;

        Health = Math.Max(0f, Health - amount);
        Damaged?.Invoke(amount);
        HealthChanged?.Invoke(Health, MaxHealth);

        if (IsDead)
            Died?.Invoke();

        return true;
    }

    public void Kill()
    {
        if (IsDead)
            return;

        Health = 0f;
        HealthChanged?.Invoke(Health, MaxHealth);
        Died?.Invoke();
    }

    // Abilities are deliberately kept: dying shouldn't take away what you unlocked.
    public void Respawn()
    {
        Health = MaxHealth;
        IsInvincible = false;
        IsDashing = false;
        IsWallSliding = false;
        IsClimbing = false;
        RefillAirJumps();
        HealthChanged?.Invoke(Health, MaxHealth);
        Respawned?.Invoke();
    }
}