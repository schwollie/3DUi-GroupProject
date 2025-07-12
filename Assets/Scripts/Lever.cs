// --- CORRECTED SCRIPT ---
// Name this script "LeverController.cs" and place it on the PIVOT object.

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Required for the Colliders list

[RequireComponent(typeof(Rigidbody))]
public class LeverController : XRGrabInteractable
{
    [Header("Lever Setup")]
    [Tooltip("The child object with the collider that the user will actually grab.")]
    [SerializeField]
    private Transform handle;

    [Header("Lever Settings")] [SerializeField]
    private RotationAxis rotationAxis = RotationAxis.Z;

    [Header("Rotation Limits")]
    [Tooltip("Sets the lever's min/max limits symmetrically. E.g., 45 means -45 to +45 degrees.")]
    [Range(0, 180)]
    [SerializeField]
    private float symmetricLimit = 45f;

    [Tooltip("Offset angle for the zero position. Use this to adjust where the lever's center/neutral position is.")]
    [Range(-180, 180)]
    [SerializeField]
    private float zeroAngleOffset = 0f;


    [Header("Events")]
    [Tooltip("How close to the min/max angle (in degrees) the lever must be to trigger events.")]
    [SerializeField]
    private float eventTriggerThreshold = 5f;

    public UnityEvent onSideA; // Event for reaching the minimum angle
    public UnityEvent onSideB; // Event for reaching the maximum angle

    [Header("Movement Settings")] [Tooltip("Rotation speed in degrees per second for large movements")] [SerializeField]
    private float rotationSpeed = 90f;

    [Tooltip("Use smoothing (lerp) only when within this many degrees of target")] [SerializeField]
    private float lerpThreshold = 5f;

    [Tooltip("Lerp speed when very close to target (higher = faster)")] [SerializeField]
    private float lerpSpeed = 10f;

    // Private fields
    private float currentAngle;
    private bool hasTriggeredSideA;
    private bool hasTriggeredSideB;
    private float initialLeverAngle;
    private float initialInteractorAngle;

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    protected override void Awake()
    {
        base.Awake();
        // --- FIX: Ensure the interactable knows which collider to use for grabbing ---
        // If you haven't assigned colliders in the Inspector, this will find the one on the handle.
        if (colliders.Count == 0 && handle != null)
        {
            var handleCollider = handle.GetComponent<Collider>();
            if (handleCollider != null)
                colliders.Add(handleCollider);
            else
                Debug.LogError($"Lever 'Handle' ({handle.name}) is missing a Collider component!", this);
        }

        ConfigureRigidbody();

        // We will handle the rotation ourselves in ProcessInteractable
        trackPosition = false;
        trackRotation = false;
        movementType = MovementType.Instantaneous;
    }

    private void Start()
    {
        // Set initial angle based on the pivot's current rotation
        currentAngle = GetCurrentAngleFromTransform();
        ApplyRotation(currentAngle);
    }

