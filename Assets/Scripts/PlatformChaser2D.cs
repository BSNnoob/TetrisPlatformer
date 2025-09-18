using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// @kurtdekker
// 2D platform follower and leaper
//
// See platform_chaser_2d.png for visual explanation.
//

public class PlatformChaser2D : MonoBehaviour
{
	public AudioSource AudioJump;
	public AudioSource AudioTurn;
	public AudioSource AudioNope;

	// "left" means from the perspective of the platform you are on, so "left"
	// actually means "right" when you're dancing on the ceiling. What a feeling.
	bool facingLeft;

	const float PlatformWalkSpeed = 5.0f;
	const float InterPlatformSpeed = 10.0f;

	// only applies to visuals: this is the Z rotation to match ground contour
	float currentRotation;
	float desiredRotation;

	// increase this as you move faster otherwise it looks funny as you go around corners
	const float RateOfRotation = 600.0f;

	// this is around the Y rotation as the player turns left / right
	float currentFacing;
	const float RateOfFacing = 1000.0f;

	// how much above our contact point do we cast? (to avoid entering slopes)
	const float SensingCastLift = 0.25f;
	// limit our cast to avoid grabbing distant things
	const float SensingCastDistance = 1.0f;

	Transform visuals;
	Transform visualsLeftRightPivot;

	void Start ()
	{
		// find the visuals (first transform)
		visuals = transform.GetChild(0);
		// rip them off (deparent them)
		visuals.SetParent( null);
		// and we assume the visuals have ONE child which is used
		// to look left/right based on facingLeft
		visualsLeftRightPivot = visuals.GetChild(0);

		// find a starting point by going directly transform.down
		var hit = Physics2D.Raycast( origin: transform.position, direction: -transform.up);

		if (!hit)
		{
			Debug.LogError( "Didn't hit anything on start!");
			Debug.Break();
		}

		JumpToHit( hit);
	}

	void SetDesiredRotationFromNormal( Vector3 normal)
	{
		desiredRotation = Mathf.Atan2( -normal.x, normal.y) * Mathf.Rad2Deg;
	}

	void JumpToHit( RaycastHit2D hit)
	{
		AudioJump.Play();

		transform.up = hit.normal;

		SetDesiredRotationFromNormal( hit.normal);

		isAirborne = true;
		Destination = hit.point;
	}

	bool AttemptToJump()
	{
		var hit = Physics2D.Raycast( origin: transform.position + transform.up * SensingCastLift, direction: transform.up);

		if (hit.collider)
		{
			// NOTE: this is a matter of taste: you might want
			// to flip the walk direction so it seems more
			// continuous upon arrival. Remove this if so.
			facingLeft = !facingLeft;

			JumpToHit( hit);		// I regret nothing!

			return true;
		}

		AudioNope.Play();

		return false;
	}

	bool reverseIntent;
	bool jumpIntent;

	void GatherInputIntents()
	{
		reverseIntent = false;
		jumpIntent = false;

		if (Input.GetKeyDown( KeyCode.Space))
		{
			reverseIntent = true;
		}
		if (Input.GetKeyDown( KeyCode.Tab))
		{
			jumpIntent = true;
		}
	}

	bool isAirborne;
	Vector3 Destination;
	bool ProcessAirborne()
	{
		if ( isAirborne)
		{
			transform.position = Vector3.MoveTowards( transform.position, Destination, InterPlatformSpeed * Time.deltaTime);

			if ( Vector3.Distance( transform.position, Destination) < 0.1f)
			{
				transform.position = Destination;
				isAirborne = false;
			}
		}

		return isAirborne;
	}

	void Update ()
	{
		// input is disregarded while you're flying through the air
		if (ProcessAirborne())
		{
			UpdateVisuals();
			return;
		}

		GatherInputIntents();

		if (reverseIntent)
		{
			facingLeft = !facingLeft;
			AudioTurn.Play();
		}

		if (jumpIntent)
		{
			if (AttemptToJump())
			{
				return;
			}
		}

		Vector3 downVector = -transform.up;

		Vector3 walkVector = transform.right;

		if (facingLeft)
		{
			walkVector = -walkVector;
		}

		walkVector *= PlatformWalkSpeed;

		walkVector *= Time.deltaTime;

		// let's just keep each step semi-reasonable
		const float MaximumStepDistance = 0.2f;
		if (walkVector.magnitude > MaximumStepDistance)
		{
			walkVector = walkVector.normalized * MaximumStepDistance;
		}

		var position = transform.position;

		Vector3 CastLiftOffset = transform.up * SensingCastLift;

		position += walkVector;

		var nextHit = Physics2D.Raycast(
			origin: position + CastLiftOffset,
			direction: downVector,
			distance: SensingCastDistance);

		// if you step into the void, we have to track the ground
		if (!nextHit.collider)
		{
			float rotate45 = (facingLeft ? +1 : -1) * 45.0f;

			var rot45 = Quaternion.Euler( 0, 0, rotate45);

			// step a teeny bit further out along this way
			Vector3 extraForwardOffset = walkVector.normalized * 0.01f;

			Vector3 tiltedPosition = position + extraForwardOffset + rot45 * CastLiftOffset;
			Vector3 tiltedDirection = rot45 * downVector;

			// try and hit that collider now, leaning forward and angling back
			var tiltedHit = Physics2D.Raycast(
				origin: tiltedPosition,
				direction: tiltedDirection,
				distance: SensingCastDistance);

			if (!tiltedHit.collider)
			{
				// we couldn't get around this corner, reverse!
				facingLeft = !facingLeft;
				return;
			}

			nextHit = tiltedHit;
		}

		position = nextHit.point;

		SetDesiredRotationFromNormal( nextHit.normal);

		// primary object instant-snaps always
		transform.position = position;
		transform.rotation = Quaternion.Euler( 0, 0, desiredRotation);

		UpdateVisuals();
	}

	void UpdateVisuals()
	{
		// visuals always stick with us
		visuals.position = transform.position;

		// but they gradually rotate to face
		currentRotation = Mathf.MoveTowardsAngle( currentRotation, desiredRotation, RateOfRotation * Time.deltaTime);
		visuals.rotation = Quaternion.Euler ( 0, 0, currentRotation);

		float desiredFacing = facingLeft ? 180 : 0;

		currentFacing = Mathf.MoveTowardsAngle( currentFacing, desiredFacing, RateOfFacing * Time.deltaTime);
		visualsLeftRightPivot.localRotation = Quaternion.Euler( 0, currentFacing, 0);
	}
}
