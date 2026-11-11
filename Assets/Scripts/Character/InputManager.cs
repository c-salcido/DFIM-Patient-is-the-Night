using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //this was made with help from this tutorial: https://www.youtube.com/watch?v=K1xZ-rycYY8

    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool RunIsHeld;

    private float horizontal;


    // Update is called once per frame
    void Update()
    {
        Movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Movement != Vector2.zero)
        {
            RunIsHeld = true;
        }
        else {RunIsHeld = false;}

        JumpWasPressed = Input.GetButtonDown("Jump");

        if (JumpWasPressed)
        {
             Debug.Log("jump pressed");
            JumpIsHeld = true;
        }
        else {JumpIsHeld = false;}

        JumpWasReleased = Input.GetButtonUp("Jump");
       
        
    }


    private void FixedUpdate()
    {
        
    }
}
