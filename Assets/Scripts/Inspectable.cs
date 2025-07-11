using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InspectionController : MonoBehaviour
{
    [Header("Setup")] [Tooltip("Assign the controller interactor (e.g., RightHand Controller) here.")]
    [SerializeField] private XRBaseInputInteractor interactor;
    [SerializeField] private XRGrabInteractable xrGrabInteractable;

    [Header("Input")]
    [Tooltip("Reference to the input action for rotation. Use the 'Turn' action from the 'XRI Right Locomotion' map.")]
    [SerializeField] private InputActionReference rotateInputAction;

    [Header("Animation & Feel")] 
    [SerializeField] private float inspectionDistance = 0.7f;
    [SerializeField] private float inspectionBoundaryDistance = 2f;
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private Ease moveEase = Ease.OutBack;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private Vector3 inspectionStartRotation;

    [Header("References")] [SerializeField]
    private Collider collider;

    [SerializeField] private Outline outline;

    [Header("Audio References")] [SerializeField]
    private SoundDefinition inspectStartAudio;

    [SerializeField] private SoundDefinition inspectOverAudio;

    private Transform originalParent;
    private Vector3 originalPosition;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Quaternion originalRotation;
    private bool isInspecting;
    private bool reachedToPlayer;
    private Tween currentTween;

    private Transform inspectionPivot;
    
    private Vector3 _inspectablePosForDistanceCheck;
    private Vector3 _playerPosForDistanceCheck;
    
    private XRInteractionManager interactionManager;
    private IXRSelectInteractable selectInteractable;
    


    private void Awake()
    {
        rotateInputAction.action.Enable();
        InputSystem.onDeviceChange += OnDeviceChange;

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        selectInteractable = GetComponent<IXRSelectInteractable>();
    }

    private void Start()
    {
        interactionManager = xrGrabInteractable.interactionManager;
    }

    private void OnDestroy()
    {
        if (inspectionPivot) Destroy(inspectionPivot.gameObject);

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
        if (outline) outline.OutlineWidth = 3f;
    }

    public void OnHoverExit(HoverExitEventArgs args)
    {
        if (outline) outline.OutlineWidth = 0f;
    }

    public void OnInspectStart(SelectEnterEventArgs args)
    {
        if (isInspecting) return;
        isInspecting = true;
        currentTween?.Kill();
        
        if (DetectiveModeController.Instance.IsDetectiveModeOn) DetectiveModeController.Instance.StopDetectiveMode();

        if (collider) collider.enabled = false;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (interactor) interactor.transform.GetChild(0).gameObject.SetActive(false);

        var cameraTransform = Camera.main.transform;

        if (inspectStartAudio) AudioManager.Instance.PlaySound(inspectStartAudio, cameraTransform.position);

        var forwardDirection = cameraTransform.forward;
        forwardDirection.y = 0;
        var targetPosition = cameraTransform.position + forwardDirection.normalized * inspectionDistance;

        inspectionPivot = new GameObject("InspectionPivot").transform;
        inspectionPivot.position = targetPosition;

        // ---- the "I'm so cool to deal with transformation matrices" hack start ----
        var parent = transform.parent;
        transform.SetParent(Camera.main.transform);
        transform.localRotation = Quaternion.Euler(inspectionStartRotation);
        var rotationCalculated = transform.rotation;

        transform.SetParent(parent);
        transform.rotation = originalRotation;
        // ---- hack ended ----

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(targetPosition, moveDuration).SetEase(moveEase));
        sequence.Join(transform.DORotateQuaternion(rotationCalculated, moveDuration));
        sequence.OnComplete(() =>
        {
            reachedToPlayer = true;
            transform.SetParent(inspectionPivot);
        });

        currentTween = sequence;
    }

    public void OnInspectEnd(SelectExitEventArgs args)
    {
        if (!isInspecting) return;
        isInspecting = false;

        if (interactor) interactor.transform.GetChild(0).gameObject.SetActive(true);
        if (collider) collider.enabled = true;

        currentTween?.Kill();

        transform.SetParent(null);

        var returnSequence = DOTween.Sequence();
        returnSequence.Append(transform.DOMove(originalPosition, moveDuration).SetEase(moveEase));
        returnSequence.Join(transform.DORotateQuaternion(originalRotation, moveDuration));
        returnSequence.OnComplete(() =>
        {
            if (inspectOverAudio) AudioManager.Instance.PlaySound(inspectOverAudio, originalPosition);
            transform.SetParent(originalParent);
            transform.localRotation = originalLocalRotation;
            transform.localPosition = originalLocalPosition;
            reachedToPlayer = false;
        });

        if (inspectionPivot) Destroy(inspectionPivot.gameObject);
    }

    private void Update()
    {
        if (isInspecting && inspectionPivot)
        {
            var rotateInput = rotateInputAction.action.ReadValue<Vector2>();

            if (Mathf.Abs(rotateInput.x) > 0.1f)
                inspectionPivot.Rotate(Vector3.up, -rotateInput.x * rotateSpeed * Time.deltaTime, Space.World);

            if (Mathf.Abs(rotateInput.y) > 0.1f)
                inspectionPivot.Rotate(Camera.main.transform.right, rotateInput.y * rotateSpeed * Time.deltaTime,
                    Space.World);
            
            _inspectablePosForDistanceCheck = transform.position;
            _inspectablePosForDistanceCheck.y = 0f;
            _playerPosForDistanceCheck = Camera.main.transform.position;
            _playerPosForDistanceCheck.y = 0f;

            if (reachedToPlayer && Vector3.Distance(_playerPosForDistanceCheck, transform.position) > inspectionBoundaryDistance)
            {
                OnInspectEnd(null);
                if (interactionManager && interactor)
                {
                    interactionManager.SelectCancel(interactor, selectInteractable);
                }
            }
        }
    }
}