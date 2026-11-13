using UnityEngine;

// A trigger volume that tells the player "you're next to a ladder".
// The actual climbing happens in PlayerController, so the ladder no longer
// reads input or changes the player's gravity (which used to fight with the dash).
[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    [SerializeField] private float climbSpeed = 8f;

    public float ClimbSpeed => climbSpeed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            player.EnterLadder(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            player.ExitLadder(this);
    }
}