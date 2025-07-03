using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Rigidbody))]
public class DrawerGrabbable : XRGrabInteractable
{
    [Header("Drawer Settings")] [SerializeField]
    private float pullAxis = 2; // 0=X, 1=Y, 2=Z

    [SerializeField] private float minPosition = 0f;
    [SerializeField] private float maxPosition = 0.3f;

    [Header("Movement Settings")] [SerializeField]
    private float smoothSpeed = 15f;

    [SerializeField] private bool instantStop = true;

    private Vector3 startLocalPosition;
    private Vector3 grabOffset;
    private Rigidbody rb;
    private bool isBeingGrabbed;
    private Transform originalParent;
    private IXRSelectInteractor currentInteractor;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();

        // Store initial state
        originalParent = transform.parent;
        if (originalParent != null)
        {
            startLocalPosition = transform.localPosition;
        }
        else
        {
            Debug.LogError($"DrawerGrabbable on {gameObject.name} requires a parent transform!");
            startLocalPosition = transform.position;
        }

        // Configure rigidbody for drawer behavior
        ConfigureRigidbody();

        // Override XR Grab Interactable settings
        movementType = MovementType.Instantaneous;
        trackPosition = false;
        trackRotation = false;
        throwOnDetach = false;
    }

    private void ConfigureRigidbody()
    {
        rb.isKinematic = true; // We'll handle movement manually
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Freeze all rotations
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Also freeze position on non-pull axes
        if (pullAxis != 0) rb.constraints |= RigidbodyConstraints.FreezePositionX;
        if (pullAxis != 1) rb.constraints |= RigidbodyConstraints.FreezePositionY;
        if (pullAxis != 2) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Store the interactor
        currentInteractor = args.interactorObject;
        isBeingGrabbed = true;

        // Calculate grab offset in local space if we have a parent
        if (originalParent != null)
        {
            var interactorWorldPos = args.interactorObject.transform.position;
            var interactorLocalPos = originalParent.InverseTransformPoint(interactorWorldPos);
            grabOffset = transform.localPosition - interactorLocalPos;
        }
        else
        {
            grabOffset = Vector3.zero;
        }

        // Ensure we're kinematic during grab
        rb.isKinematic = true;
        base.OnSelectEntered(args);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        isBeingGrabbed = false;
        currentInteractor = null;

        if (instantStop && rb != null)
        {
            // Since we're kinematic, we don't need to set velocity
            // The drawer will just stop where it is
        }

        base.OnSelectExited(args);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        // We override the base processing but still need to call it for other functionality
        base.ProcessInteractable(updatePhase);

        if (isBeingGrabbed && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed) UpdateDrawerPosition();
    }

    private void UpdateDrawerPosition()
    {
        if (!isBeingGrabbed || currentInteractor == null) return;

        // Get interactor position
        var interactorWorldPos = currentInteractor.transform.position;

        if (originalParent != null)
        {
            // Convert to parent's local space
            var interactorLocalPos = originalParent.InverseTransformPoint(interactorWorldPos);

            // Calculate target position
            var targetPosition = transform.localPosition;

            // Apply movement only on the pull axis
            var axisIndex = Mathf.RoundToInt(pullAxis);
            var desiredAxisPosition = interactorLocalPos[axisIndex] + grabOffset[axisIndex];

            // Clamp to limits
            desiredAxisPosition = Mathf.Clamp(
                desiredAxisPosition,
                startLocalPosition[axisIndex] + minPosition,
                startLocalPosition[axisIndex] + maxPosition
            );

            targetPosition[axisIndex] = desiredAxisPosition;

            // Ensure other axes remain at start position
            for (var i = 0; i < 3; i++)
                if (i != axisIndex)
                    targetPosition[i] = startLocalPosition[i];

            // Apply position smoothly
            if (smoothSpeed > 0)
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    targetPosition,
                    Time.fixedDeltaTime * smoothSpeed
                );
            else
                transform.localPosition = targetPosition;
        }
        else
        {
            // Fallback for no parent (shouldn't happen with proper setup)
            Debug.LogWarning("Drawer has no parent - movement may be incorrect!");
        }

        // Force rotation to stay exactly the same
        transform.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        // Extra safety to ensure rotation never changes
        if (transform.localRotation != Quaternion.identity) transform.localRotation = Quaternion.identity;
    }

    // Override to prevent the default grab behavior
    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        // Always selectable unless you want to add conditions
        return base.IsSelectableBy(interactor);
    }

    // Public method to reset drawer position
    public void ResetPosition()
    {
        transform.localPosition = startLocalPosition;
        transform.localRotation = Quaternion.identity;
    }

    // Public method to check if drawer is open
    public bool IsOpen()
    {
        var axisIndex = Mathf.RoundToInt(pullAxis);
        var currentAxisPosition = transform.localPosition[axisIndex];
        var startAxisPosition = startLocalPosition[axisIndex];
        return Mathf.Abs(currentAxisPosition - startAxisPosition) > 0.01f;
    }

    // Get normalized open amount (0 = closed, 1 = fully open)
    public float GetOpenAmount()
    {
        var axisIndex = Mathf.RoundToInt(pullAxis);
        var currentAxisPosition = transform.localPosition[axisIndex];
        var startAxisPosition = startLocalPosition[axisIndex];
        var currentOffset = currentAxisPosition - startAxisPosition;
        return Mathf.Clamp01(currentOffset / maxPosition);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var startPos = Application.isPlaying
            ? originalParent != null ? originalParent.TransformPoint(startLocalPosition) : startLocalPosition
            : transform.position;

        var axis = Mathf.RoundToInt(pullAxis);
        var direction = axis == 0 ? transform.right : axis == 1 ? transform.up : transform.forward;

        var minPos = startPos;
        var maxPos = startPos + direction * maxPosition;

        // Draw the movement range
        Gizmos.color = Color.green;
        Gizmos.DrawLine(minPos, maxPos);
        Gizmos.DrawWireCube(minPos, Vector3.one * 0.05f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(maxPos, Vector3.one * 0.05f);

        // Draw current position if playing
        if (Application.isPlaying && isBeingGrabbed)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
#endif
}