    private void ConfigureRigidbody()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        // Capture initial state for relative rotation
        initialLeverAngle = currentAngle;
        var interactorPos = args.interactorObject.transform.position;
        initialInteractorAngle = CalculateInteractorAngle(interactorPos);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected) UpdateLeverRotation();
            CheckForEvents();
        }
    }


    private void UpdateLeverRotation()
    {
        var interactor = firstInteractorSelecting;
        if (interactor == null) return;

        // Calculate rotation based on the change from the initial grab point
        var currentInteractorAngle = CalculateInteractorAngle(interactor.transform.position);
        var angleDelta = Mathf.DeltaAngle(initialInteractorAngle, currentInteractorAngle);

        var targetAngle = initialLeverAngle + angleDelta;
        targetAngle = Mathf.Clamp(targetAngle, -symmetricLimit, symmetricLimit);

        // Calculate how far we are from the target
        var angleDistance = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        // Choose movement method based on distance
        if (angleDistance <= lerpThreshold)
            // Very close to target - use lerp for smooth final approach
            // This prevents micro-jittering when the hand is relatively still
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * lerpSpeed);
        else
            // Far from target - use constant speed movement to prevent jumping
            // This ensures smooth, predictable movement for large rotations
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        ApplyRotation(currentAngle);
    }


    private float CalculateInteractorAngle(Vector3 interactorPosition)
    {
        // --- FIX: The direction is now from this pivot's position, not the handle's ---
        var localDirection = transform.InverseTransformPoint(interactorPosition).normalized;
        var angle = 0f;

        switch (rotationAxis)
        {
            case RotationAxis.X:
                angle = -Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;
                break;
            case RotationAxis.Y:
                angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                break;
            case RotationAxis.Z:
                angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
                break;
        }

        return angle;
    }

    private void ApplyRotation(float angle)
    {
        // --- FIX: Rotate this object (the pivot), not the handle directly. ---
        // The handle will move correctly because it is a child of the pivot.
        var rotationVector = Vector3.zero;
        rotationVector[(int)rotationAxis] = angle + zeroAngleOffset;
        transform.localRotation = Quaternion.Euler(rotationVector);
    }

    private float GetCurrentAngleFromTransform()
    {
        // --- FIX: Read the angle from this pivot object ---
        var currentEuler = transform.localRotation.eulerAngles;
        var angle = currentEuler[(int)rotationAxis];
        if (angle > 180) angle -= 360;
        return angle - zeroAngleOffset;
    }

    private void CheckForEvents()
    {
        if (currentAngle <= -symmetricLimit + eventTriggerThreshold)
        {
            if (!hasTriggeredSideA)
            {
                Debug.Log($"Lever triggered on Side A at angle {currentAngle}");
                onSideA?.Invoke();
                hasTriggeredSideA = true;
                hasTriggeredSideB = false;
            }
        }
        else if (currentAngle >= symmetricLimit - eventTriggerThreshold)
        {
            if (!hasTriggeredSideB)
            {
                Debug.Log($"Lever triggered on Side A at angle {currentAngle}");
                onSideB?.Invoke();
                hasTriggeredSideB = true;
                hasTriggeredSideA = false;
            }
        }
        else
        {
            hasTriggeredSideA = false;
            hasTriggeredSideB = false;
        }
    }

#if UNITY_EDITOR
    // This special method is called by the Unity Editor whenever the object is selected.
    // It allows us to draw visual helpers (gizmos) in the scene view.
    private void OnDrawGizmosSelected()
    {
        // Get the pivot point and the axis of rotation in world space
        var pivotPosition = transform.position;
        var worldAxis = Vector3.forward;
        switch (rotationAxis)
        {
            case RotationAxis.X:
                worldAxis = transform.right;
                break;
            case RotationAxis.Y:
                worldAxis = transform.up;
                break;
            case RotationAxis.Z:
                worldAxis = transform.forward;
                break;
        }

        // Use the distance to the handle to set the radius of our visual arc
        var gizmoRadius = handle != null ? Vector3.Distance(pivotPosition, handle.position) : 1f;

        // --- Draw the Arc for the entire range of motion ---
        // We'll use the UnityEditor.Handles class for more advanced drawing options.
        Handles.color = new Color(1, 0.5f, 0, 0.1f); // A semi-transparent orange

        // Calculate the starting direction for the arc, including the min limit and the zero offset
        var minRotation = Quaternion.AngleAxis(-symmetricLimit + zeroAngleOffset, worldAxis);
        var arcStartDirection = minRotation * transform.up; // A default "up" direction to rotate from

        // Draw the solid arc representing the full range
        Handles.DrawSolidArc(
            pivotPosition, // Center of the arc
            worldAxis, // Axis to rotate around
            arcStartDirection, // Vector to start drawing from
            symmetricLimit * 2, // Total angle of the arc
            gizmoRadius // Radius of the arc
        );

        // --- Draw a line indicating the current angle (only works in Play Mode) ---
        if (Application.isPlaying)
        {
            Handles.color = Color.yellow;
            var currentRotation = Quaternion.AngleAxis(currentAngle + zeroAngleOffset, worldAxis);
            var currentDirection = currentRotation * transform.up;
            // Make the line thicker for better visibility
            Handles.DrawLine(pivotPosition, pivotPosition + currentDirection * gizmoRadius, 4f);
        }
    }
#endif
}