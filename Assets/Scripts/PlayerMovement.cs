using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

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
    public float castLift = 0.2f;
    public float rayDistance;
    public float desiredRotation;
    public float rayDir = 1f;
    public bool rotated;

    void Update()
    {
        Move = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded())
            {
                rb.velocity = new Vector2(rb.velocity.x, jump);
            }
        }
        if (!isWallJumping && !isStickyWalking) Flip();
        WallSlide();
        WallJump();

        Vector3 worldDirection = transform.right * Move;
        Vector3 forward = transform.TransformDirection(new Vector2(rayDir, -1));
        Vector3 tilted = transform.TransformDirection(new Vector2(-rayDir, -1));
        Vector3 wallOffset = transform.up * 0.25f;
        Vector3 littleOffset = transform.up * 0.1f;
        rb.velocity = new Vector2(speed * worldDirection.x, speed * worldDirection.y);

        RaycastHit2D forwardCast = Physics2D.Raycast(transform.position + littleOffset, forward, 0.5f, stickyLayer);
        RaycastHit2D groundCast = Physics2D.Raycast(transform.position + littleOffset, -transform.up, 0.5f, stickyLayer);
        RaycastHit2D tiltedCast = Physics2D.Raycast(transform.position + littleOffset, tilted, 0.5f, stickyLayer);
        RaycastHit2D wallCast = Physics2D.Raycast(transform.position + wallOffset, rayDir * transform.right, 0.3f, stickyLayer);
        Debug.DrawRay(transform.position + littleOffset, forward * forwardCast.distance, Color.blue);
        Debug.DrawRay(transform.position + littleOffset, -transform.up * groundCast.distance, Color.green);
        Debug.DrawRay(transform.position + littleOffset, tilted * tiltedCast.distance, Color.red);
        Debug.DrawRay(transform.position + wallOffset, rayDir * transform.right * 0.3f, Color.magenta);

        float zRotation = transform.eulerAngles.z;

        if (zRotation != 0)
        {
            rotated = true;
        }
        else
        {
            rotated = false;
        }

        if (rotated)
        {
            rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = 1;
        }

        if (!forwardCast.collider)
        {
            transform.Rotate(0, 0, -Move * 90f);
        }

        Vector3 offsets = 0.3f * transform.up;

        if (wallCast.collider)
        {
            transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move * 1), 90);
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
            rayDir = -rayDir;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }
}