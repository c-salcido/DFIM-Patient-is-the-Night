// Abilities the player can have. Some start unlocked (set on the PlayerController),
// the rest are unlocked in the world with an AbilityPickup.
// The numbers are fixed so reordering the list never changes what's saved in prefabs/scenes.
public enum PlayerAbility
{
    DoubleJump = 0,
    WallJump = 1,
    Dash = 2
}