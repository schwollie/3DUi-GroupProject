using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScaleController : MonoBehaviour
{
    [Header("Scale Components")]
    [Tooltip("The transform of the part of the scale that will visually tilt (e.g., the beam).")]
    public Transform scaleBeam;

    [Tooltip("The collider for the left side of the scale. Must be a trigger.")]
    public Collider leftPanCollider;

    [Tooltip("The collider for the right side of the scale. Must be a trigger.")]
    public Collider rightPanCollider;

    [Header("Handle Visuals")]
    [Tooltip("(Optional) The transform of a handle or dial that will rotate based on the weight difference.")]
    public Transform handleTransform;

    [Tooltip("How far the handle rotates at maximum imbalance. 90 means it will point straight left/right.")]
    public float handleRotationSensitivity = 20.0f;

    [Header("Puzzle Solution")]
    [Tooltip("The target weight difference (Right Weight - Left Weight) to solve the puzzle.")]
    public float targetWeightDifference = 5.0f;

    [Tooltip("The allowed margin of error for the solution.")]
    public float tolerance = 0.1f;

    [Header("Animation")] [Tooltip("How quickly the scale and handle animate to their new positions.")]
    public float tiltSpeed = 0.4f;

    [Header("Indicator Lamp")] [Tooltip("The renderer component of the indicator lamp.")]
    public Renderer indicatorLampRenderer;

    [Tooltip("The material property name to change (usually '_Color' for standard shader).")]
    public string colorPropertyName = "_GlowColor";

    [Tooltip("Color when no weights are on the scale.")]
    public Color noWeightsColor = Color.black;

    [Tooltip("Color when the scale is unbalanced.")]
    public Color unbalancedColor = Color.red;

    [Tooltip("Color when the scale is balanced.")]
    public Color balancedColor = Color.green;

    [Header("Events")] [Tooltip("This event is triggered once when the scale reaches the target weight.")]
    public UnityEvent onTargetWeightReached;

    // Lists to keep track of all weights currently within each pan's trigger
    private List<ScaleWeight> weightsOnLeftPan = new();
    private List<ScaleWeight> weightsOnRightPan = new();

    private bool isSolved;

    // --- Public Methods for Triggers ---

    public void RegisterWeight(Collider panCollider, ScaleWeight weight)
    {
        if (panCollider == leftPanCollider && !weightsOnLeftPan.Contains(weight))
            weightsOnLeftPan.Add(weight);
        else if (panCollider == rightPanCollider && !weightsOnRightPan.Contains(weight))
            weightsOnRightPan.Add(weight);
    }

    public void UnregisterWeight(Collider panCollider, ScaleWeight weight)
    {
        if (panCollider == leftPanCollider)
            weightsOnLeftPan.Remove(weight);
        else if (panCollider == rightPanCollider)
            weightsOnRightPan.Remove(weight);
    }

    // --- Core Logic ---

    private void Update()
    {
        // 1. Calculate the current weight on each side based on grab state
        var currentLeftWeight = CalculatePanWeight(weightsOnLeftPan);
        var currentRightWeight = CalculatePanWeight(weightsOnRightPan);

        // 2. Animate the scale visually based on the calculated weights
        UpdateScaleTilt(currentLeftWeight, currentRightWeight);

        // 3. Animate the handle based on the calculated weights
        UpdateHandleRotation(currentLeftWeight, currentRightWeight);

        UpdateIndicatorLamp(currentLeftWeight, currentRightWeight);

        // 4. Check for the puzzle solution if not already solved
        if (!isSolved)
            CheckForSolution(currentLeftWeight, currentRightWeight);
    }

    private float CalculatePanWeight(List<ScaleWeight> weightsInPan)
    {
        var totalWeight = 0f;
        // Use a for loop to safely iterate while potentially removing null entries
        for (var i = weightsInPan.Count - 1; i >= 0; i--)
        {
            var weight = weightsInPan[i];
            // If an object gets destroyed while on the scale, remove it from the list
            if (weight == null)
            {
                weightsInPan.RemoveAt(i);
                continue;
            }

            // Add its weight to the total ONLY if it is not being held
            if (!weight.Interactable.isSelected)
                totalWeight += weight.weightValue;
        }

        return totalWeight;
    }

    private void UpdateScaleTilt(float leftWeight, float rightWeight)
    {
        var totalWeight = leftWeight + rightWeight;
        var tiltRatio = 0f;

        if (totalWeight > 0)
            tiltRatio = (rightWeight - leftWeight) / totalWeight;


        var targetAngle = tiltRatio * handleRotationSensitivity;
        var targetRotation = Quaternion.Euler(0, 0, -targetAngle);
        scaleBeam.localRotation = Quaternion.Slerp(scaleBeam.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    // --- NEW METHOD ---
    // This method controls the rotation of the separate handle
    private void UpdateHandleRotation(float leftWeight, float rightWeight)
    {
        // If no handle is assigned in the inspector, do nothing.
        if (handleTransform == null) return;

        var totalWeight = leftWeight + rightWeight;
        var weightRatio = 0f;

        // Calculate the ratio of imbalance. Ranges from -1 (all on left) to +1 (all on right).
        // If total weight is zero, the ratio remains 0, returning the handle to its neutral position.
        if (totalWeight > 0)
            weightRatio = (rightWeight - leftWeight) / totalWeight;

        // Calculate the target angle using the sensitivity.
        // A negative sign is used so the handle rotates in the same direction as the scale tilt.
        var targetAngle = -weightRatio * handleRotationSensitivity;

        // Create the target rotation around the Z-axis.
        var targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // Smoothly animate the handle towards the target rotation.
        handleTransform.localRotation =
            Quaternion.Slerp(handleTransform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    private void UpdateIndicatorLamp(float leftWeight, float rightWeight)
    {
        // If no indicator lamp renderer is assigned, do nothing
        if (indicatorLampRenderer == null) return;

        Color targetColor;

        // Check if there are no weights on either side
        if (leftWeight == 0 && rightWeight == 0)
            targetColor = noWeightsColor;
        // Check if the scale is balanced (weights are equal)
        else if (Mathf.Abs(leftWeight - rightWeight) <= tolerance)
            targetColor = balancedColor;
        // Otherwise, the scale is unbalanced
        else
            targetColor = unbalancedColor;

        // Apply the color to the material
        indicatorLampRenderer.material.SetColor(colorPropertyName, targetColor);
    }


    private void CheckForSolution(float leftWeight, float rightWeight)
    {
        var currentDifference = rightWeight - leftWeight;

        if (Mathf.Abs(currentDifference - targetWeightDifference) <= tolerance)
        {
            isSolved = true;
            onTargetWeightReached?.Invoke();
        }
    }
}