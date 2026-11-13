using UnityEngine;

// Pit / out-of-bounds trigger. Kills the player (who then respawns where they entered the room)
// and deletes anything else that falls in.
// It used to reload the scene directly, but the player survives scene loads (DontDestroy),
// so it would land back inside the kill zone and reload forever.
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        PlayerController player = col.GetComponentInParent<PlayerController>();
        if (player != null)
            player.Kill();
        else
            Destroy(col.gameObject);
    }
}
