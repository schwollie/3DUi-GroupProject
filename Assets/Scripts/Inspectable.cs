using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using DG.Tweening;

public class InspectionController : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Assign the controller interactor (e.g., RightHand Controller) here.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor interactor;

    [Header("Input")]
    [Tooltip("Reference to the input action for rotation. Use the 'Turn' action from the 'XRI Right Locomotion' map.")]
    public InputActionReference rotateInputAction;

    [Header("Animation & Feel")]
    public float inspectionDistance = 0.7f;
    public float moveDuration = 0.4f;
    public Ease moveEase = Ease.OutBack;
    public float rotateSpeed = 180f;
    
    [Header("References")]
    [SerializeField] private Collider collider;
    [SerializeField] private Outline outline;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isInspecting = false;
    private Tween currentTween;
    
    private void Awake()
    {
        rotateInputAction.action.Enable();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDestroy()
    {
        rotateInputAction.action.Disable();
        InputSystem.onDeviceChange -= OnDeviceChange;
    }
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Disconnected:
                rotateInputAction.action.Disable();
                break;
            case InputDeviceChange.Reconnected:
                rotateInputAction.action.Enable();
                break;
        }
    }
    
    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (outline)
        {
            outline.OutlineWidth = 3f;
        }
    }
    
    public void OnHoverExit(HoverExitEventArgs args)
    {
        if (outline)
        {
            outline.OutlineWidth = 0f;
        }
    }

    public void OnInspectStart(SelectEnterEventArgs args)
    {
        if (isInspecting) return;
        isInspecting = true;
        currentTween?.Kill();

        if (DetectiveModeController.Instance.IsDetectiveModeOn)
        {
            DetectiveModeController.Instance.StopDetectiveMode();
        }
        
        if (collider)
        {
            collider.enabled = false;
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        if (interactor)
        {
            interactor.transform.GetChild(0).gameObject.SetActive(false);
        }

        Transform cameraTransform = Camera.main.transform;
        Vector3 forwardDirection = cameraTransform.forward;
        forwardDirection.y = 0;

        Vector3 targetPosition = cameraTransform.position + forwardDirection.normalized * inspectionDistance;
        
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(targetPosition, moveDuration).SetEase(moveEase));
        sequence.Join(transform.DORotateQuaternion(targetRotation, moveDuration));
        currentTween = sequence;
    }

    public void OnInspectEnd(SelectExitEventArgs args)
    {
        if (!isInspecting) return;
        isInspecting = false;
        
        if (interactor)
        {
            interactor.transform.GetChild(0).gameObject.SetActive(true);
        }
        
        if (collider)
        {
            collider.enabled = true;
        }
        
        currentTween?.Kill();

        Sequence returnSequence = DOTween.Sequence();
        returnSequence.Append(transform.DOMove(originalPosition, moveDuration).SetEase(moveEase));
        returnSequence.Join(transform.DORotateQuaternion(originalRotation, moveDuration));
    }

    private void Update()
    {
        if (isInspecting)
        {
            Vector2 rotateInput = rotateInputAction.action.ReadValue<Vector2>();

            if (Mathf.Abs(rotateInput.x) > 0.1f)
            {
                transform.Rotate(Vector3.up, -rotateInput.x * rotateSpeed * Time.deltaTime, Space.World);
            }

            if (Mathf.Abs(rotateInput.y) > 0.1f)
            {
                transform.Rotate(Vector3.right, rotateInput.y * rotateSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}