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
        WallJumpCheck();
        WallSlideCheck();

    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        Fall();
        WallSlide();
        WallJump();
        if (isGrounded)
        {
            Move(MoveStats.GroundAcceleration, MoveStats.GroundDecceleration, InputManager.Movement);
        }
        else
        {
            //wall jumping
            if(useWallJumpMoveStats)
            {
                Move(MoveStats.GroundAcceleration, MoveStats.GroundDecceleration, InputManager.Movement);
            }
            //airborne
            else
            {
                Move(MoveStats.AirAcceleration, MoveStats.AirDecceleration, InputManager.Movement);
            }
            

        }
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        if(!isDashing)
        {
            //clamp fall speed
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -MoveStats.MaxFallSpeed, 50f);

        }
        else
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity,-50f, 50f);

        }
        
        rb.velocity = new Vector2(HorizontalVelocity, VerticalVelocity);

    }

#region movement

    private void Move( float acceleraction, float decceleration, Vector2 moveInput)
    {
        if(!isDashing)
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
    if((isJumping || isFalling || isWallJumpFalling ||isWallJumping ||isWallSlideFalling || isWallSliding ||isDashFastFalling) && isGrounded && VerticalVelocity <= 0f)
    {
        ResetJumpValues();
        StopWallSlide();
        ResetWallJumpValues();
        ResetDashes();
        numberOfJumpsUsed = 0;
        VerticalVelocity = Physics2D.gravity.y;

        if (isDashFastFalling && isGrounded)
        {
            ResetDashValues();
            return;
        }
        ResetDashValues();
    }
}

private void Fall()
{
//normal gravity while falling
    if (!isGrounded && !isJumping && !isWallSliding&& /* isWallJumping &&*/ !isDashing && !isDashFastFalling)
    {
        if(!isFalling)
        {
            isFalling = true;
        }
        VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;

    }
}


#endregion
#region Wall Slide


private void WallSlideCheck()
{ 
    if(isTouchingWall && !isGrounded && !isDashing)
    {
        if (VerticalVelocity <0f && !isWallSliding)
        {
            ResetJumpValues();
            ResetWallJumpValues();
            ResetDashValues();
            if(MoveStats.ResetDashOnWallSlide)
            {
                ResetDashes();
            }

            isWallSlideFalling = false;
            isWallSliding = true;

            if (MoveStats.ResetJumpsOnWallSlide)
            {
                numberOfJumpsUsed = 0;
            }
        }
    }
    else if (isWallSliding && !isTouchingWall && !isGrounded && !isWallSlideFalling)
    {
        isWallSlideFalling = true;
        StopWallSlide();
    }
    else
    { 
        StopWallSlide();
    } 

}

private void StopWallSlide()
{ 
    if(isWallSliding)
    {
        numberOfJumpsUsed ++;
        isWallSliding = false;
    }
}
private void WallSlide()
{
    if(isWallSliding)
    {
        VerticalVelocity = Mathf.Lerp(VerticalVelocity, -MoveStats.WallSlideSpeed, MoveStats.WallSlideDeccelerationSpeed * Time.fixedDeltaTime);
    }

}

#endregion
#region Jump

