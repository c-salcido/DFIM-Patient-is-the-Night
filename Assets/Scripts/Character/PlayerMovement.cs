using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script was made using help from this tutorial: https://www.youtube.com/watch?v=zHSWG05byEc

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public InputManager InputManager;
    public PlayerMovementStats MoveStats;
    [SerializeField] private Collider2D bodyCol;
    [SerializeField] private Collider2D feetCol;

    private Rigidbody2D rb;


    //movement vars
    public float HorizontalVelocity {get; private set;}
    private bool isFacingRight;

    //collision vars
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;
    private RaycastHit2D wallHit;
    private RaycastHit2D lastWallHit;
    private bool isGrounded;
    private bool bumpedHead;
    private bool isTouchingWall;

    //jump vars
    public float VerticalVelocity { get; private set;}
    private bool isJumping;
    private bool isFastFalling;
    private bool isFalling;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int numberOfJumpsUsed;
    
    //apex vars
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;

    //jump buffer vars
    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;

    //coyote time vars
    private float coyoteTimer;

    //wall slide
    private bool isWallSliding;
    private bool isWallSlideFalling;

    //wall jump
    private bool useWallJumpMoveStats;
    private bool isWallJumping;
    private float wallJumpTime;
    private bool isWallJumpFastFalling;
    private bool isWallJumpFalling;
    private float wallJumpFastFallTime;
    private float wallJumpFastFallReleaseSpeed;

    private float wallJumpPostBufferTimer;

    private float wallJumpApexPoint;
    private float timePastWallJumpApexThreshold;
    private bool isPastWallJumpApexThreshold;



    //dash
    private bool isDashing;
    private bool isAirDashing;
    private float dashTimer;
    private float dashOnGroundTimer;
    private int numberOfDashesUsed;
    private Vector2 dashDirection;
    private bool isDashFastFalling;
    private float dashFastFallTime;
    private float dashFastFallReleaseSpeed;


    private void Awake()
    {
        isFacingRight = true;
        rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        CountTimers();
        JumpChecks();
        LandCheck();

    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        Fall();
        if (isGrounded)
        {
            Move(MoveStats.GroundAcceleration, MoveStats.GroundDecceleration, InputManager.Movement);
        }
        else
        {
            Move(MoveStats.AirAcceleration, MoveStats.AirDecceleration, InputManager.Movement);

        }
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        //clamp fall speed
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -MoveStats.MaxFallSpeed, 50f);
        rb.velocity = new Vector2(HorizontalVelocity, VerticalVelocity);

    }

#region movement

    private void Move( float acceleraction, float decceleration, Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold)
        {
            //check if she needs to turn
            TurnCheck(moveInput);
    
            float targetVelocity = 0f;
            if (InputManager.RunIsHeld)
            {
                targetVelocity = moveInput.x * MoveStats.MaxRunSpeed;
            }
            else { targetVelocity =  moveInput.x * MoveStats.MaxWalkSpeed;}


            HorizontalVelocity = Mathf.Lerp(HorizontalVelocity,targetVelocity, acceleraction * Time.fixedDeltaTime);

            
        }
        else if (Mathf.Abs(moveInput.x) <MoveStats.MoveThreshold)
        {
            HorizontalVelocity = Mathf.Lerp(HorizontalVelocity,0f, decceleration * Time.fixedDeltaTime);
            

        }
    
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x <0)
        {
            Turn(false);
        }
        else if (!isFacingRight && moveInput.x >0)
        {
            Turn(true);
        }

    }

    private void Turn(bool turnRight)
    {
        if(turnRight)
        {
            isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);

        }
        else
        {
            isFacingRight = false;
            transform.Rotate(0f, -180f, 0f);
        }


    }


#endregion

#region Land/Fall

private void LandCheck()
{
    if((isJumping || isFalling) && isGrounded && VerticalVelocity <= 0f)
    {
        isJumping = false;
        isFalling = false;
        isFastFalling = false;
        fastFallTime = 0f;
        isPastApexThreshold = false;
        numberOfJumpsUsed = 0;
        VerticalVelocity = Physics2D.gravity.y;
    }
}

private void Fall()
{
//normal gravity while falling
    if (!isGrounded && !isJumping)
    {
        if(!isFalling)
        {
            isFalling = true;
        }
        VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;

    }
}


#endregion
#region Jump

