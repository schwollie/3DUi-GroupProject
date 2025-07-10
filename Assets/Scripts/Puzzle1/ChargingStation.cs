using System.Collections;
using UnityEngine;

public class ChargingStation : MonoBehaviour
{
    [Header("Components")] private SnapAnchor _snapAnchor;
    private PlutoniumContainer _plutoniumContainer;

    [Header("Monitor Settings")] [SerializeField]
    private GameObject monitor;

    [SerializeField] private float blinkingInterval = 0.5f;
    [SerializeField] private float emptyInterval = 0.3f;
    [SerializeField] private float errorBlinkInterval = 0.1f;
    [SerializeField] private int errorBlinkCount = 5;
    [SerializeField] private float chargingAnimationDuration = 3f;

    [Header("Materials")] [SerializeField] private Material emptyMaterial;
    [SerializeField] private Material halfFullRedMaterial;
    [SerializeField] private Material halfFullBlueMaterial;
    [SerializeField] private Material fullRedMaterial;
    [SerializeField] private Material fullBlueMaterial;

    private Coroutine currentBlinkCoroutine;
    private Renderer monitorRenderer;
    private Powercell currentPowercell;
    private bool isCharging;

    private void Awake()
    {
        _snapAnchor = GetComponentInChildren<SnapAnchor>();
        _plutoniumContainer = GetComponentInChildren<PlutoniumContainer>();

        if (_snapAnchor == null) Debug.LogError("SnapAnchor component not found in children!");
        if (_plutoniumContainer == null) Debug.LogError("PlutoniumContainer component not found in children!");
        if (monitor == null) Debug.LogError("Monitor GameObject not assigned!");

        monitorRenderer = monitor.GetComponent<Renderer>();
        if (monitorRenderer == null) Debug.LogError("Monitor GameObject doesn't have a Renderer component!");

        // Validate materials
        ValidateMaterials();

        _plutoniumContainer.OnWeightChange.AddListener(OnWeightChange);
        _snapAnchor.onSnapEnter.AddListener(OnCellEntered);
        _snapAnchor.onSnapExit.AddListener(OnCellExited);
    }

    private void ValidateMaterials()
    {
        if (emptyMaterial == null) Debug.LogError("Empty material not assigned!");
        if (halfFullRedMaterial == null) Debug.LogError("Half full red material not assigned!");
        if (halfFullBlueMaterial == null) Debug.LogError("Half full blue material not assigned!");
        if (fullRedMaterial == null) Debug.LogError("Full red material not assigned!");
        if (fullBlueMaterial == null) Debug.LogError("Full blue material not assigned!");
    }

    private void Start()
    {
        // Start blinking when no cell is present
        StartIdleBlinking();
    }

    public void OnWeightChange()
    {
        Debug.Log("Charging Station Triggered");

        if (currentPowercell == null || isCharging) return;

        // Check if the cell is already charged
        if (currentPowercell.CorrectCharge())
        {
            StopCurrentBlink();
            SetMonitorMaterial(GetFullMaterial(currentPowercell));
            return;
        }

        // Check if plutonium weight matches target charge
        var totalWeight = _plutoniumContainer.GetTotalWeight();
        if (Mathf.Abs(totalWeight - currentPowercell.targetCharge) < 1e-3)
            // Correct weight - start charging animation
            StartChargingAnimation();
        else
            // Wrong weight - show error
            OnError();
    }

    public void OnCellEntered()
    {
        Debug.Log("Cell entered charging station");

        if (_snapAnchor.currentSnappedObject != null)
        {
            currentPowercell = _snapAnchor.currentSnappedObject.GetComponent<Powercell>();

            if (currentPowercell != null)
            {
                Debug.Log(
                    $"Powercell detected: isRed = {currentPowercell.isRed}, isCharged = {currentPowercell.CorrectCharge()}");

                StopCurrentBlink();

                // Ensure monitor is active
                monitor.SetActive(true);

                // Check if cell is already charged
                if (currentPowercell.CorrectCharge())
                {
                    // Show full material immediately
                    var fullMat = GetFullMaterial(currentPowercell);
                    if (fullMat != null)
                    {
                        SetMonitorMaterial(fullMat);
                    }
                    else
                    {
                        Debug.LogError("Full material is null!");
                        StartChargingBlink(); // Fallback to blinking
                    }
                }
                else
                {
                    if (Mathf.Abs(_plutoniumContainer.GetTotalWeight() - currentPowercell.targetCharge) < 1e-3)
                        // Start charging animation if weight matches
                        StartChargingAnimation();
                    else
                        // Start blinking for empty/half-full state
                        StartChargingBlink();
                }
            }
        }
    }

    public void OnCellExited()
    {
        currentPowercell = null;
        isCharging = false;
        StopCurrentBlink();
        StartIdleBlinking();
    }

