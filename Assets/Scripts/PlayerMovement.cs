using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float Move;
    public float wallSlidingSpeed = 2f;
    private bool isWallSliding;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration= 0.4f;
    private Vector2 wallJumpingPower = new Vector2(3f, 3f);

    [SerializeField] public Rigidbody2D rb;
    [SerializeField] public Transform groundCheck;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform wallCheck;
    [SerializeField] public LayerMask wallLayer;
    [SerializeField] public LayerMask stickyLayer;
    [SerializeField] public LayerMask highJumpLayer;
    [SerializeField] public Transform ceilingCheck;
    private bool isStickyWalking;
    public float jump = 6f;
    private bool isFacingRight = true;
    private bool isRight;
    private bool canClimb = true;
    [SerializeField] private bool isCeillingClimbing = false;
    private bool notFacing = false;
    private Vector2 worldDirection;
    private bool isGround = false;
    private RaycastHit2D wallHit;

    void Update()
    {
        Move = Input.GetAxisRaw("Horizontal");

        WallSlide();
        WallJump();

    isGround = Physics2D.Raycast(transform.position, -transform.up, 0.2f, groundLayer);

    Vector2 rayOriginRight = (Vector2)transform.position + Vector2.right * 0.2f;
    Vector2 rayOriginLeft = (Vector2)transform.position + Vector2.left * 0.2f;

    RaycastHit2D rightHit = Physics2D.Raycast(rayOriginRight, -transform.up, 0.5f, groundLayer);
    RaycastHit2D leftHit = Physics2D.Raycast(rayOriginLeft, -transform.up, 0.5f, groundLayer);

    if (Mathf.Abs(Move) > 0.1f)
    {
        if (Move > 0)
        {
            wallHit = rightHit;
        }
        else
        {
            wallHit = leftHit;
        }
    }

    if (wallHit.collider != null && Mathf.Abs(wallHit.normal.y) < 0.9f)
    {
        isStickyWalking = true;
    }
    else
    {
        isStickyWalking = false;
    }

        if (isStickyWalking)
        {
            Debug.DrawRay(wallHit.point, wallHit.normal, Color.red);

            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, wallHit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);

            Vector2 tangent = Vector2.Perpendicular(wallHit.normal);
            if (Vector2.Dot(tangent, transform.right) < 0)
            {
                tangent = -tangent;
            }

            if (Mathf.Abs(Move) > 0.1f)
            {
                rb.velocity = tangent * speed * Move;
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
            rb.gravityScale = 0;
        }
        else if (isGround)
        {
            Debug.DrawRay(transform.position, -transform.up * 0.2f, Color.green);

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 10f * Time.deltaTime);
            rb.velocity = new Vector2(Move * speed, 0);
            rb.gravityScale = 1;
        }
        else
        {
            if (Mathf.Abs(Move) > 0.1f)
            {
                transform.Rotate(0, 0, -Mathf.Sign(Move) * 100 * Time.deltaTime);
            }

            rb.velocity = new Vector2(Move * speed, rb.velocity.y);
            rb.gravityScale = 1;
        }

    

        // Flip logic should be here
        if (!isStickyWalking)
        {
            if (Move > 0 && transform.localScale.x < 0)
            {
                Flip();
            }
            else if (Move < 0 && transform.localScale.x > 0)
            {
                Flip();
            }
        }
    }

    bool isGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    bool isGroundedHighJump()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, highJumpLayer);
    }

    bool isWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    bool isWalledSticky()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.05f, stickyLayer);
    }


    void WallSlide()
    {
        if (isWalled() && Move != 0 && !isGrounded())
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1;
                transform.localScale = localScale;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    void StopWallJumping()
    {
        isWallJumping = false;
    }

    void Flip()
    {
        if ((Move < 0f && isFacingRight) || Move > 0f && !isFacingRight)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }
}