using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
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
    private bool isStickyWalking = false;
    public float jump = 3f;
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
    public bool stickyJumping = false;

    void Update()
    {
        Move = Input.GetAxisRaw("Horizontal");

        if (!isWallJumping) Flip();
        WallSlide();
        WallJump();

        Vector3 worldDirection = transform.right * Move;
        Vector3 forward = transform.TransformDirection(new Vector2(rayDir, -1));
        Vector3 tilted = transform.TransformDirection(new Vector2(-rayDir, -1));
        Vector3 wallOffset = transform.up * 0.25f;
        Vector3 littleOffset = transform.up * 0.1f;

        if (!stickyJumping && !isWallJumping)
        {
            if (isStickyWalking)
                rb.velocity = new Vector2(speed * worldDirection.x, speed * worldDirection.y);
            else
                rb.velocity = new Vector2(speed * worldDirection.x, rb.velocity.y);
        }

        Vector3 offsets = 0.3f * transform.up;

        if (!stickyJumping)
        {
            RaycastHit2D forwardCast = Physics2D.Raycast(transform.position + littleOffset, forward, 0.4f, stickyLayer);
            RaycastHit2D groundCast = Physics2D.Raycast(transform.position + littleOffset, -transform.up, 0.1f, stickyLayer);
            RaycastHit2D tiltedCast = Physics2D.Raycast(transform.position + littleOffset, tilted, 0.4f, stickyLayer);
            RaycastHit2D upCast = Physics2D.Raycast(transform.position + littleOffset, transform.up, 0.4f, stickyLayer);
            RaycastHit2D wallCast = Physics2D.Raycast(transform.position + wallOffset, rayDir * transform.right, 0.3f, stickyLayer);
            Debug.DrawRay(transform.position + littleOffset, forward * forwardCast.distance, Color.blue); //forward
            Debug.DrawRay(transform.position + littleOffset, -transform.up * groundCast.distance, Color.green); //ground
            Debug.DrawRay(transform.position + littleOffset, tilted * tiltedCast.distance, Color.red); //tilted
            Debug.DrawRay(transform.position + littleOffset, transform.up * upCast.distance, Color.black); //up
            Debug.DrawRay(transform.position + wallOffset, rayDir * transform.right * wallCast.distance, Color.magenta); //wall

            float zRotation = transform.eulerAngles.z;

            if (groundCast)
            {
                isStickyWalking = true;
                if (isWalled())
                {
                    if (rb.velocity.y < 0)
                    {
                        transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move * 1), 90);
                        isStickyWalking = false;
                    }
                }
            }
            else
            {
                isStickyWalking = false;
            }

            if (isStickyWalking)
            {
                rb.gravityScale = 0;
                if (!forwardCast && tiltedCast)
                {
                    transform.Rotate(0, 0, -Move * 90f);
                }
            }
            else
            {
                rb.gravityScale = 1;
            }

            if (wallCast.collider)
            {
                transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move * 1), 90);
                isStickyWalking = true;
            }

            if (upCast)
            {
                transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move * 1), 180);
                isStickyWalking = true;
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isStickyWalking)
            {
                Vector3 jumpDir = transform.up * jump;
                rb.velocity = jumpDir;
                transform.RotateAround(transform.position + offsets, Vector3.forward, -transform.eulerAngles.z);
                isStickyWalking = false;
                rb.gravityScale = 1;
                stickyJumping = true;
            }
            else if (isGrounded())
            {
                rb.velocity = new Vector2(rb.velocity.x, jump);
            }

            Invoke(nameof(StopStickyJumping), 0.4f);
        }
        Debug.Log(isGrounded());

        if (isGroundedHighJump())
        {
            jump = 10f;
        }
        else
        {
            jump = 3f;
        }
    }

    bool isGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    bool isGroundedHighJump()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, highJumpLayer);
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

    void StopStickyJumping()
    {
        stickyJumping = false;
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