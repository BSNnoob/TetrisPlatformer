using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2D Sprite version of PlatformChaser2D
// Works with SpriteRenderers instead of 3D meshes

public class Test : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource AudioJump;
    public AudioSource AudioTurn;
    public AudioSource AudioNope;

    [Header("Movement Settings")]
    public float PlatformWalkSpeed = 5.0f;
    public float InterPlatformSpeed = 10.0f;
    public float SensingCastLift = 0.25f;
    public float SensingCastDistance = 1.0f;

    // Movement state
    bool facingLeft = false;
    bool isAirborne = false;
    Vector3 Destination;

    // Visual rotation for ground following
    float currentRotation;
    float desiredRotation;
    const float RateOfRotation = 600.0f;

    // Left/Right facing (sprite flipping)
    float currentFacing;
    const float RateOfFacing = 1000.0f;

    // Visual components - adapted for 2D
    Transform visuals;
    Transform spriteContainer;

    // Input
    bool moveLeftIntent;
    bool moveRightIntent;
    bool jumpIntent;

    void Start()
    {
        // Create visual hierarchy if it doesn't exist
        SetupVisualHierarchy();

        // DEBUG: Show starting position
        Debug.Log("Player starting at position: " + transform.position);
        
        // Find starting ground - cast in multiple directions to be sure
        RaycastHit2D hit = new RaycastHit2D();
        
        // Try straight down first
        hit = Physics2D.Raycast(origin: transform.position, direction: Vector2.down, distance: 20f);
        Debug.Log("Down raycast hit: " + (hit.collider ? hit.collider.name : "NOTHING"));
        
        if (!hit.collider)
        {
            // Try all directions to find ANY ground
            Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right, 
                                   new Vector2(-1,-1).normalized, new Vector2(1,-1).normalized,
                                   new Vector2(-1,1).normalized, new Vector2(1,1).normalized };
            
            Debug.LogWarning("No ground directly below - searching in all directions...");
            
            foreach(Vector2 dir in directions)
            {
                hit = Physics2D.Raycast(origin: transform.position, direction: dir, distance: 10f);
                Debug.Log("Trying direction " + dir + " - Hit: " + (hit.collider ? hit.collider.name : "NOTHING"));
                if (hit.collider) break;
            }
        }

        if (!hit.collider)
        {
            Debug.LogError("=== SETUP PROBLEM ===");
            Debug.LogError("No platforms found around player!");
            Debug.LogError("Check these things:");
            Debug.LogError("1. Do your platforms have Collider2D components?");
            Debug.LogError("2. Are platforms on the same Z-level as player?");
            Debug.LogError("3. Are Physics2D collision layers set up correctly?");
            Debug.LogError("Player position: " + transform.position);
            return;
        }

        Debug.Log("SUCCESS: Found ground on " + hit.collider.name + " at " + hit.point);
        JumpToHit(hit);
    }

    void SetupVisualHierarchy()
    {
        // Check if we already have the proper setup
        if (transform.childCount > 0)
        {
            visuals = transform.GetChild(0);
            visuals.SetParent(null);
            
            if (visuals.childCount > 0)
            {
                spriteContainer = visuals.GetChild(0);
            }
            else
            {
                // Create sprite container
                GameObject container = new GameObject("SpriteContainer");
                container.transform.SetParent(visuals);
                spriteContainer = container.transform;
            }
        }
        else
        {
            // Create the visual hierarchy from scratch
            GameObject visualsObj = new GameObject("Visuals");
            visuals = visualsObj.transform;
            
            GameObject spriteObj = new GameObject("SpriteContainer");
            spriteObj.transform.SetParent(visuals);
            spriteContainer = spriteObj.transform;

            // If there's a SpriteRenderer on this object, move it to the sprite container
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Move the sprite renderer to the container
                SpriteRenderer newSR = spriteContainer.gameObject.AddComponent<SpriteRenderer>();
                newSR.sprite = sr.sprite;
                newSR.color = sr.color;
                newSR.sortingLayerName = sr.sortingLayerName;
                newSR.sortingOrder = sr.sortingOrder;
                
                // Remove original
                DestroyImmediate(sr);
            }
        }

        // Make sure we have a collider for raycasting
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
    }

    void SetDesiredRotationFromNormal(Vector2 normal)
    {
        desiredRotation = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
    }

    void JumpToHit(RaycastHit2D hit)
    {
        if (AudioJump) AudioJump.Play();

        transform.up = hit.normal;
        SetDesiredRotationFromNormal(hit.normal);

        isAirborne = true;
        Destination = hit.point;
    }

    bool AttemptToJump()
    {
        var hit = Physics2D.Raycast(
            origin: (Vector2)transform.position + (Vector2)transform.up * SensingCastLift, 
            direction: transform.up,
            distance: SensingCastDistance
        );

        if (hit.collider)
        {
            // Flip direction when jumping (optional behavior)
            facingLeft = !facingLeft;
            JumpToHit(hit);
            return true;
        }

        if (AudioNope) AudioNope.Play();
        return false;
    }

    void GatherInputIntents()
    {
        // A/D movement
        moveLeftIntent = Input.GetKey(KeyCode.A);
        moveRightIntent = Input.GetKey(KeyCode.D);
        jumpIntent = Input.GetKeyDown(KeyCode.Space);
        
        // Alternative: Arrow keys
        if (Input.GetKey(KeyCode.LeftArrow)) moveLeftIntent = true;
        if (Input.GetKey(KeyCode.RightArrow)) moveRightIntent = true;
        if (Input.GetKeyDown(KeyCode.UpArrow)) jumpIntent = true;
    }

    bool ProcessAirborne()
    {
        if (isAirborne)
        {
            transform.position = Vector3.MoveTowards(transform.position, Destination, InterPlatformSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, Destination) < 0.1f)
            {
                transform.position = Destination;
                isAirborne = false;
            }
        }

        return isAirborne;
    }

    void ProcessMovement()
    {
        Vector3 walkVector = Vector3.zero;
        bool isMoving = false;
        bool shouldFlip = false;

        // Determine movement direction
        if (moveLeftIntent && !moveRightIntent)
        {
            walkVector = -transform.right;
            isMoving = true;
            if (!facingLeft) shouldFlip = true;
            facingLeft = true;
        }
        else if (moveRightIntent && !moveLeftIntent)
        {
            walkVector = transform.right;
            isMoving = true;
            if (facingLeft) shouldFlip = true;
            facingLeft = false;
        }

        // Play turn sound when changing direction
        if (shouldFlip && AudioTurn)
        {
            AudioTurn.Play();
        }

        if (!isMoving) return;

        // Apply movement
        walkVector *= PlatformWalkSpeed * Time.deltaTime;

        // Limit step size
        const float MaximumStepDistance = 0.2f;
        if (walkVector.magnitude > MaximumStepDistance)
        {
            walkVector = walkVector.normalized * MaximumStepDistance;
        }

        Vector3 nextPosition = transform.position + walkVector;
        Vector2 downVector = -transform.up;
        Vector2 castLiftOffset = (Vector2)transform.up * SensingCastLift;

        // DEBUG: Show what we're doing
        Debug.Log("Moving from " + transform.position + " to " + nextPosition);
        Debug.Log("Cast from " + ((Vector2)nextPosition + castLiftOffset) + " direction " + downVector);

        // Check for ground ahead
        var nextHit = Physics2D.Raycast(
            origin: (Vector2)nextPosition + castLiftOffset,
            direction: downVector,
            distance: SensingCastDistance
        );

        Debug.Log("Forward raycast hit: " + (nextHit.collider ? nextHit.collider.name + " at " + nextHit.point : "NOTHING"));

        // If no ground ahead, try corner detection
        if (!nextHit.collider)
        {
            Debug.Log("No ground ahead - trying corner detection");
            
            float rotateAngle = (facingLeft ? +1 : -1) * 45.0f;
            var rot45 = Quaternion.Euler(0, 0, rotateAngle);
            
            Vector2 extraOffset = ((Vector2)walkVector.normalized) * 0.01f;
            Vector2 tiltedPosition = (Vector2)nextPosition + extraOffset + (Vector2)(rot45 * (Vector3)castLiftOffset);
            Vector2 tiltedDirection = rot45 * (Vector3)downVector;

            var tiltedHit = Physics2D.Raycast(
                origin: tiltedPosition,
                direction: tiltedDirection,
                distance: SensingCastDistance
            );

            Debug.Log("Corner raycast hit: " + (tiltedHit.collider ? tiltedHit.collider.name + " at " + tiltedHit.point : "NOTHING"));

            if (!tiltedHit.collider)
            {
                Debug.Log("No ground in any direction - staying put");
                return;
            }

            nextHit = tiltedHit;
        }

        // Move to new position
        Debug.Log("Moving to ground at: " + nextHit.point + " with normal: " + nextHit.normal);
        transform.position = nextHit.point;
        SetDesiredRotationFromNormal(nextHit.normal);

        // Snap rotation for physics
        transform.rotation = Quaternion.Euler(0, 0, desiredRotation);
    }

    void Update()
    {
        // Handle airborne movement
        if (ProcessAirborne())
        {
            UpdateVisuals();
            return;
        }

        // Get input
        GatherInputIntents();

        // Handle jumping
        if (jumpIntent)
        {
            if (AttemptToJump())
            {
                UpdateVisuals();
                return;
            }
        }

        // Process ground movement
        ProcessMovement();

        // Update visual appearance
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (!visuals || !spriteContainer) return;

        // Position visuals
        visuals.position = transform.position;

        // Smooth rotation for ground angle
        currentRotation = Mathf.MoveTowardsAngle(currentRotation, desiredRotation, RateOfRotation * Time.deltaTime);
        visuals.rotation = Quaternion.Euler(0, 0, currentRotation);

        // Handle sprite flipping for left/right
        float desiredFacing = facingLeft ? 180 : 0;
        currentFacing = Mathf.MoveTowardsAngle(currentFacing, desiredFacing, RateOfFacing * Time.deltaTime);
        spriteContainer.localRotation = Quaternion.Euler(0, currentFacing, 0);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Draw sensing rays
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + transform.up * SensingCastLift, -transform.up * SensingCastDistance);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + transform.up * SensingCastLift, transform.up * SensingCastDistance);
            
            if (isAirborne)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, Destination);
            }
        }
    }
}