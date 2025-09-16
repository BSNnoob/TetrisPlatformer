using UnityEngine;

public static class PhysicsExtension2D
{
    // A helper function to convert an angle to a normalized 2D vector
    private static Vector2 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    static public bool ArcCast2D(Vector2 center, float rotationAngle, float angle, float radius, int resolution, LayerMask layer, out RaycastHit2D hit)
    {
        // Adjust the initial angle to center the arc around the rotation.
        rotationAngle -= angle / 2;

        for (int i = 0; i < resolution; i++)
        {
            // Calculate start and end points of the ray segment.
            Vector2 A = center + GetVectorFromAngle(rotationAngle) * radius;
            
            // CORRECTED: Increment the rotation angle by adding to it
            rotationAngle += angle / resolution;

            Vector2 B = center + GetVectorFromAngle(rotationAngle) * radius;
            Vector2 AB = B - A;

            // Draw a green line to visualize this segment of the arc.
            Debug.DrawLine(A, B, Color.green);

            // Cast the ray.
            RaycastHit2D rayHit = Physics2D.Raycast(A, AB.normalized, AB.magnitude * 1.001f, layer);

            if (rayHit.collider != null)
            {
                // If it hits, draw a red line to show where a hit occurred.
                Debug.DrawLine(A, rayHit.point, Color.red);
                hit = rayHit;
                return true;
            }
        }

        hit = new RaycastHit2D();
        return false;
    }
}