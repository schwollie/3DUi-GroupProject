using UnityEngine;
using UnityEngine.Events;

public class KeyCardReader : MonoBehaviour
{
    [Header("Keycard Settings")] public GameObject keycard; // Explicitly set in Inspector

    [Header("Reader Settings")] public float requiredSeconds = 2f; // Adjustable in Inspector
    public UnityEvent onKeycardAccepted;

    [Header("Glow Settings")] public Renderer readerRenderer;
    public Color glowColor = Color.yellow;
    public Color normalColor = Color.white;

    private float timer;
    private bool keycardInside;

    private void Start()
    {
        if (readerRenderer != null)
            readerRenderer.material.color = normalColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == keycard)
        {
            keycardInside = true;
            timer = 0f;
            if (readerRenderer != null)
                readerRenderer.material.color = glowColor;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == keycard)
        {
            keycardInside = false;
            timer = 0f;
            if (readerRenderer != null)
                readerRenderer.material.color = normalColor;
        }
    }

    private void Update()
    {
        if (keycardInside)
        {
            timer += Time.deltaTime;
            if (timer >= requiredSeconds)
            {
                onKeycardAccepted.Invoke();
                keycardInside = false; // Prevent multiple triggers
                if (readerRenderer != null)
                    readerRenderer.material.color = normalColor;
            }
        }
    }
}