private void JumpChecks()
{

    //when we press jump button
    if (InputManager.JumpWasPressed)
    {
       
        jumpBufferTimer = MoveStats.JumpBufferTime;
        jumpReleasedDuringBuffer = false;
    }

    //when we release button
    if (InputManager.JumpWasReleased)
    {
        if (jumpBufferTimer > 0f)
        {
            jumpReleasedDuringBuffer = true;

        }
        if(isJumping && VerticalVelocity > 0f)
        {
            if(isPastApexThreshold)
            {
                isPastApexThreshold = false;
                isFastFalling = true;
                fastFallTime = MoveStats.TimeForUpwardsCancel;
                VerticalVelocity = 0f;
            }
            else 
            {
                isFastFalling = true;
                fastFallReleaseSpeed = VerticalVelocity;
            }
        }
    }

    //initiate jump with jump buffer
    if (jumpBufferTimer > 0f & isJumping && (isGrounded || coyoteTimer > 0f))
    {
        InitiateJump(1);

        if(jumpReleasedDuringBuffer)
        {
            isFastFalling = true;
            fastFallReleaseSpeed = VerticalVelocity;
        }

    }

    //double jump

    else if (jumpBufferTimer >0f && isJumping && numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
    {
        isFastFalling = false;
        InitiateJump(1);

    }
    //air jump after coyote time lapsed
    else if (jumpBufferTimer > 0f && isFalling && numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed - 1)
    {
        InitiateJump(2);
        isFastFalling = false;
    }
  

}

private void InitiateJump(int numberOfJumpsUsed)
{
    if (!isJumping)
    {
        isJumping = true;
    }
    jumpBufferTimer = 0f;
    numberOfJumpsUsed += numberOfJumpsUsed;
    VerticalVelocity = MoveStats.InitialJumpVelocity;
}

private void Jump()
{

    //apply gravity while jumping
    if (isJumping)
    {
        //check for head bump
        if(bumpedHead)
        {
            isFastFalling = true;
        }
        //gravity ascending
        if (VerticalVelocity >= 0f)
        {
            //apex controls
            apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, VerticalVelocity);
            if (apexPoint > MoveStats.ApexThreshold)
            {
                if(!isPastApexThreshold)
                {
                    isPastApexThreshold = true;
                    timePastApexThreshold = 0f;
                }
                if(isPastApexThreshold)
                {
                    timePastApexThreshold += Time.fixedDeltaTime;
                    if(timePastApexThreshold < MoveStats.ApexHangTime)
                    {
                        VerticalVelocity =0f;
                    }
                    else
                    {
                        VerticalVelocity = -0.01f;
                    }

                }
            }
            //gravity on ascending but not past apex threshold 
            else if (!isFastFalling)
            {
                VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
                if(isPastApexThreshold)
                {
                    isPastApexThreshold = false;
                }

            }

        }

        //gravity descending
        else if (!isFastFalling)
        {
            VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
        }
        else if (VerticalVelocity < 0f)
        {
            if (!isFalling)
            {
                isFalling = true;
            }
        }
    
    }

    //jump cut
    if (isFastFalling)
    {
        if(fastFallTime >= MoveStats.TimeForUpwardsCancel)
        {
            VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
        }
        else if (fastFallTime < MoveStats.TimeForUpwardsCancel)
        {
            VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime /MoveStats.TimeForUpwardsCancel));
        }
        fastFallTime += Time.fixedDeltaTime;
    }
    
    }
    

 private void CountTimers()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else{ coyoteTimer = MoveStats.JumpCoyoteTime;}
    }

#endregion

#region Collision Checks
    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetCol.bounds.center.x, feetCol.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetCol.bounds.size.x, MoveStats.GroundDetectionRayLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);
        if(groundHit.collider != null)
        {
            isGrounded =true;
        }
        else { isGrounded = false; }

       /* #region Debug Visualization
        //debug visualization
        if (MoveStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if(isGrounded)
            {
                rayColor = Color.green;
            }
            else { raycolor = Color.red;}
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x/2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x/2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x/2, boxCastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize, rayColor);
        }
        #endregion */



    }

    private void BumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(feetCol.bounds.center.x, bodyCol.bounds.max.y);
        Vector2 boxCastSize = new Vector2(feetCol.bounds.size.x * MoveStats.HeadWidth, MoveStats.HeadDetectionRayLength);

        headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.HeadDetectionRayLength);
        if(headHit.collider != null)
        {
            bumpedHead = true;
        }
        else { bumpedHead = false;}
    }


    private void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();

    }



#endregion

  
}
