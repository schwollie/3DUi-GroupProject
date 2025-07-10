using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class SnapAnchor : MonoBehaviour
{
    [Serializable]
    public class SnapEvent
    {
        [Tooltip("The specific Snapable object that will trigger this event.")]
        public Snapable specificSnapable;

        [Tooltip("The event to fire when the specific Snapable is snapped.")]
        public UnityEvent onSnap;
    }

    [Header("Snapping Configuration")]
    [Tooltip("The transform where the snapped object will be moved. If empty, it will use this object's transform.")]
    [SerializeField]
    private Transform snapTarget;

    [Header("Rotation Snapping")] [SerializeField]
    private bool rotationSnapX = true;

    [SerializeField] private bool rotationSnapY = true;
    [SerializeField] private bool rotationSnapZ = true;

    [Header("Can Leave Snap")]
    [Tooltip("If false, once an object is snapped, it cannot be removed by grabbing it.")]
    [SerializeField]
    private bool allowLeaveSnap = true;

    [Header("Animation")] [Tooltip("How long the snapping animation should take in seconds.")] [SerializeField]
    private float animationDuration = 0.25f;

    [Header("Filtering")] [Tooltip("Which types of Snapable objects will this anchor accept?")] [SerializeField]
    private List<Snapable.SnapableType> acceptedTypes = new();

    [Header("Events")]
    [Tooltip("Events for specific Snapable objects. These override the default event.")]
    [SerializeField]
    private List<SnapEvent> specificEvents;

    [Tooltip("The default event for any accepted Snapable that isn't in the specific list.")] [SerializeField]
    private UnityEvent defaultOnSnapEvent;

    [Tooltip("Event which is always called additionally when any object enters the snap.")] [SerializeField]
    public UnityEvent onSnapEnter;

    [Tooltip("Event fired when a snapped object is pulled out from the anchor.")]
    public UnityEvent onSnapExit;

    private bool isBusy;
    private Snapable lastSnapped;
    public Snapable currentSnappedObject { get; private set; }

    private void Awake()
    {
        if (snapTarget == null) snapTarget = transform;
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("SnapAnchor's collider was not set to 'Is Trigger'. Forcing it now.", this);
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentSnappedObject != null || isBusy) return;

        var snapable = other.GetComponent<Snapable>();
        if (snapable == null) return;

        if (snapable.grabInteractable.isSelected && acceptedTypes.Contains(snapable.type))
            StartCoroutine(AnimateSnap(snapable));
    }

    private IEnumerator AnimateSnap(Snapable snapable, bool triggerEvents = true)
    {
        isBusy = true;
        currentSnappedObject = snapable;

        var grabInteractable = snapable.grabInteractable;
        var rb = snapable.GetComponent<Rigidbody>();

        // Force drop the object
        grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);

        // Temporarily make kinematic for smooth animation
        rb.isKinematic = true;

        var objectTransform = snapable.transform;
        var startPosition = objectTransform.position;
        var startRotation = objectTransform.rotation;

        // Calculate target rotation
        var targetEuler = snapTarget.rotation.eulerAngles;
        var currentEuler = startRotation.eulerAngles;
        var finalEuler = new Vector3(
            rotationSnapX ? targetEuler.x : currentEuler.x,
            rotationSnapY ? targetEuler.y : currentEuler.y,
            rotationSnapZ ? targetEuler.z : currentEuler.z
        );
        var finalRotation = Quaternion.Euler(finalEuler);

        snapable.OnSnap();

        // Animate to snap position
        var elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            objectTransform.position =
                Vector3.Lerp(startPosition, snapTarget.position, elapsedTime / animationDuration);
            objectTransform.rotation = Quaternion.Slerp(startRotation, finalRotation, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize position
        objectTransform.position = snapTarget.position;
        objectTransform.rotation = finalRotation;

        // Re-enable physics but with constraints
        rb.isKinematic = false;


        // Listen for grab if allowed
        if (allowLeaveSnap) grabInteractable.selectEntered.AddListener(OnSnapableGrabbed);

        lastSnapped = snapable;

        if (triggerEvents)
        {
            var specificEvent = specificEvents.FirstOrDefault(e => e.specificSnapable == snapable);
            if (specificEvent != null && specificEvent.onSnap != null)
                specificEvent.onSnap.Invoke();
            else
                defaultOnSnapEvent.Invoke();
            onSnapEnter.Invoke();
        }

        isBusy = false;
    }

    private void OnSnapableGrabbed(SelectEnterEventArgs args)
    {
        if (currentSnappedObject == null) return;

        // Unsubscribe immediately to prevent multiple calls
        currentSnappedObject.grabInteractable.selectEntered.RemoveListener(OnSnapableGrabbed);

        // Unsnap the object
        currentSnappedObject.OnUnsnap();

        // Fire exit event
        onSnapExit.Invoke();

        // Clear references
        lastSnapped = null;
        currentSnappedObject = null;
    }

    private void OnTriggerExit(Collider other)
    {
        // This is now just a backup check, main unsnapping happens in OnSnapableGrabbed
        if (lastSnapped != null && other.gameObject == lastSnapped.gameObject &&
            lastSnapped.grabInteractable.isSelected && currentSnappedObject == null)
            lastSnapped = null;
    }

    public void Reset()
    {
        // Stop any ongoing animations
        StopAllCoroutines();

        if (currentSnappedObject != null)
        {
            // Remove event listener
            currentSnappedObject.grabInteractable.selectEntered.RemoveListener(OnSnapableGrabbed);

            // Force drop if currently held
            if (currentSnappedObject.grabInteractable.isSelected)
                currentSnappedObject.grabInteractable.interactionManager.CancelInteractableSelection(
                    (IXRSelectInteractable)currentSnappedObject.grabInteractable);

            // Unsnap the object (restore physics)
            currentSnappedObject.OnUnsnap();

            // Clear references
            currentSnappedObject = null;
            lastSnapped = null;
        }

        // Reset busy flag
        isBusy = false;
    }

    public void AllowLeave(bool allow)
    {
        allowLeaveSnap = allow;
        if (allow && currentSnappedObject != null)
            currentSnappedObject.grabInteractable.selectEntered.AddListener(OnSnapableGrabbed);
        else if (!allow && currentSnappedObject != null)
            currentSnappedObject.grabInteractable.selectEntered.RemoveListener(OnSnapableGrabbed);
    }
}