using UnityEngine;


/// <summary>
/// Prevents teleportation when the controller is inside a wall or other geometry.
/// It works by checking for a clear line of sight between the player's head and the controller.
/// If the line of sight is blocked by an object on the Occlusion Layers, the teleport interactor is disabled.
/// </summary>
public class WallClipPrevention : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The XR Ray Interactor responsible for teleportation. This will be enabled/disabled.")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor teleportRayInteractor;

    [Tooltip("The main camera representing the player's head/eyes.")]
    [SerializeField] private Transform playerCameraTransform;

    [Tooltip("The transform of this controller, used as the line-of-sight target.")]
    [SerializeField] private Transform controllerTransform;

    [Header("Occlusion Settings")]
    [Tooltip("Set this to the layer(s) that should block the teleport ray (e.g., 'Walls', 'Environment').")]
    [SerializeField] private LayerMask occlusionLayers;

    private void Update()
    {
        // Safety check to ensure all references are assigned in the Inspector.
        if (teleportRayInteractor == null || playerCameraTransform == null || controllerTransform == null)
        {
            return;
        }

        // Perform a linecast from the player's head to their hand.
        // This checks if any object on the occlusionLayers is blocking the path.
        bool isOccluded = Physics.Linecast(
            playerCameraTransform.position, 
            controllerTransform.position, 
            occlusionLayers
        );

        // The teleport ray should only be enabled if the line of sight is NOT occluded.
        bool shouldBeEnabled = !isOccluded;

        // To avoid setting the value every frame, only change it if the state is different.
        if (teleportRayInteractor.enabled != shouldBeEnabled)
        {
            teleportRayInteractor.enabled = shouldBeEnabled;
        }
    }
}
