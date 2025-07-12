using System;
using System.Collections;
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

    private void Start()
    {
        StartCoroutine(ActivateTutorialCanvasAfterDelay());
    }
    

    private void OnDisable()
    {
        exitTutorialInputActionReference.action.performed -= OnTutorialExitPressed;
    }
    
    private void OnTutorialExitPressed(InputAction.CallbackContext obj)
    {
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            AudioManager.Instance.PlayUISound(tutorialSoundDefinition);
        }
    }
    
    private IEnumerator ActivateTutorialCanvasAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (!tutorialGameObject.activeInHierarchy)
        {
            tutorialGameObject.SetActive(true);
            AudioManager.Instance.PlayUISound(tutorialSoundDefinition);
            exitTutorialInputActionReference.action.performed += OnTutorialExitPressed;
        }
    }
}
