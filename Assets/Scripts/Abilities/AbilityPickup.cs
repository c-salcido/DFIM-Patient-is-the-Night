using UnityEngine;
using UnityEngine.Events;

// Put this on an object in the level to unlock an ability (e.g. DoubleJump) when the player touches it.
[RequireComponent(typeof(Collider2D))]
public class AbilityPickup : MonoBehaviour
{
    [SerializeField] private PlayerAbility ability = PlayerAbility.DoubleJump;
    [Tooltip("Hook up a dialogue or sound here, e.g. DialogueInstance.TriggerDialogue to explain the new move.")]
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    // Runs when the component is added in the editor, so the collider is set up correctly.
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        // If the player already has it (e.g. came back to this room after respawning)
        // the pickup just disappears without replaying the event.
        if (player.UnlockAbility(ability))
            onCollected.Invoke();

        Destroy(gameObject);
    }
}