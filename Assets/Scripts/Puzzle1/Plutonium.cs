using System;
using UnityEngine;

[RequireComponent(typeof(ScaleWeight))]
[RequireComponent(typeof(TeleportHome))]
public class Plutonium : MonoBehaviour
{
    public float GetWeight()
    {
        var scaleWeight = GetComponent<ScaleWeight>();
        if (scaleWeight != null) return scaleWeight.weightValue;
        throw new Exception("ScaleWeight component not found on Plutonium object.");
    }

    public void OnCorrectCharge()
    {
        GetComponent<TeleportHome>().ToHome();
    }
}