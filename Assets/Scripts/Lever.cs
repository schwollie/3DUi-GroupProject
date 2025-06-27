using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Lever : MonoBehaviour
{
    [Tooltip("How close to the min/max angle (in degrees) the lever must be to trigger events")]
    public float eventTriggerTreshold = 10f;

    [Header("Events")] public UnityEvent onSideA; // Multiple events for reaching the minimum angle
    public UnityEvent onSideB; // Multiple events for reaching the maximum angle

    [Header("Anchor")] [SerializeField] private GameObject anchor;

    [Header("Hinge Configuration")]
    [Tooltip("The HingeJoint component, usually on a child object like 'Bar'.")]
    [SerializeField]
    private HingeJoint hinge;

    private Rigidbody barRigidbody;

    [Header("Hinge Limits")]
    [Tooltip("Sets the hinge's min/max limits symmetrically. E.g., 45 means -45 to +45 degrees.")]
    [Range(0, 180)]
    public float symmetricLimit = 45f;

    // State tracking
    private bool isBeingHeld;
    private bool hasTriggeredSideA;
    private bool hasTriggeredSideB;

    private void OnEnable()
    {
        FindAndSetupHinge();
    }

    private void Update()
    {
        if (hinge == null) return;

        // Runtime-only logic
        if (Application.isPlaying)
            CheckForEvents();
    }

    /// <summary>
    ///     Checks if the lever is near its limits and triggers events.
    /// </summary>
    private void CheckForEvents()
    {
        if (isBeingHeld) return;

        var currentAngle = hinge.angle;
        var minLimit = hinge.limits.min;
        var maxLimit = hinge.limits.max;

        // Check for Side A (minimum limit)
        if (Mathf.Abs(currentAngle - minLimit) < eventTriggerTreshold)
        {
            if (!hasTriggeredSideA)
            {
                onSideA?.Invoke();
                hasTriggeredSideA = true;
                hasTriggeredSideB = false;
            }
        }
        // Check for Side B (maximum limit)
        else if (Mathf.Abs(currentAngle - maxLimit) < eventTriggerTreshold)
        {
            if (!hasTriggeredSideB)
            {
                onSideB?.Invoke();
                hasTriggeredSideB = true;
                hasTriggeredSideA = false;
            }
        }
        else
        {
            // Reset flags if the lever is in the middle, allowing events to be re-triggered
            hasTriggeredSideA = false;
            hasTriggeredSideB = false;
        }
    }

    /// <summary>
    ///     Sets the hinge limits symmetrically around zero.
    /// </summary>
    public void SetSymmetricLimits(float limit)
    {
        if (hinge == null) return;
        var limits = hinge.limits;
        limits.min = -Mathf.Abs(limit);
        limits.max = Mathf.Abs(limit);
        hinge.limits = limits;
    }

    /// <summary>
    ///     Call this from the XR Grab Interactable's "Select Entered" event.
    /// </summary>
    public void OnLeverGrabbed()
    {
        isBeingHeld = true;
    }

    /// <summary>
    ///     Call this from the XR Grab Interactable's "Select Exited" event.
    /// </summary>
    public void OnLeverReleased()
    {
        isBeingHeld = false;
    }

    /// <summary>
    ///     This function is called in the editor when the script is loaded or a value is changed in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        if (hinge == null) FindAndSetupHinge();
        SetSymmetricLimits(symmetricLimit);
    }

    /// <summary>
    ///     Finds the HingeJoint in children and sets up references.
    /// </summary>
    private void FindAndSetupHinge()
    {
        if (hinge == null) hinge = GetComponentInChildren<HingeJoint>(true);

        if (hinge != null)
        {
            barRigidbody = hinge.GetComponent<Rigidbody>();
        }
        else
        {
            if (Application.isPlaying)
                Debug.LogWarning("Lever could not find a HingeJoint in its children.", this);
        }
    }
}