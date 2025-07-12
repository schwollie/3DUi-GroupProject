using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class KeypadController : MonoBehaviour
{
    [SerializeField] private List<int> correctPassword = new();
    [SerializeField] private TMP_InputField codeDisplay;
    [SerializeField] private Image codeDisplayBG;
    [SerializeField] private string successText = "ACCESS GRANTED";
    [Min(0.1f)]
    [SerializeField] private float resetDelay = 1f;

    [Header("Keypad Events")]
    public UnityEvent onCorrectPassword;
    public UnityEvent onIncorrectPassword;
    
    [Header("Sound Definitions")]
    [SerializeField] private SoundDefinition accessDeniedSoundDefinition;
    [SerializeField] private SoundDefinition welcomeSoundDefinition;
    [SerializeField] private SoundDefinition passwordIncorrectSoundDefinition;
    [SerializeField] private SoundDefinition passwordCorrectSoundDefinition;
    [SerializeField] private SoundDefinition openDoorSoundDefinition;
    
    [Header("Keypad References")]
    [SerializeField] private CanvasGroup keypadCanvasGroup;

    private readonly List<int> _input = new();
    private bool _hasSucceeded = false; 
    private bool _isLocked = false;

    private Color codeDisplayBGOriginalColor;

    public bool HasSucceeded => _hasSucceeded;

    private void Awake()
    {
        if (codeDisplay)
        {
            codeDisplay.readOnly = true;
            codeDisplay.text = "NO POWER";
        }

        codeDisplayBGOriginalColor = codeDisplayBG.color;
    }
    
    private void OnEnable()
    {
        GameEvents.OnPowerRestored += HandlePowerRestored;
    }

    private void OnDisable()
    {
        GameEvents.OnPowerRestored -= HandlePowerRestored;
    }
    
    private void HandlePowerRestored(bool powerRestored)
    {
        if (powerRestored && codeDisplay)
        {
            keypadCanvasGroup.interactable = true;
            codeDisplay.text = "Enter code...";
        }
    }

    public void AddDigit(int digit)
    {
        if (_isLocked || _hasSucceeded) return;
        if (_input.Count >= correctPassword.Count) return;

        _input.Add(digit);
        RefreshDisplay();
    }

    public void DeleteLast()
    {
        if (_isLocked || _hasSucceeded) return;
        if (_input.Count == 0) return;

        _input.RemoveAt(_input.Count - 1);
        RefreshDisplay();
    }

    public void Submit()
    {
        if (_isLocked || _hasSucceeded) return;
        if (_input.Count != correctPassword.Count) return;

        bool match = true;
        for (int i = 0; i < correctPassword.Count && match; ++i)
            match &= _input[i] == correctPassword[i];

        if (match)
            HandleSuccess();
        else
            HandleFailure();
    }

    private void HandleSuccess()
    {
        _hasSucceeded = true;
        onCorrectPassword?.Invoke();
        if (codeDisplay != null) codeDisplay.text = successText;
        StartCoroutine(AccessResultSoundCoroutine(true));
    }

    private void HandleFailure()
    {
        onIncorrectPassword?.Invoke();
        StartCoroutine(ResetCoroutine());
        StartCoroutine(AccessResultSoundCoroutine(false));
    }

    private IEnumerator AccessResultSoundCoroutine(bool accessGranted)
    {
        if (accessGranted)
        {
            AudioManager.Instance.PlaySound(passwordCorrectSoundDefinition, transform.position);
            yield return new WaitForSeconds(0.5f);
            AudioManager.Instance.PlaySound(openDoorSoundDefinition, transform.position + Vector3.right * 1.5f);
            AudioManager.Instance.PlaySound(welcomeSoundDefinition, transform.position);
            this.enabled = false;
        }
        
        else
        {
            AudioManager.Instance.PlaySound(passwordIncorrectSoundDefinition, transform.position);
            codeDisplayBG.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            AudioManager.Instance.PlaySound(accessDeniedSoundDefinition, transform.position);
        }
    }

    private IEnumerator ResetCoroutine()
    {
        _isLocked = true; 
        yield return new WaitForSeconds(resetDelay);

        _input.Clear();
        codeDisplayBG.color = codeDisplayBGOriginalColor;
        if (codeDisplay) codeDisplay.text = "Enter code...";
        _isLocked = false;  
    }

    private void RefreshDisplay()
    {
        if (!codeDisplay) return;

        codeDisplay.text = string.Empty;
        for (int i = 0; i < _input.Count; ++i)
            codeDisplay.text += _input[i];
    }
}
