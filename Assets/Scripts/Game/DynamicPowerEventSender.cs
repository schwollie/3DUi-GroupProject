using UnityEngine;
using UnityEngine.Events;

public class DynamicPowerEventSender : MonoBehaviour
{
    public UnityEvent onPowerCorrectRestored;
    public UnityEvent onPowerInCorrectRestored;

    private void Start()
    {
        // Subscribe to the GameEvents
        GameEvents.OnPowerRestored += HandlePowerRestored;
    }

    public void HandlePowerRestored(bool restored)
    {
        if (restored)
            onPowerCorrectRestored?.Invoke();
        else
            onPowerInCorrectRestored?.Invoke();
    }

    [ContextMenu("TestRestored")]
    public void TestRestored()
    {
        GameEvents.TriggerPowerRestored(true);
    }
}