    private void OnError()
    {
        if (currentPowercell == null || isCharging) return;

        StopCurrentBlink();
        currentBlinkCoroutine = StartCoroutine(ErrorBlinkRoutine());
    }

    private void StartIdleBlinking()
    {
        StopCurrentBlink();
        currentBlinkCoroutine = StartCoroutine(IdleBlinkRoutine());
    }

    private void StartChargingBlink()
    {
        StopCurrentBlink();
        currentBlinkCoroutine = StartCoroutine(ChargingBlinkRoutine());
    }

    private void StartChargingAnimation()
    {
        if (currentPowercell == null || isCharging) return;

        isCharging = true;
        StopCurrentBlink();
        currentBlinkCoroutine = StartCoroutine(ChargingAnimationRoutine());
    }

    private void StopCurrentBlink()
    {
        if (currentBlinkCoroutine != null)
        {
            StopCoroutine(currentBlinkCoroutine);
            currentBlinkCoroutine = null;
        }
    }

    private IEnumerator IdleBlinkRoutine()
    {
        while (true)
        {
            monitor.SetActive(true);
            SetMonitorMaterial(emptyMaterial); // Set empty material when idle
            yield return new WaitForSeconds(blinkingInterval);
            monitor.SetActive(false);
            yield return new WaitForSeconds(blinkingInterval);
        }
    }

    private IEnumerator ChargingBlinkRoutine()
    {
        var halfFullMaterial = GetHalfFullMaterial(currentPowercell);

        // Validate materials before starting
        if (emptyMaterial == null || halfFullMaterial == null)
        {
            Debug.LogError("Materials not properly assigned for charging blink!");
            monitor.SetActive(true); // Keep monitor on as fallback
            yield break;
        }

        while (true)
        {
            monitor.SetActive(true); // Ensure monitor is active
            SetMonitorMaterial(emptyMaterial);
            yield return new WaitForSeconds(emptyInterval);
            SetMonitorMaterial(halfFullMaterial);
            yield return new WaitForSeconds(emptyInterval);
        }
    }

    private IEnumerator ErrorBlinkRoutine()
    {
        var halfFullMaterial = GetHalfFullMaterial(currentPowercell);

        for (var i = 0; i < errorBlinkCount; i++)
        {
            monitor.SetActive(true);
            SetMonitorMaterial(emptyMaterial);
            yield return new WaitForSeconds(errorBlinkInterval);
            SetMonitorMaterial(halfFullMaterial);
            yield return new WaitForSeconds(errorBlinkInterval);
        }

        // Resume normal charging blink after error
        StartChargingBlink();
    }

    private IEnumerator ChargingAnimationRoutine()
    {
        var halfFullMaterial = GetHalfFullMaterial(currentPowercell);
        var fullMaterial = GetFullMaterial(currentPowercell);

        var thirdDuration = chargingAnimationDuration / 3f;

        monitor.SetActive(true); // Ensure monitor stays on

        // Empty phase
        SetMonitorMaterial(emptyMaterial);
        yield return new WaitForSeconds(thirdDuration);

        // Half full phase
        SetMonitorMaterial(halfFullMaterial);
        yield return new WaitForSeconds(thirdDuration);

        // Completely full phase
        SetMonitorMaterial(fullMaterial);
        yield return new WaitForSeconds(thirdDuration);

        // Stay on completely full and charge the cell
        if (currentPowercell != null)
        {
            _plutoniumContainer.OnCorrectCharge();
            currentPowercell.SetCharge(_plutoniumContainer.GetTotalWeight());
        }

        isCharging = false;
    }

    private void SetMonitorMaterial(Material material)
    {
        if (monitorRenderer != null && material != null)
            monitorRenderer.material = material;
        else
            Debug.LogWarning(
                $"Cannot set monitor material: renderer={monitorRenderer != null}, material={material != null}");
    }

    private Material GetHalfFullMaterial(Powercell cell)
    {
        if (cell == null)
        {
            Debug.LogWarning("GetHalfFullMaterial called with null cell");
            return emptyMaterial;
        }

        var mat = cell.isRed ? halfFullRedMaterial : halfFullBlueMaterial;
        if (mat == null)
        {
            Debug.LogError($"Half full material is null for {(cell.isRed ? "red" : "blue")} cell!");
            return emptyMaterial;
        }

        return mat;
    }

    private Material GetFullMaterial(Powercell cell)
    {
        if (cell == null)
        {
            Debug.LogWarning("GetFullMaterial called with null cell");
            return emptyMaterial;
        }

        var mat = cell.isRed ? fullRedMaterial : fullBlueMaterial;
        if (mat == null)
        {
            Debug.LogError($"Full material is null for {(cell.isRed ? "red" : "blue")} cell!");
            return emptyMaterial;
        }

        return mat;
    }

    private void OnDestroy()
    {
        StopCurrentBlink();
    }
}