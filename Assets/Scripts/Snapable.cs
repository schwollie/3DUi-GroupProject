using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// This ensures the object has the necessary components to be grabbed and have physics.
[RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(XRGrabInteractable))]
public class Snapable : MonoBehaviour
{
    // Define the different types of objects that can be snapped.
    public enum SnapableType
    {
        Key,
        PowerCell
    }

    [Tooltip("The type of this snapable object.")]
    public SnapableType type;

    // Public reference to the grab interactable component.
    [HideInInspector] public XRGrabInteractable grabInteractable;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    /// <summary>
    ///     Called by the SnapAnchor when this object is successfully snapped.
    ///     This makes the object static but leaves it interactable.
    /// </summary>
    public void OnSnap()
    {
        //rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    /// <summary>
    ///     Called by the SnapAnchor when this object is removed (grabbed).
    ///     This re-enables physics.
    /// </summary>
    public void OnUnsnap()
    {
        //rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.isKinematic = false;
        //grabInteractable.selectExited.AddListener(args => { GetComponent<Rigidbody>().isKinematic = false; });
    }
}