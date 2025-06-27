using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DetectiveModeController : MonoBehaviour
{
    #region Singleton

    public static DetectiveModeController Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of DetectiveModeController detected. Destroying the new instance.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
    }

    #endregion
   
    
    [Header("URP Renderer Settings")]
    [SerializeField]
    private UniversalRendererData rendererData;

    [SerializeField]
    private string clueGlowFeatureName = "ClueGlow";
    [SerializeField]
    private string xrayWallsFeatureName = "XrayWalls";

    [Header("Input Settings")]
    [SerializeField] 
    private InputActionReference detectiveModeInputAction;
    
    [Header("Scanner")]
    [SerializeField] private ParticleSystem scannerParticleSystem;
    [SerializeField] private float scannerDuration = 1f;
    [SerializeField] private float activateGlowWaitDuration = 0.2f;
    [SerializeField] private float detectiveModeDuration = 3f;
    [SerializeField] private Volume postProcessVolume;

    private ColorAdjustments _colorAdjustments;
    
    private Coroutine _scannerCoroutine;
    
    private LayerMask originalOpaqueLayerMask;

    private bool isInitialized = false;

    private bool isDetectiveModeOn = false;
    
    public bool IsDetectiveModeOn => isDetectiveModeOn;

    private void OnEnable()
    {
        detectiveModeInputAction.action.performed += OnDetectiveModePressed;
    }

    private void OnDisable()
    {
        detectiveModeInputAction.action.performed -= OnDetectiveModePressed;
        ResetRendererFeatures();
    }
    
    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        if (!rendererData)
        {
            Debug.LogError("Renderer Data is not assigned in the DetectiveModeController Inspector!");
            return;
        }
        
        originalOpaqueLayerMask = rendererData.opaqueLayerMask;
        
        scannerParticleSystem.time = scannerDuration;

        postProcessVolume.profile.TryGet(out ColorAdjustments colorAdjustments);

        _colorAdjustments = colorAdjustments;

        SetDetectiveMode(false);
        isInitialized = true;
    }

    private void OnDetectiveModePressed(InputAction.CallbackContext context)
    {
        if(_scannerCoroutine != null)
        {
            StopCoroutine(_scannerCoroutine);
        }
        
        _scannerCoroutine = StartCoroutine(ToggleDetectiveMode());
    }

    private IEnumerator ToggleDetectiveMode()
    {
        if (!isInitialized) Initialize();

        isDetectiveModeOn = true;
        
        scannerParticleSystem.Play();

        if (_colorAdjustments)
        {
            _colorAdjustments.active = true;
        }
        
        yield return new WaitForSeconds(activateGlowWaitDuration);
        SetDetectiveMode(true);

        yield return new WaitForSeconds(detectiveModeDuration);
        
        StopDetectiveMode();
    }

    public void StopDetectiveMode()
    {
        SetDetectiveMode(false);
        
        if (_colorAdjustments)
        {
            _colorAdjustments.active = false;
        }

        isDetectiveModeOn = false;
    }

    private void SetDetectiveMode(bool isOn)
    {
        SetRendererFeatureActive(clueGlowFeatureName, isOn);
        SetRendererFeatureActive(xrayWallsFeatureName, isOn);

        if (!isOn)
        {
            rendererData.opaqueLayerMask = originalOpaqueLayerMask;
        }
        
        rendererData.SetDirty();
    }
    
    private void ResetRendererFeatures()
    {
        SetDetectiveMode(false);
    }

    private void SetRendererFeatureActive(string featureName, bool active)
    {
        var feature = rendererData.rendererFeatures.Find(f => f.name == featureName);
        if (feature)
        {
            feature.SetActive(active);
        }
    }
}