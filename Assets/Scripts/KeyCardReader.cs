using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class KeyCardReader : MonoBehaviour
{
    // An enum makes managing the reader's state cleaner than using multiple booleans.
    private enum ReaderState
    {
        Idle,
        Detecting,
        Accepted,
        Rejected
    }

    private ReaderState currentState;

    [Header("Interaction Settings")] [Tooltip("The tag of the GameObject that will be accepted by this reader.")]
    public GameObject keycard; // Use tags to identify object types.

    public float requiredSeconds = 2.0f;
    public UnityEvent onKeycardAccepted;

    [Header("Visual Settings")] public Renderer indicatorRenderer;
    public Color normalColor = Color.yellow;
    public Color acceptedColor = Color.green;
    public Color rejectedColor = Color.red;
    public Color offColor = Color.black;

    [Header("Blinking Timings")] [Tooltip("Blinking speed when the reader is idle.")]
    public float idleBlinkInterval = 1.0f;

    [Tooltip("Blinking speed when a card is being scanned.")]
    public float detectingBlinkInterval = 0.3f;

    [Tooltip("How long the final result (green/red) is shown.")]
    public float resultDisplayDuration = 2.0f;

    private float stateTimer;
    private float blinkTimer;
    private bool isBlinkOn;
    private GameObject objectInTrigger;

    private void Start()
    {
        // The reader starts in the Idle state.
        currentState = ReaderState.Idle;
        if (indicatorRenderer != null)
            // Enable emission to make the color glow.
            indicatorRenderer.material.EnableKeyword("_EMISSION");
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((currentState == ReaderState.Idle && gameObject.GetComponent<XRBaseInteractable>() != null) ||
            other.gameObject == keycard)
        {
            objectInTrigger = other.gameObject;
            ChangeState(ReaderState.Detecting);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If the object that was being detected leaves, reset to Idle.
        if (other.gameObject == objectInTrigger)
        {
            objectInTrigger = null;
            if (currentState == ReaderState.Detecting) ChangeState(ReaderState.Idle);
        }
    }

    private void Update()
    {
        // The Update loop simply calls the method for the current state.
        // This is cleaner than a large if-else chain.
        switch (currentState)
        {
            case ReaderState.Idle:
                UpdateIdleState();
                break;
            case ReaderState.Detecting:
                UpdateDetectingState();
                break;
            case ReaderState.Accepted:
            case ReaderState.Rejected:
                UpdateResultState();
                break;
        }
    }

    // --- State Logic ---

    private void UpdateIdleState()
    {
        // In Idle, the light blinks slowly to show it's active.
        BlinkLamp(idleBlinkInterval, normalColor);
    }

    private void UpdateDetectingState()
    {
        // In Detecting, the light blinks faster.
        BlinkLamp(detectingBlinkInterval, normalColor);

        stateTimer += Time.deltaTime;
        if (stateTimer >= requiredSeconds)
        {
            if (objectInTrigger != null && objectInTrigger == keycard)
            {
                ChangeState(ReaderState.Accepted);
                onKeycardAccepted.Invoke(); // Fire the event for success.
            }
            else
            {
                ChangeState(ReaderState.Rejected);
            }
        }
    }

    private void UpdateResultState()
    {
        // In a result state, the light is a solid color (green or red).
        // No blinking here.
        stateTimer += Time.deltaTime;
        if (stateTimer >= resultDisplayDuration)
            // Once the display duration is over, reset to Idle.
            ChangeState(ReaderState.Idle);
    }

    // --- Helper Methods ---

    private void ChangeState(ReaderState newState)
    {
        currentState = newState;
        stateTimer = 0f; // Reset the timer whenever the state changes.

        // Set the initial color for the new state.
        switch (newState)
        {
            case ReaderState.Idle:
            case ReaderState.Detecting:
                SetLampColor(normalColor);
                break;
            case ReaderState.Accepted:
                SetLampColor(acceptedColor);
                break;
            case ReaderState.Rejected:
                SetLampColor(rejectedColor);
                break;
        }
    }

    private void BlinkLamp(float interval, Color onColor)
    {
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= interval)
        {
            isBlinkOn = !isBlinkOn;
            blinkTimer = 0f;
        }

        SetLampColor(isBlinkOn ? onColor : offColor);
    }

    private void SetLampColor(Color color)
    {
        if (indicatorRenderer != null)
        {
            // Using SetColor with "_EmissionColor" is the standard way to change emissive materials.
            indicatorRenderer.material.SetColor("_EmissionColor", color);
            indicatorRenderer.material.color = color;
        }
    }
}