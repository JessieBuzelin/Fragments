using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Gameplay Properties")]

    // Horizontal player keyboard input
    //  -1 = Left
    //   0 = No input
    //   1 = Right
    private float playerInput = 0;
    private int tempExtraJumps;
    public float tempSpeed;
    private float tempJumpForce;

    // Horizontal player speed
    [SerializeField] private float speed = 10;
    [SerializeField] private float velPower = 0.9f;
    [SerializeField] private float acceleration = 7;
    [SerializeField] private float deceleration = 7;
    [SerializeField] private float frictionAmount = 0.2f;
    [SerializeField] private float jumpForce = 5;
    [Range(0, 1)][SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private int extraJumps = 0;
    [SerializeField] private float extraJumpForce = 5;

    [SerializeField] private float fallGravityMultiplayer;
    [SerializeField] private float maxFallingVel;

    public float tempAcceleration;
    private float tempDeceleration;


    [Header("Gameplay Features")]
    [SerializeField] private bool offGroundJumpEnabled = true;
    [SerializeField] private bool crouchingEnabled = true;
    [SerializeField] private bool dashingEnabled = true;
    [SerializeField] private bool wallSlidingEnabled = true;

    private bool isJumping;

    [Header("Wall Sliding/Jumping")]
    [SerializeField] private float wallSlidingSpeed = 5;
    [SerializeField] private float wallJumpAcceleration = 4f;
    [SerializeField] private float wallJumpingDuration = 0.4f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(8f, 16f);

    private bool isWallJumping;
    private bool isWallSliding;
    private bool isWallJumpingControl;
    private float wallJumpingDirection;


    [Header("Crouching")]
    [SerializeField] private float crouchSpeed = 6;
    [SerializeField] private float crouchJump = 4;

    [Header("Dashing")]
    [SerializeField] private float dashForce = 20;
    [SerializeField] private float dashTime = 0.8f;
    [SerializeField] private float dashDelay = 2f;

    private bool dash = true;
    public bool isDashing = false;
    public bool isKnockedBack = false;

    [Header("Component References")]
    [SerializeField] private Collider2D standingCol;
    [SerializeField] private Collider2D crouchingCol;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    private Rigidbody2D rb;
    private float gravityScale;

    [Header("offGroundJump")]
    private bool offGroundJump;
    private bool offGroundFirstFrame;
    [SerializeField] private float timeOffGround = 0.2f;

    [Header("Fragments")]
    [SerializeField] bool canMove;
    [SerializeField] bool canJump;
    [SerializeField] bool canDash;
    [SerializeField] bool canWallClimb;


    void Awake()
    {
        // Get component references
        rb = GetComponent<Rigidbody2D>();

        //Setup Variables
        tempSpeed = speed;
        tempJumpForce = jumpForce;

        gravityScale = rb.gravityScale;

        tempAcceleration = acceleration;
        tempDeceleration = deceleration;
    }

    void Update()
    {
        // Detect and store horizontal player input   
        playerInput = Input.GetAxisRaw("Horizontal");

        if (rb.velocity.y < maxFallingVel)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallingVel);
        }


        //Main Code
        Controller();

        if (isWallJumping) return;

        // Swap the player sprite scale to face the movement direction
        SwapSprite();
    }

    private void FixedUpdate()
    {
        if (isKnockedBack) return;
        Run();
    }

    // Swap the player sprite scale to face the movement direction
    void SwapSprite()
    {
        // Right
        if (playerInput > 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        // Left
        else if (playerInput < 0)
        {
            transform.localScale = new Vector3(
                -1 * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    // Is called automatically every physics step
    void Controller()
    {
        //Apply Artificial Friction
        Friction();

        //Crouching
        CrouchingLogic();

        //Dashing
        DashingLogic();

        //Detect if player jumped
        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            if (isGrounded() || offGroundJump && offGroundJumpEnabled)
            {
                Jump(tempJumpForce);
                offGroundJump = false;
            }
            else if (tempExtraJumps > 0 && !isGrounded())
            {
                Jump(extraJumpForce);
                tempExtraJumps--;
            }
        }

        //Sets variables accordingly to whether player is grounded or not
        GroundedLogic();

        //Applies Jump Gravity
        Gravity();

        //Checks if Jump was released
        JumpRelease();

        if (wallSlidingEnabled)
        {
            //Wall Sliding and Wall Jumping
            WallSlide();
            WallJump();
        }
    }

    void Run()
    {
        // Move the player horizontally
        if (!isDashing && canMove)
        {
            float targetSpeed = playerInput * tempSpeed;
            float speedDif = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? tempAcceleration : tempDeceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);
            rb.AddForce(movement * Vector2.right);
        }
    }

    void Friction()
    {
        if (playerInput == 0 && isGrounded())
        {
            float amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Mathf.Abs(frictionAmount));

            amount *= Mathf.Sign(rb.velocity.x);

            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    void Gravity()
    {
        if (!isDashing)
        {
            if (rb.velocity.y < 0)
            {
                rb.gravityScale = gravityScale * fallGravityMultiplayer;
            }
            else
            {
                rb.gravityScale = gravityScale;
            }
        }
    }

    void Jump(float force)
    {
        if (!canJump)       
            return;
        force -= rb.velocity.y / 1.25f;
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        isJumping = true;
    }

    void JumpRelease()
    {
        if (!Input.GetKey(KeyCode.Space))
        {
            if (rb.velocity.y > 0 && isJumping)
            {
                rb.AddForce(Vector2.down * rb.velocity.y * (1 - jumpCutMultiplier), ForceMode2D.Impulse);
            }

        }
    }

    void DashingLogic()
    {
        if (dashingEnabled && canDash)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && dash)
            {
                float dashPlayerInput = playerInput;
                StartCoroutine(ResetDash(dashPlayerInput));
            }
        }
    }

    void GroundedLogic()
    {
        if (isGrounded() && rb.velocity.y == 0)
        {
            tempExtraJumps = extraJumps;
            offGroundJump = true;
            offGroundFirstFrame = false;
            tempAcceleration = acceleration;

            if (rb.velocity.y < 0)
            {
                isJumping = false;

            }
        }
        else if (!isWallJumpingControl)
        {
            tempSpeed = speed;
            if (!offGroundFirstFrame)
            {
                StartCoroutine(offGroundJumpTimer());
                offGroundFirstFrame = true;
            }

        }
    }

    void CrouchingLogic()
    {
        //Crouching
        if (crouchingEnabled)
        {
            if (Input.GetKey(KeyCode.S))
            {
                //Crouching
                standingCol.enabled = false;
                crouchingCol.enabled = true;
                tempSpeed = crouchSpeed;
                tempJumpForce = crouchJump;
            }
            else if (!Input.GetKey(KeyCode.S) && !isAbove())
            {
                //Standing
                standingCol.enabled = true;
                crouchingCol.enabled = false;
                tempSpeed = speed;
                tempJumpForce = jumpForce;
            }
        }
    }

    void WallJump()
    {
        if (!canWallClimb)
            return;
        if (Input.GetKeyDown(KeyCode.Space) && isWallSliding)
        {
            Jump(wallJumpingPower.y);
            isWallJumping = true;
            wallJumpingDirection = -transform.localScale.x;
            StartCoroutine(ResetWallJump());
        }

        if (!isDashing)
        {
            if (isWallJumping)
            {
                rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
                tempAcceleration = wallJumpAcceleration;
                transform.localScale = new Vector2(wallJumpingDirection, transform.localScale.y);
            }
        }
    }

    void WallSlide()
    {
        if (IsWalled() && !isGrounded() && playerInput != 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);
    }

    bool isGrounded()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.down, 0.05f, groundLayer);
    }

    bool isAbove()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.up, 2f, groundLayer);
    }

    private IEnumerator offGroundJumpTimer()
    {
        yield return new WaitForSeconds(timeOffGround);
        offGroundJump = false;
    }
    private IEnumerator ResetDash(float input)
    {
        isDashing = true;
        dash = false;

        tr.emitting = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        if (playerInput == 1)
        {
            rb.velocity = new Vector2(dashForce, 0);
        }
        else if (playerInput == -1)
        {
            rb.velocity = new Vector2(-dashForce, 0);
        }
        else
        {
            //Want to dash right
            if (transform.localScale.x == 1)
            {
                rb.velocity = new Vector2(dashForce, 0);
            }
            //Wants to dash left
            else
            {
                rb.velocity = new Vector2(-dashForce, 0);
            }
        }

        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = originalGravity;
        isDashing = false;
        tr.emitting = false;

        yield return new WaitForSeconds(dashDelay);
        dash = true;
    }

    private IEnumerator ResetWallJump()
    {
        yield return new WaitForSeconds(wallJumpingDuration);
        isWallJumping = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        string objName = collision.gameObject.name;
        switch (objName)
        {
            case "Move":
                canMove = true;
                Destroy(collision.gameObject);
                break;
            case "Jump":
                canJump = true;
                Destroy(collision.gameObject);
                break;
            case "Dash":
                canDash = true;
                Destroy(collision.gameObject);
                break;
            case "WallClimb":
                canWallClimb = true;
                Destroy(collision.gameObject);
                break;
            case "Death":
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;

        }

        
    }
}
