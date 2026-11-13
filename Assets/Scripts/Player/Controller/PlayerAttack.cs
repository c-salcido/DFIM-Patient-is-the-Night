using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Part of the player CONTROLLER: performs attacks when PlayerController tells it to.
// It doesn't read input or play animations itself any more - the controller reads the keys,
// and the view plays the attack animation when the model says an attack happened.
[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [FormerlySerializedAs("dmgValue")]
    [SerializeField] private float damage = 4f;
    [SerializeField] private float meleeRadius = 0.9f;
    [SerializeField] private float meleeCooldown = 0.25f;
    [SerializeField] private Transform attackCheck;
    [SerializeField] private GameObject throwableObject;
    [Tooltip("Camera with a CameraFollow component to shake on hit (optional).")]
    [SerializeField] private GameObject cam;

    private bool canAttack = true;

    // Returns true if the attack started, so the controller can tell the model (which triggers the animation).
    public bool TryStartMelee()
    {
        if (!canAttack)
            return false;

        canAttack = false;
        StartCoroutine(MeleeCooldown());
        return true;
    }

    public void ThrowWeapon()
    {
        if (throwableObject == null)
            return;

        float facing = Mathf.Sign(transform.localScale.x);
        Vector3 spawnPosition = transform.position + new Vector3(facing * 0.5f, -0.2f);
        GameObject weapon = Instantiate(throwableObject, spawnPosition, Quaternion.identity);
        weapon.name = "ThrowableWeapon";
        weapon.GetComponent<ThrowableWeapon>().direction = new Vector2(facing, 0f);
    }

    // Called by an Animation Event on the attack animation clip, at the frame the hit should land.
    // (Name kept as-is because the animation clip calls it by name.)
    public void DoDashDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCheck.position, meleeRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            // The sign tells the enemy which way to be knocked back.
            float direction = hit.transform.position.x < transform.position.x ? -1f : 1f;
            hit.gameObject.SendMessage("ApplyDamage", damage * direction, SendMessageOptions.DontRequireReceiver);
            ShakeCamera();
        }
    }

    private void ShakeCamera()
    {
        if (cam != null && cam.TryGetComponent(out CameraFollow follow))
            follow.ShakeCamera();
    }

    private IEnumerator MeleeCooldown()
    {
        yield return new WaitForSeconds(meleeCooldown);
        canAttack = true;
    }
}