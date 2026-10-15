using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    public int ladderLength;
    private Collider2D _collider;

    private float vertical;
    private float speed = 8f;
    private bool _playerOnLadder;
    private bool _playerClimbing;

    private Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        _collider =GetComponent<Collider2D>();   

       
    }


    private void SetPlayerOnLadder(Collision2D other, bool value)
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

    }

    private void OnTriggerExit2D(Collider2D other)
    {

    }

    private void FixedUpdate()
    {
        if (_playerClimbing)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, vertical * speed);
        }
    }

    // Update is called once per frame
    void Update()
    {
        vertical = Input.GetAxis("Vertical");

         if(_playerOnLadder && Mathf.Abs(vertical) > 0f)
         {
            _playerClimbing = true;

         }
        
    }
}
