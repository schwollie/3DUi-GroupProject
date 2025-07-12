using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LeverXRGrabInteractable : XRGrabInteractable
{
    [Header("Lever Settings")] [SerializeField]
    private Vector3 rotationAxis = Vector3.right; // Local axis to rotate around

    [SerializeField] private float angleLimit = 45f; // Symmetric angle limit (degrees)
    [SerializeField] private float smoothingSpeed = 10f; // Smoothing speed for rotation

    [Header("Event Thresholds")] [SerializeField]
    private float eventThresholdAngle = 30f; // Angle at which to fire events

    [SerializeField] private UnityEvent onSideA;
    [SerializeField] private UnityEvent onSideB;
    [SerializeField] private UnityEvent onReturnToCenter;

    [Header("Gizmo Settings")] [SerializeField]
    private Color gizmoColor = Color.green;

    [SerializeField] private float gizmoRadius = 0.2f;
    [SerializeField] private bool showGizmoArc = true;

    // References
    private Transform originalParent;
    private Quaternion initialLocalRotation;
    public Transform grabPoint;
    private Rigidbody rb;

    // Runtime variables
    private float currentAngle;
    private float targetAngle;
    private Vector3 lastInteractorPosition;
    private bool isBeingGrabbed;

    // Event tracking
    private bool wasInPositiveThreshold;
    private bool wasInNegativeThreshold;
    private bool wasCentered = true;

    protected override void Awake()
    {
        base.Awake();

        // Store original parent and rotation
        originalParent = transform.parent;
        initialLocalRotation = transform.localRotation;


        if (grabPoint == null) Debug.LogWarning("No grabPoint found as child of lever handle!");

        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // Configure Rigidbody for kinematic movement
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePosition; // Only allow rotation

        // Configure XRGrabInteractable settings
        movementType = MovementType.Kinematic;
        attachTransform = grabPoint;
        throwOnDetach = false;
        trackRotation = false;
        trackPosition = false;

        // Initialize angle
        currentAngle = 0f;
        targetAngle = 0f;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        isBeingGrabbed = true;

        // Store initial interactor position
        if (args.interactorObject.transform != null) lastInteractorPosition = args.interactorObject.transform.position;

        // Ensure proper parent relationship
        if (transform.parent != originalParent && originalParent != null) transform.SetParent(originalParent, true);
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
        if (isBeingGrabbed && isSelected) UpdateLeverRotation();

        // Apply smoothing
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothingSpeed);

        // Clamp to limits
        currentAngle = Mathf.Clamp(currentAngle, -angleLimit, angleLimit);

        ApplyRotation();
        CheckThresholdEvents();
    }

    private void UpdateLeverRotation()
    {
        if (firstInteractorSelecting == null || grabPoint == null) return;

        // Get current interactor position
        var currentInteractorPosition = firstInteractorSelecting.transform.position;

        // Calculate the rotation based on hand movement
        var leverPivotWorld = transform.position;
        var grabPointWorld = grabPoint.position;

        // Vector from pivot to grab point
        var leverArmInitial = grabPointWorld - leverPivotWorld;

        // Vector from pivot to current hand position
        var leverArmCurrent = currentInteractorPosition - leverPivotWorld;

        // Project both vectors onto the plane perpendicular to rotation axis
        var worldRotationAxis = transform.TransformDirection(rotationAxis.normalized);
        leverArmInitial = Vector3.ProjectOnPlane(leverArmInitial, worldRotationAxis);
        leverArmCurrent = Vector3.ProjectOnPlane(leverArmCurrent, worldRotationAxis);

        // Calculate angle between vectors
        var angle = Vector3.SignedAngle(leverArmInitial, leverArmCurrent, worldRotationAxis);

        // Update target angle
        targetAngle = Mathf.Clamp(currentAngle + angle, -angleLimit, angleLimit);

        // Update last position
        lastInteractorPosition = currentInteractorPosition;
    }

    private void ApplyRotation()
    {
        // Calculate rotation quaternion
        var rotationDelta = Quaternion.AngleAxis(currentAngle, rotationAxis);
        var newLocalRotation = initialLocalRotation * rotationDelta;

        // Apply rotation
        transform.localRotation = newLocalRotation;

        // Sync rigidbody if needed
        if (rb != null) rb.rotation = transform.rotation;
    }

    private void CheckThresholdEvents()
    {
        var inPositiveThreshold = currentAngle >= eventThresholdAngle;
        var inNegativeThreshold = currentAngle <= -eventThresholdAngle;
        var isCentered = Mathf.Abs(currentAngle) < eventThresholdAngle * 0.5f; // 50% of threshold for center zone

        // Check positive threshold
        if (inPositiveThreshold && !wasInPositiveThreshold) onSideA?.Invoke();

        // Check negative threshold
        if (inNegativeThreshold && !wasInNegativeThreshold) onSideB?.Invoke();

        // Check return to center
        if (isCentered && !wasCentered) onReturnToCenter?.Invoke();

        // Update state
        wasInPositiveThreshold = inPositiveThreshold;
        wasInNegativeThreshold = inNegativeThreshold;
        wasCentered = isCentered;
    }

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        return grabPoint != null ? grabPoint : base.GetAttachTransform(interactor);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (!isBeingGrabbed) base.ProcessInteractable(updatePhase);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Get the transform to use (handle parent relationships correctly)
        var parentTransform = Application.isPlaying ? originalParent : transform.parent;
        var baseRotation = Application.isPlaying ? initialLocalRotation : transform.localRotation;

        // Calculate world space rotation axis
        var worldAxis = transform.TransformDirection(rotationAxis.normalized);
        var center = transform.position;

        // Draw rotation axis
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center - worldAxis * gizmoRadius * 0.5f, center + worldAxis * gizmoRadius * 0.5f);

        // Get a perpendicular vector in world space
        var worldPerpendicular = GetPerpendicularVector(worldAxis);

        // Draw current angle
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            DrawRotationGizmo(center, worldAxis, worldPerpendicular, currentAngle, gizmoRadius * 0.8f);
        }

        // Draw limits
        Gizmos.color = gizmoColor;
        DrawRotationGizmo(center, worldAxis, worldPerpendicular, angleLimit, gizmoRadius);
        DrawRotationGizmo(center, worldAxis, worldPerpendicular, -angleLimit, gizmoRadius);

        // Draw threshold angles
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        DrawRotationGizmo(center, worldAxis, worldPerpendicular, eventThresholdAngle, gizmoRadius * 0.9f);
        DrawRotationGizmo(center, worldAxis, worldPerpendicular, -eventThresholdAngle, gizmoRadius * 0.9f);

        // Draw arc showing range
        if (showGizmoArc) DrawArc(center, worldAxis, worldPerpendicular, -angleLimit, angleLimit, gizmoRadius, 20);
    }

    private Vector3 GetPerpendicularVector(Vector3 axis)
    {
        // Find a perpendicular vector that accounts for parent rotation
        Vector3 perpendicular;

        if (Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.9f)
            perpendicular = Vector3.Cross(axis, Vector3.up).normalized;
        else
            perpendicular = Vector3.Cross(axis, Vector3.forward).normalized;

        return perpendicular;
    }

    private void DrawRotationGizmo(Vector3 center, Vector3 axis, Vector3 perpendicular, float angle, float radius)
    {
        // Rotate perpendicular by angle around axis
        var rotation = Quaternion.AngleAxis(angle, axis);
        var direction = rotation * perpendicular;

        // Draw line from center
        Gizmos.DrawLine(center, center + direction * radius);

        // Draw small sphere at end
        Gizmos.DrawSphere(center + direction * radius, radius * 0.05f);
    }

    private void DrawArc(Vector3 center, Vector3 axis, Vector3 perpendicular, float startAngle, float endAngle,
        float radius, int segments)
    {
        var angleStep = (endAngle - startAngle) / segments;
        var lastPoint = center + Quaternion.AngleAxis(startAngle, axis) * perpendicular * radius;

        for (var i = 1; i <= segments; i++)
        {
            var currentAngleInArc = startAngle + angleStep * i;
            var currentPoint = center + Quaternion.AngleAxis(currentAngleInArc, axis) * perpendicular * radius;
            Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }
#endif

    // Helper methods
    public void ResetToCenter()
    {
        targetAngle = 0f;
        currentAngle = 0f;
        ApplyRotation();
    }

    public void SetNormalizedAngle(float normalizedAngle)
    {
        targetAngle = Mathf.Clamp01(normalizedAngle) * angleLimit * 2f - angleLimit;
    }

    public float GetNormalizedAngle()
    {
        return (currentAngle + angleLimit) / (angleLimit * 2f);
    }

    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}