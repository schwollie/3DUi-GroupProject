using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class TutorialCanvasController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference exitTutorialInputActionReference;
    
    [Header("Object References")]
    [SerializeField] private GameObject tutorialGameObject;
    [SerializeField] private SoundDefinition tutorialSoundDefinition;
    
    [Header("Headset Following")]
    [Tooltip("Reference to the player's main camera (head).")]
    [SerializeField] private Transform playerCamera;
    
    [Tooltip("The angle (in degrees) the player can look away from the canvas before it starts to move.")]
    [SerializeField] private float horizontalDeadZone = 20f;
    [Tooltip("How quickly the canvas moves to its target position once the dead zone is exceeded.")]
    [SerializeField] private float followSpeed = 5f;

    private float _tutorialCanvasY;
    private float _distanceToCamera;
    
    

    private void Awake()
    {
        if (!playerCamera)
        {
            playerCamera = Camera.main.transform;
            if (!playerCamera)
            {
                Debug.LogError("Player Camera reference is not set on the TutorialCanvasController, and no Main Camera was found.", this);
            }
        }

        _tutorialCanvasY = transform.position.y;
        Vector3 tempCameraPosition = playerCamera.position;
        tempCameraPosition.y = 0f;
        
        Vector3 tempCanvasPosition = transform.position;
        tempCanvasPosition.y = 0f;
        
        _distanceToCamera = Vector3.Distance(tempCameraPosition, tempCanvasPosition);
    }

    private void Start()
    {
        StartCoroutine(ActivateTutorialCanvasAfterDelay());
    }
    

    private void OnDisable()
    {
        exitTutorialInputActionReference.action.performed -= OnTutorialExitPressed;
    }
    
    private void LateUpdate()
    {
        if (!playerCamera) return;

        Vector3 cameraForward = playerCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraToCanvas = transform.position - playerCamera.position;
        cameraToCanvas.y = 0;
        cameraToCanvas.Normalize();

        float angle = Vector3.Angle(cameraForward, cameraToCanvas);

        if (angle > horizontalDeadZone)
        {
            Vector3 targetPosition = playerCamera.position + cameraForward * _distanceToCamera;
            targetPosition.y = _tutorialCanvasY;

            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - playerCamera.position);
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }
    
    private void OnTutorialExitPressed(InputAction.CallbackContext obj)
    {
        if (gameObject.activeInHierarchy)
        {
            tutorialGameObject.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
            AudioManager.Instance.PlayUISound(tutorialSoundDefinition);
        }
    }
    
    private IEnumerator ActivateTutorialCanvasAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (!tutorialGameObject.activeInHierarchy)
        {
            tutorialGameObject.transform.localScale = Vector3.zero;
            tutorialGameObject.SetActive(true);
            tutorialGameObject.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            AudioManager.Instance.PlayUISound(tutorialSoundDefinition);
            exitTutorialInputActionReference.action.performed += OnTutorialExitPressed;
        }
    }
}
