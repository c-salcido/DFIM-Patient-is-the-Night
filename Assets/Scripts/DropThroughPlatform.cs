using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//this was made using this tutorial: https://www.youtube.com/watch?v=Lyeb7c0-R8c 

public class DropThroughPlatform : MonoBehaviour
{
    private Collider2D _collider;
    private bool _playerOnPlatform;
    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponent<Collider2D>();
        
    }

    private void SetPlayerOnPlatform(Collision2D other, bool value)
    {
        var player = other.gameObject.tag;
        if (player == "Player")
        {
            _playerOnPlatform = value;
        }
    }

private void OnCollisionEnter2D(Collision2D other)
{
    SetPlayerOnPlatform(other, true);
}

private void OnCollisionExit2D(Collision2D other)
{
    SetPlayerOnPlatform(other, true);
}

private IEnumerator EnableCollider()
{
    yield return new WaitForSeconds(0.5f);
    _collider.enabled = true;
}


// Update is called once per frame
void Update()
{
    if(_playerOnPlatform && Input.GetAxisRaw("Vertical")< 0 )
    {
        _collider.enabled = false;
        StartCoroutine(EnableCollider());

    }
        
}
}
