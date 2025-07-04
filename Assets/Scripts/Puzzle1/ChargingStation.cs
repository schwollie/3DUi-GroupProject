using UnityEngine;

public class ChargingStation : MonoBehaviour
{
    private SnapAnchor _snapAnchor;
    private PlutoniumContainer _plutoniumContainer;


    private void Awake()
    {
        _snapAnchor = GetComponentInChildren<SnapAnchor>();
        _plutoniumContainer = GetComponentInChildren<PlutoniumContainer>();

        if (_snapAnchor == null) Debug.LogError("SnapAnchor component not found in children!");

        if (_plutoniumContainer == null) Debug.LogError("PlutoniumContainer component not found in children!");

        _plutoniumContainer.OnWeightChange.AddListener(OnChargeTrigger);
        _snapAnchor.onSnapEnter.AddListener(OnChargeTrigger);
    }

    public void OnChargeTrigger()
    {
        Debug.Log("Charging Station Triggered");
        if (_snapAnchor.currentSnappedObject != null &&
            _snapAnchor.currentSnappedObject.GetComponent<Powercell>() != null)
            _snapAnchor.currentSnappedObject.GetComponent<Powercell>().SetCharge(_plutoniumContainer.GetTotalWeight());
    }
}