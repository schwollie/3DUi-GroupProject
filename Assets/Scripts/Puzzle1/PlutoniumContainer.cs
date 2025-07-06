using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlutoniumContainer : MonoBehaviour
{
    private List<Plutonium> plutonium = new();

    public UnityEvent OnWeightChange;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Plutonium plutoniumItem))
        {
            plutonium.Add(plutoniumItem);
            OnWeightChange.Invoke();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Plutonium plutoniumItem))
        {
            plutonium.Remove(plutoniumItem);
            OnWeightChange.Invoke();
        }
    }

    public float GetTotalWeight()
    {
        var totalWeight = 0f;
        foreach (var item in plutonium) totalWeight += item.GetWeight();
        return totalWeight;
    }
}