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
    private UnityEvent onSnapEnter;

    [Tooltip("Event fired when a snapped object is pulled out from the anchor.")]
    public UnityEvent onSnapExit;

    private Snapable currentlySnappedObject;
    private bool isBusy; // Used to lock the anchor during snap/unsnap animations

    private Snapable lastSnapped;

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
        if (currentlySnappedObject != null || isBusy) return;

        var snapable = other.GetComponent<Snapable>();
        if (snapable == null) return;

        // We only snap if the object is currently being held and is of an accepted type.
        if (snapable.grabInteractable.isSelected && acceptedTypes.Contains(snapable.type))
            StartCoroutine(AnimateSnap(snapable));
    }

    private IEnumerator AnimateSnap(Snapable snapable, bool triggerEvents = true)
    {
        isBusy = true;
        currentlySnappedObject = snapable;

        var grabInteractable = snapable.grabInteractable;

        // --- KEY CHANGE: Force the controller to drop the object ---
        // This detaches the object from the hand, allowing it to snap freely.
        grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);

        var objectTransform = snapable.transform;
        var startPosition = objectTransform.position;
        var startRotation = objectTransform.rotation;

        // Calculate target rotation based on snap settings
        var targetEuler = snapTarget.rotation.eulerAngles;
        var currentEuler = startRotation.eulerAngles;
        var finalEuler = new Vector3(
            rotationSnapX ? targetEuler.x : currentEuler.x,
            rotationSnapY ? targetEuler.y : currentEuler.y,
            rotationSnapZ ? targetEuler.z : currentEuler.z
        );
        var finalRotation = Quaternion.Euler(finalEuler);

        // Animate to snap position over the specified duration.
        var elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            objectTransform.position =
                Vector3.Lerp(startPosition, snapTarget.position, elapsedTime / animationDuration);
            objectTransform.rotation = Quaternion.Slerp(startRotation, finalRotation, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- Finalize Snap State ---
        objectTransform.position = snapTarget.position;
        objectTransform.rotation = finalRotation;

        // NO PARENTING: This prevents scale issues.
        // objectTransform.SetParent(snapTarget); // This line is removed.

        snapable.OnSnap(); // Make the object kinematic so it stays in place.

        // If the object can be removed, listen for a NEW grab event.
        if (allowLeaveSnap)
        {
            grabInteractable.selectEntered.AddListener(OnSnapableReGrabbed);
            grabInteractable.selectExited.AddListener(OnSnapableLeave);
        }

        lastSnapped = snapable;

        if (triggerEvents)
        {
            // Trigger all relevant snap events.
            var specificEvent = specificEvents.FirstOrDefault(e => e.specificSnapable == snapable);
            if (specificEvent != null && specificEvent.onSnap != null)
                specificEvent.onSnap.Invoke();
            else
                defaultOnSnapEvent.Invoke();
            onSnapEnter.Invoke();
        }

        isBusy = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (lastSnapped != null && other.gameObject == lastSnapped.gameObject &&
            lastSnapped.grabInteractable.isSelected)
        {
            // Re-enable physics. The XR system will handle moving it to the hand.
            currentlySnappedObject.OnUnsnap();

            // Fire the exit event.
            onSnapExit.Invoke();

            lastSnapped = null;
            currentlySnappedObject = null;
        }
    }

    private void OnSnapableLeave(SelectExitEventArgs args)
    {
        if (currentlySnappedObject != null)
        {
            currentlySnappedObject.grabInteractable.selectExited.RemoveListener(OnSnapableLeave);
            StartCoroutine(AnimateSnap(currentlySnappedObject, false));
        }
    }

    private void OnSnapableReGrabbed(SelectEnterEventArgs args)
    {
        // Check if the object being grabbed is the one currently snapped.
        if (currentlySnappedObject == null ||
            args.interactableObject.transform != currentlySnappedObject.transform) return;

        // Object was re-grabbed, perform immediate unsnap.
        var grabInteractable = currentlySnappedObject.grabInteractable;

        // Stop listening to the event to prevent memory leaks.
        grabInteractable.selectEntered.RemoveListener(OnSnapableReGrabbed);
    }
}