using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// This script requires the GameObject to have a Rigidbody component.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ScaleWeight : MonoBehaviour
{
    [Tooltip("The weight value of this object. This will also be set as the Rigidbody's mass.")]
    public float weightValue = 1.0f;

    // Public property to hold a reference to the interactable component
    public XRGrabInteractable Interactable { get; private set; }

    private Rigidbody rb;

    // Awake is called when the script instance is being loaded.
    private void Awake()
    {
        // Get the Rigidbody component attached to this same GameObject.
        rb = GetComponent<Rigidbody>();

        Interactable = GetComponent<XRGrabInteractable>();

        // Set the mass of the Rigidbody to match the specified weight value.
        // This makes the physics simulation aware of the object's weight.
        if (rb != null)
            rb.mass = weightValue;
        else
            Debug.LogError("ScaleWeight script on " + gameObject.name +
                           " requires a Rigidbody component, but none was found.");
    }
}