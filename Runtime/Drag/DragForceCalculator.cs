using UnityEngine;

public static class DragForceCalculator
{
    /**
     * Computes the drag force vector to apply to the Rigidbody.
     * * @param localVelocity The velocity relative to the object's rotation (for lookup).
     * @param worldVelocity The actual movement direction in the scene.
     * @param s The drag settings.
     * @param lookup The baked directional data.
     */
    public static Vector3 ComputeDrag(Vector3 localVelocity, Vector3 worldVelocity, DragSettings s, DirectionalDragLookup lookup)
    {
        float speed = worldVelocity.magnitude;

        // The normalized directions
        Vector3 worldDir = worldVelocity / speed;
        Vector3 localDir = localVelocity / speed;

        float forceMagnitude = 0f;

        if (s.model == DragModel.Linear)
        {
            // Simple linear drag (often used for very small particles or low speeds)
            forceMagnitude = s.linearCoeff_b * speed;
        }
        else // Quadratic (Standard aerodynamic drag)
        {
            if (s.useDirectionalLookup && lookup != null)
            {
                // Grab the pre-calculated K value based on the local angle of attack
                float k = lookup.GetK(localDir);
                forceMagnitude = k * speed * speed;
            }
            else
            {
                // Fallback if lookup is disabled: Assumes a uniform sphere
                forceMagnitude = 0.5f * s.airDensity * speed * speed * s.globalCd * 1f;
            }
        }

        // Drag always pushes in the exact opposite direction of movement
        return -worldDir * forceMagnitude;
    }
}