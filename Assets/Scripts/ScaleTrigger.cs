using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class ScaleTrigger : MonoBehaviour
{
    private Collider panCollider;

    private ScaleController scaleController;


    private void Awake()
    {
        scaleController = GetComponentInParent<ScaleController>();
        panCollider = GetComponent<Collider>();

        if (scaleController == null)
            Debug.LogError("ScaleBowl could not find a ScaleController in its parents.");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ScaleWeight weight)) scaleController.RegisterWeight(panCollider, weight);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ScaleWeight weight))
            scaleController.UnregisterWeight(panCollider, weight);
    }
}