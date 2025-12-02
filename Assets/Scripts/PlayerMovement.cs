using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource walkSource;
    public AudioClip jumpSound;
    public AudioClip stickySound;
    public AudioClip highjumpSound;
    public AudioClip runningSound;
    public AudioClip bouncySound;
    public AudioClip splashSound;
    public float speed = 5f;
    public float Move;
    public float wallSlidingSpeed = 2f;
    private bool isWallSliding;

    [SerializeField] public Rigidbody2D rb;
    [SerializeField] public Transform groundCheck;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform wallCheck;
    [SerializeField] public LayerMask wallLayer;
    [SerializeField] public LayerMask stickyLayer;
    [SerializeField] public LayerMask highJumpLayer;
    [SerializeField] public Transform ceilingCheck;
    private bool isStickyWalking = false;
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
    public bool stickyJumping = false;
    public bool grounded;
    [SerializeField] public Animator animator;
    public static int keyCount = 0;
    public Text keyText;
    public GameObject WinPanel;

    void Start()
    {
        keyCount = 0;
    }

    void Update()
    {
        keyText.text = "Keys Collected: " + keyCount.ToString();
        Move = Input.GetAxisRaw("Horizontal");

        if (Move != 0 && (isGrounded() || isStickyWalking))
        {
            if (isStickyWalking){
                walkSource.clip = stickySound;
                if (walkSource.isPlaying == false)
                    walkSource.Play();
            }
            else
            {
                walkSource.clip = runningSound;
                if (walkSource.isPlaying == false)
                    walkSource.Play();
            }
        }
        else
        {
            walkSource.Stop();
        }

        if (!stickyJumping) Flip();

        Vector3 worldDirection = transform.right * Move;
        Vector3 forward = transform.TransformDirection(new Vector2(rayDir, -1));
        Vector3 tilted = transform.TransformDirection(new Vector2(-rayDir, -1));
        Vector3 wallOffset = transform.up * 0.25f;
        Vector3 littleOffset = transform.up * 0.1f;

        if (!stickyJumping)
        {
            if (isStickyWalking)
                rb.velocity = new Vector2(speed * worldDirection.x, speed * worldDirection.y);
            else
                rb.velocity = new Vector2(speed * worldDirection.x, rb.velocity.y);
        }
        else
        {
            if(isStickyWalking && (Vector3.Angle(transform.up, Vector3.up) < 0.01f)) rb.velocity = new Vector2(speed * worldDirection.x, rb.velocity.y);
        }

        Vector3 offsets = 0.25f * transform.up;
        Vector3 offsets2 = 0.35f * transform.up;


        RaycastHit2D forwardCast = Physics2D.Raycast(transform.position + littleOffset, forward, 0.3f, stickyLayer);
        RaycastHit2D forwardGCast = Physics2D.Raycast(transform.position + littleOffset, forward, 0.3f, groundLayer);
        RaycastHit2D groundCast = Physics2D.Raycast(transform.position + littleOffset, -transform.up, 0.2f, stickyLayer);
        RaycastHit2D tiltedCast = Physics2D.Raycast(transform.position + littleOffset, tilted, 0.3f, stickyLayer);
        RaycastHit2D upCast = Physics2D.Raycast(transform.position + littleOffset, transform.up, 0.4f, stickyLayer);
        RaycastHit2D wallCast = Physics2D.Raycast(transform.position + wallOffset, rayDir * transform.right, 0.3f, stickyLayer);
        Debug.DrawRay(transform.position + littleOffset, forward * 0.4f, Color.blue); //forward
        Debug.DrawRay(transform.position + littleOffset, -transform.up * 0.2f, Color.green); //ground
        Debug.DrawRay(transform.position + littleOffset, tilted * 0.4f, Color.red); //tilted
        Debug.DrawRay(transform.position + littleOffset, transform.up * 0.4f, Color.black); //up
        Debug.DrawRay(transform.position + wallOffset, rayDir * transform.right * 0.3f, Color.magenta); //wall

        if (groundCast && !stickyJumping)
        {
            isStickyWalking = true;
        }
        else
        {
            if (!tiltedCast && !forwardCast) isStickyWalking = false;
        }

        if (isWalled() && groundCast && isGrounded())
        {
            if (rb.velocity.y < 0)
            {
                transform.RotateAround(transform.position + offsets2, new Vector3(0, 0, Move * 1), 90);

                isStickyWalking = false;
            }
        }

        if (isStickyWalking)
        {
            rb.gravityScale = 0;
            if (!forwardCast && tiltedCast && !groundCast && !forwardGCast && !stickyJumping)
            {
                transform.Rotate(0, 0, -Move * 90f);
                isStickyWalking = true;
            }
            if (forwardGCast)
            {
                float currentRotation = transform.eulerAngles.z;
                bool isVertical = Mathf.Abs(currentRotation - 90) < 5f || Mathf.Abs(currentRotation - 270) < 5f;

                if (isVertical)
                {
                    if (Move * rayDir > 0)
                    {
                        rb.velocity = new Vector2(0, 0);
                    }

                    if (Move * rayDir < 0)
                    {
                        transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move), -90);
                        isStickyWalking = false;
                    }
                }
            }
        }
        else
        {
            transform.eulerAngles = Vector3.zero;
            rb.gravityScale = 1;
        }

        if (wallCast && !stickyJumping)
        {
            transform.RotateAround(transform.position + offsets, new Vector3(0, 0, Move * 1), 90);
            isStickyWalking = true;
        }

        if (upCast && !stickyJumping)
        {
            transform.RotateAround(transform.position + offsets, new Vector3(0, 0, 1), 180);
            isStickyWalking = true;
        }

        if (groundCast || isGrounded()) grounded = true;
        else grounded = false;


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
                if (isGroundedHighJump())
                {
                    audioSource.clip = highjumpSound;
                    audioSource.Play();
                    rb.velocity = new Vector2(rb.velocity.x, jump);
                }else{
                    audioSource.clip = jumpSound;
                    audioSource.Play();
                    rb.velocity = new Vector2(rb.velocity.x, jump);
                }
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
            jump = 6f;
        }

        animator.SetBool("isJumping", !grounded);
    }

    private void FixedUpdate()
    {
        animator.SetFloat("xVelocity", Mathf.Abs(Move));
        if (!isStickyWalking) animator.SetFloat("yVelocity", rb.velocity.y);
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
        return Physics2D.OverlapCircle(wallCheck.position, 0.3f, groundLayer);
    }

    bool isWalledSticky()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.05f, stickyLayer);
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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Key"))
        {
            keyCount += 1;
            Destroy(collision.gameObject);
        }

        if (collision.gameObject.CompareTag("FinishLine"))
        {
            if (keyCount < 3) return;
            WinPanel.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 11)
        {
            audioSource.clip = bouncySound;
            audioSource.Play();
        }

        if (collision.gameObject.layer == 9)
        {
            audioSource.clip = splashSound;
            audioSource.Play();
        }
    }
}