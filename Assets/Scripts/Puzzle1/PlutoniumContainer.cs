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
            Debug.Log($"Total weight of plutonium {GetTotalWeight()}");
            plutonium.Add(plutoniumItem);
            OnWeightChange.Invoke();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Plutonium plutoniumItem))
        {
            Debug.Log($"Total weight of plutonium {GetTotalWeight()}");
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

    public void OnCorrectCharge()
    {
        for (var i = plutonium.Count - 1; i >= 0; i--) plutonium[i].OnCorrectCharge();
    }
}