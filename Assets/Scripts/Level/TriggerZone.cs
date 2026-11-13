using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//this script was made following this tutorial https://www.youtube.com/watch?v=1BdR5d1JTEI

public class TriggerZone : MonoBehaviour
{
    public bool oneShot = false;
    private bool alreadyEntered = false;
    private bool alreadyExited = false;

    public string collisionTag;

    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.tag);


        if (alreadyEntered) { return; }

        

        if (!string.IsNullOrEmpty(collisionTag) && !collision.CompareTag(collisionTag))
        return;

        onTriggerEnter?.Invoke();

        if(oneShot){ alreadyEntered = true; }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (alreadyExited) { return; }

        if (!string.IsNullOrEmpty(collisionTag) && !collision.CompareTag(collisionTag))
        return;

        onTriggerExit?.Invoke();

        if (oneShot) { alreadyExited = true;}

    }
}
