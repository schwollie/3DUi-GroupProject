using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    [Header("Hover Settings")]
    public float targetHoverHeight = 0f; // Height relative to force field center where objects should hover

    public float hoverForceStrength = 10f; // Base strength of the hover force
    public float maxForce = 50f; // Maximum force that can be applied

    [Header("Stabilization")] public float dragWhileHovering = 8f; // Higher drag for more stability
    public float verticalDamping = 0.5f; // Damping factor for vertical velocity (0-1)

    [Header("Force Falloff")] public float forceRangeMultiplier = 2f; // How quickly force falls off with distance

    private SphereCollider sphereCollider;
    private readonly Dictionary<Rigidbody, float> originalDrags = new();
    private readonly Dictionary<Rigidbody, Vector3> previousVelocities = new();

    private void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (!sphereCollider.isTrigger)
        {
            Debug.LogWarning("SphereCollider should be set as a trigger.");
            sphereCollider.isTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            // Calculate target hover position
            var targetY = transform.position.y + targetHoverHeight;
            var currentY = rb.position.y;
            var yDifference = targetY - currentY;

            // Calculate base force needed to hover
            var gravityCompensation = -Physics.gravity.y * rb.mass;

            // Calculate position-based force with smooth falloff
            var normalizedDistance = Mathf.Clamp01(Mathf.Abs(yDifference) / forceRangeMultiplier);
            var positionForce = yDifference * hoverForceStrength * (1f - normalizedDistance);

            // Apply velocity damping to reduce oscillation
            var currentVelocityY = rb.linearVelocity.y;
            var velocityDamping = -currentVelocityY * verticalDamping * rb.mass;

            // Combine all forces
            var totalForce = gravityCompensation + positionForce + velocityDamping;

            // Clamp the force to prevent extreme values
            totalForce = Mathf.Clamp(totalForce, -maxForce, maxForce);

            // Apply the force
            rb.AddForce(Vector3.up * totalForce, ForceMode.Force);

            // Set drag for stability
            if (!originalDrags.ContainsKey(rb))
            {
                originalDrags[rb] = rb.linearDamping;
                rb.linearDamping = dragWhileHovering;
            }

            // Store velocity for next frame
            previousVelocities[rb] = rb.linearVelocity;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            if (originalDrags.ContainsKey(rb))
            {
                rb.linearDamping = originalDrags[rb];
                originalDrags.Remove(rb);
            }

            if (previousVelocities.ContainsKey(rb)) previousVelocities.Remove(rb);
        }
    }
}