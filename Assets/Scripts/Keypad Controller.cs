using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class KeypadController : MonoBehaviour
{
    [SerializeField] private List<int> correctPassword = new();
    [SerializeField] private TMP_InputField codeDisplay;
    [SerializeField] private string successText = "UNLOCKED";
    [Min(0.1f)]
    [SerializeField] private float resetDelay = 1f;

    [Header("Keypad Events")]
    public UnityEvent onCorrectPassword;
    public UnityEvent onIncorrectPassword;

    private readonly List<int> _input = new();
    private bool _hasSucceeded = false; 
    private bool _isLocked = false; 

    public bool HasSucceeded => _hasSucceeded;

    private void Awake()
    {
        if (codeDisplay != null) codeDisplay.readOnly = true;
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
    }

    private void HandleFailure()
    {
        onIncorrectPassword?.Invoke();
        StartCoroutine(ResetCoroutine());
    }

    private IEnumerator ResetCoroutine()
    {
        _isLocked = true; 
        yield return new WaitForSeconds(resetDelay);

        _input.Clear();
        if (codeDisplay != null) codeDisplay.text = "Enter code…";
        _isLocked = false;  
    }

    private void RefreshDisplay()
    {
        if (codeDisplay == null) return;

        codeDisplay.text = string.Empty;
        for (int i = 0; i < _input.Count; ++i)
            codeDisplay.text += _input[i];
    }
}
