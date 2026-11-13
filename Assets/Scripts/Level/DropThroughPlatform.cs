using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One-way platform the player can drop through by holding down (the PlayerController calls LetThrough).
// Only the player's colliders ignore the platform for a moment, so enemies standing on it don't fall too.
// originally made using this tutorial: https://www.youtube.com/watch?v=Lyeb7c0-R8c
[RequireComponent(typeof(Collider2D))]
public class DropThroughPlatform : MonoBehaviour
{
    [SerializeField] private float passThroughTime = 0.5f;

    private Collider2D platformCollider;

    public bool IsLettingThrough { get; private set; }

    private void Awake() => platformCollider = GetComponent<Collider2D>();

    public void LetThrough(IReadOnlyList<Collider2D> colliders)
    {
        if (!IsLettingThrough)
            StartCoroutine(PassThroughRoutine(colliders));
    }

    private IEnumerator PassThroughRoutine(IReadOnlyList<Collider2D> colliders)
    {
        IsLettingThrough = true;
        SetIgnored(colliders, true);
        yield return new WaitForSeconds(passThroughTime);
        SetIgnored(colliders, false);
        IsLettingThrough = false;
    }

    private void SetIgnored(IReadOnlyList<Collider2D> colliders, bool ignore)
    {
        foreach (Collider2D col in colliders)
        {
            if (col != null)
                Physics2D.IgnoreCollision(col, platformCollider, ignore);
        }
    }
}