private void ResetJumpValues()
{
    isJumping = false;
    isFalling = false;
    isFastFalling = false;
    fastFallTime = 0f;
    isPastApexThreshold = false;

}
private void JumpChecks()
{

    //when we press jump button
    if (InputManager.JumpWasPressed)
    {
        if(isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
        {
            return;
        }
        else if (isWallSliding ||(isTouchingWall&&!isGrounded))
        {
            return;
        }
       
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
    else if (jumpBufferTimer >0f && (isJumping|| isWallJumping|| isWallSlideFalling|| isAirDashing|| isDashFastFalling) && !isTouchingWall && numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
    {
        isFastFalling = false;
        InitiateJump(1);
        if(isDashFastFalling)
        {
            isDashFastFalling = false;
        }

    }
    //air jump after coyote time lapsed
    else if (jumpBufferTimer > 0f && isFalling && !isWallSlideFalling && numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed - 1)
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
    ResetWallJumpValues();
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
    



#endregion
#region Timers
 private void CountTimers()
    {
        //jump buffers
        jumpBufferTimer -= Time.deltaTime;

        //jump coyote time
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else{ coyoteTimer = MoveStats.JumpCoyoteTime;}

        //wall jump buffer tier
        if(!ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer -= Time.deltaTime;
        }
    }
#endregion
#region Wall jump

private void WallJumpCheck()
{
    if(ShouldApplyPostWallJumpBuffer())
    {
        wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;
    }
    //wall jump fast falling
    if(InputManager.JumpWasReleased && !isWallSliding && !isTouchingWall && isWallJumping)
    {
        if(VerticalVelocity >0f)
        {
            if(isPastWallJumpApexThreshold)
            {
                isPastWallJumpApexThreshold = false;
                isWallJumpFastFalling = true;
                wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel;
                VerticalVelocity = 0f;
            }
            else
            {
                isWallJumpFalling = true;
                wallJumpFastFallReleaseSpeed = VerticalVelocity;

            }
        }
    }
    //actual jump with post wall jump buffer time
    if (InputManager.JumpWasPressed && wallJumpPostBufferTimer >0f )
    {
        InitiateWallJump();
    }

}

private void InitiateWallJump()
{
    if(!isWallJumping)
    {
        isWallJumping = true;
        useWallJumpMoveStats = true;

    }

    StopWallSlide();
    ResetJumpValues();
    wallJumpTime = 0f;
    VerticalVelocity = MoveStats.InitialWallJumpVelocity;

    int dirMultiplier = 0;
    Vector2 hitPoint = lastWallHit.collider.ClosestPoint(bodyCol.bounds.center);

    if (hitPoint.x > transform.position.x)
    {
        dirMultiplier = -1;

    }
    else {dirMultiplier = 1;}
    HorizontalVelocity = Mathf.Abs(MoveStats.WallJumpDirection.x) * dirMultiplier;
}
private void WallJump()
{
    //apply wall jump gravity
    if(isWallJumping)
    {
        //time to take over movement controls while wall jumping
        wallJumpTime += Time.fixedDeltaTime;
        if (wallJumpTime >= MoveStats.TimeTillJumpApex)
        {
            useWallJumpMoveStats = false;

        }
        //hit head
        if (bumpedHead)
        {
            isWallJumpFastFalling = true;
            useWallJumpMoveStats = false;
        }
        //gravity in ascending

        if(VerticalVelocity >= 0f)
        {
            //apex controls
            wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, VerticalVelocity);
            if(wallJumpApexPoint > MoveStats.ApexThreshold)
            {
                if(!isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold = true;
                    timePastWallJumpApexThreshold = 0f;

                }
                if(isPastWallJumpApexThreshold)
                {
                    timePastWallJumpApexThreshold += Time.fixedDeltaTime;
                    if(timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
                    {
                        VerticalVelocity = 0f;
                    }
                    else
                    {
                        VerticalVelocity = -0.01f;
                    }
                }
            }

            //gravity in ascending but not past apex threshold
            else if (!isWallJumpFastFalling)
            {
                VerticalVelocity += MoveStats.WallJumpGravity * Time.fixedDeltaTime;
                if (isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold = false;
                }
            }
        }

        //gravity on descending
        else if (!isWallJumpFastFalling)
        {
            VerticalVelocity += MoveStats.WallJumpGravity * Time.fixedDeltaTime;
        }
        else if (VerticalVelocity < 0f)
        {
            if (!isWallJumpFalling)
            {
                isWallJumpFalling = true;
            }
        }

        //handle wall jump cut time
        if(isWallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                VerticalVelocity += MoveStats.WallJumpGravity*MoveStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(wallJumpFastFallReleaseSpeed, 0f, (wallJumpFastFallTime/MoveStats.TimeForUpwardsCancel));

            }
            wallJumpFastFallTime += Time.fixedDeltaTime;
        }
    }

}

private bool ShouldApplyPostWallJumpBuffer()
{
    if(!isGrounded &&(isTouchingWall || isWallSliding))
    {
        return true;
    }
    else {return false;}
}
private void ResetWallJumpValues()
{
    isWallSlideFalling = false;
    useWallJumpMoveStats = false;
    isWallJumping = false;
    isWallJumpFastFalling = false;
    isWallJumpFalling = false;
    isPastWallJumpApexThreshold = false;

    wallJumpFastFallTime = 0f;
    wallJumpTime = 0f;

}

#endregion
#region dash

private void ResetDashValues()
{
    isDashFastFalling = false;
    dashOnGroundTimer = -0.01f;

}

private void ResetDashes()
{
    numberOfDashesUsed = 0;
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
         */



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


    private void IsTouchingWall()
    {
        float originEndPoint = 0f;
        if(isFacingRight)
        {
            originEndPoint = bodyCol.bounds.max.x;

        }
        else {originEndPoint = bodyCol.bounds.min.x;}
        float adjustedHeight = bodyCol.bounds.size.y * MoveStats.WallDetectionRayHeightMultilpier;

        Vector2 boxCastOrigin = new Vector2(originEndPoint, bodyCol.bounds.center.y);
        Vector2 boxCastSize = new Vector2(MoveStats.WallDetectionRayLength, adjustedHeight);

        wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, MoveStats.WallDetectionRayLength, MoveStats.GroundLayer);
        if (wallHit.collider != null)
        {
            lastWallHit = wallHit;
            isTouchingWall = true;
        }
        else { isTouchingWall =false;}

        #region Debug Visualization

        if(MoveStats.DebugShowWallHitBox)
        {
            Color raycolor;
            if(isTouchingWall)
            {
                //rayColor = Color.green;

            }
            else
            {

            }

            Vector2 boxBottomLeft = new Vector2(boxCastOrigin.x - boxCastSize.x/2, boxCastOrigin.y - boxCastSize.y/2);
            Vector2 boxBottomRight = new Vector2(boxCastOrigin.x + boxCastSize.x/2, boxCastOrigin.y - boxCastSize.y/2);
            Vector2 boxTopLeft = new Vector2(boxCastOrigin.x - boxCastSize.x/2, boxCastOrigin.y + boxCastSize.y/2);
            Vector2 boxTopRight = new Vector2(boxCastOrigin.x + boxCastSize.x/2, boxCastOrigin.y + boxCastSize.y/2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, Color.green);
            Debug.DrawLine(boxBottomRight, boxTopRight, Color.green);
            Debug.DrawLine(boxTopRight, boxTopLeft, Color.green);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, Color.green);
            




        }

        #endregion

    }


    private void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();
        IsTouchingWall();

    }



#endregion

  
}
