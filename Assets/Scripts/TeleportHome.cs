using UnityEngine;

public class TeleportHome : MonoBehaviour
{
    public float tresholdDistance = 30f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        startPosition = transform.position; // Store the initial position
        startRotation = transform.rotation; // Store the initial rotation (if needed)
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, startPosition) > tresholdDistance) ToHome();
    }

    public void ToHome()
    {
        // Teleport back to start position
        transform.position = startPosition;
        transform.rotation = startRotation;

        // Optional: Reset velocity if object has Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = startPosition; // Ensure position is set correctly
            rb.rotation = startRotation; // Ensure rotation is set correctly
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}