using System;
using UnityEngine;

public class ReplacementShaderEffect : MonoBehaviour
{
    private static readonly int OverDrawColor = Shader.PropertyToID("_OverDrawColor");
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Shader replacementShader;
    [SerializeField] private Color overDrawColor;

    private void OnValidate()
    {
        Shader.SetGlobalColor(OverDrawColor, overDrawColor);
    }

    private void OnEnable()
    {
        if (replacementShader)
        {
            mainCamera.SetReplacementShader(replacementShader, "");
        }
    }
}
