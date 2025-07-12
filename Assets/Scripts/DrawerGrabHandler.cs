using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DrawerXRGrabInteractable : XRGrabInteractable
{
    [Header("Drawer Settings")] [SerializeField]
    private Vector3 moveAxis = Vector3.forward; // Local axis to move along

    [SerializeField] private float minPosition = 0f; // Minimum position along axis
    [SerializeField] private float maxPosition = 0.5f; // Maximum position along axis
    [SerializeField] private float smoothingSpeed = 10f; // Smoothing speed for movement

    [Header("Gizmo Settings")] [SerializeField]
    private Color gizmoColor = Color.green;

    [SerializeField] private float gizmoSize = 0.05f;

    // References
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    public Transform handle;
    private Rigidbody rb;

    // Runtime variables
    private float currentPosition;
    private float targetPosition;
    private Vector3 lastInteractorPosition;
    private bool isBeingGrabbed;

    // Constraint helpers
    private Vector3 worldAxis; // World space direction of movement axis

    protected override void Awake()
    {
        base.Awake();

        // Store original parent and position
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // Find handle in children
        if (handle == null) Debug.LogWarning("No handle found as child of drawer!");

        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // Configure Rigidbody for kinematic movement
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Freeze all rotations

        // Configure XRGrabInteractable settings
        movementType = MovementType.Kinematic;
        attachTransform = handle;
        throwOnDetach = false;
        trackRotation = false; // Disable rotation tracking
        trackPosition = false; // We'll handle position ourselves

        // Calculate current position along axis
        currentPosition = Vector3.Dot(transform.localPosition - initialLocalPosition, moveAxis.normalized);
        targetPosition = currentPosition;

        // Calculate world axis direction
        UpdateWorldAxis();
    }

    private void UpdateWorldAxis()
    {
        if (originalParent != null) worldAxis = originalParent.TransformDirection(moveAxis.normalized);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        isBeingGrabbed = true;

        // Restore our parent relationship if it was changed
        if (transform.parent != originalParent && originalParent != null) transform.SetParent(originalParent, true);

        // Store initial interactor position
        if (args.interactorObject.transform != null) lastInteractorPosition = args.interactorObject.transform.position;

        // Update world axis in case parent has rotated
        UpdateWorldAxis();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isBeingGrabbed = false;

        // Ensure we're back with original parent
        if (transform.parent != originalParent && originalParent != null) transform.SetParent(originalParent, true);
    }

    private void Update()
    {
        if (isBeingGrabbed && isSelected) UpdateDrawerPosition();

        // Apply smoothing
        currentPosition = Mathf.Lerp(currentPosition, targetPosition, Time.deltaTime * smoothingSpeed);

        // Clamp current position to ensure it never exceeds limits
        currentPosition = Mathf.Clamp(currentPosition, minPosition, maxPosition);

        ApplyPosition();
    }

    private void UpdateDrawerPosition()
    {
        if (firstInteractorSelecting == null) return;

        // Get current interactor position
        var currentInteractorPosition = firstInteractorSelecting.transform.position;

        // Calculate hand movement in world space
        var handMovement = currentInteractorPosition - lastInteractorPosition;

        // Project movement onto the drawer's movement axis (in world space)
        var movementAlongAxis = Vector3.Dot(handMovement, worldAxis);

        // Update target position
        var newTargetPosition = targetPosition + movementAlongAxis;

        // Clamp to limits
        targetPosition = Mathf.Clamp(newTargetPosition, minPosition, maxPosition);

        // Update last interactor position for next frame
        lastInteractorPosition = currentInteractorPosition;
    }

    private void ApplyPosition()
    {
        // Ensure we maintain parent relationship
        if (transform.parent != originalParent && originalParent != null) transform.SetParent(originalParent, true);

        // Calculate new LOCAL position (only along the specified axis)
        var newLocalPosition = initialLocalPosition + moveAxis.normalized * currentPosition;

        // Apply position - force local position to prevent any deviation
        transform.localPosition = newLocalPosition;

        // Force rotation to stay locked
        transform.localRotation = initialLocalRotation;

        // If using Rigidbody, sync its position
        if (rb != null && originalParent != null)
        {
            rb.position = transform.position;
            rb.rotation = transform.rotation;
        }
    }

    // Override the grab transformations to prevent default behavior
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        return handle != null ? handle : base.GetAttachTransform(interactor);
    }

    // Override to completely prevent position/rotation updates from XRGrabInteractable
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        // Call base but skip if we're grabbed
        if (!isBeingGrabbed)
        {
            base.ProcessInteractable(updatePhase);
        }
        else
        {
            // Only process the interaction state, not movement
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Maintain our constraints
                transform.localRotation = initialLocalRotation;

                // Ensure position stays on axis
                var currentLocal = transform.localPosition;
                var desiredLocal = initialLocalPosition + moveAxis.normalized * currentPosition;
                if (Vector3.Distance(currentLocal, desiredLocal) > 0.001f) transform.localPosition = desiredLocal;
            }
        }
    }

    private void OnDrawGizmos()
    {
        var axis = moveAxis.normalized;
        var basePos = Application.isPlaying ? initialLocalPosition : transform.localPosition;

        var parent = Application.isPlaying ? originalParent : transform.parent;
        if (parent == null) return;

        Gizmos.color = gizmoColor;

        // Draw min position
        var minWorldPos = parent.TransformPoint(basePos + axis * minPosition);
        Gizmos.DrawWireSphere(minWorldPos, gizmoSize);
        Gizmos.DrawWireCube(minWorldPos, Vector3.one * gizmoSize * 1.5f);

        // Draw max position
        var maxWorldPos = parent.TransformPoint(basePos + axis * maxPosition);
        Gizmos.DrawWireSphere(maxWorldPos, gizmoSize);
        Gizmos.DrawWireCube(maxWorldPos, Vector3.one * gizmoSize * 1.5f);

        // Draw line between min and max
        Gizmos.DrawLine(minWorldPos, maxWorldPos);

        // Draw current position when playing
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            var currentWorldPos = parent.TransformPoint(basePos + axis * currentPosition);
            Gizmos.DrawSphere(currentWorldPos, gizmoSize * 0.8f);
        }
    }

    public void ResetPosition()
    {
        targetPosition = 0f;
        currentPosition = 0f;
        ApplyPosition();
    }

    public void SetNormalizedPosition(float normalizedPos)
    {
        var range = maxPosition - minPosition;
        targetPosition = minPosition + range * Mathf.Clamp01(normalizedPos);
    }

    public float GetNormalizedPosition()
    {
        var range = maxPosition - minPosition;
        return range > 0 ? (currentPosition - minPosition) / range : 0f;
    }
}