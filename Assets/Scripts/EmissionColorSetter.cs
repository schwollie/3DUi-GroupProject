using UnityEngine;

public class EmissionColorSetter : MonoBehaviour
{
    [Header("Emission Settings")] [Tooltip("Start with emission turned off")]
    public bool startWithEmissionOff = false;

    [Range(0f, 10f)] [Tooltip("Current emission intensity (0 = off)")]
    public float emissionIntensity = 1f;

    [Header("Debug Info")] [SerializeField]
    private Color originalEmissionColor;

    [SerializeField] private float initialIntensity;

    private Renderer objectRenderer;
    private Material materialInstance;
    private bool hasEmission;

    private void Start()
    {
        // Get renderer component
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("No Renderer found on GameObject: " + gameObject.name);
            enabled = false;
            return;
        }

        // Create material instance to avoid modifying shared materials
        materialInstance = objectRenderer.material;

        // Check if material has emission
        hasEmission = materialInstance.HasProperty("_EmissionColor");
        if (!hasEmission)
        {
            Debug.LogWarning("Material does not have emission property!");
            enabled = false;
            return;
        }

        // Store original emission color
        originalEmissionColor = materialInstance.GetColor("_EmissionColor");

        // Calculate and store initial intensity from the original color
        initialIntensity = Mathf.Max(originalEmissionColor.r, originalEmissionColor.g, originalEmissionColor.b);

        // If starting with emission off, set intensity to 0
        if (startWithEmissionOff)
            emissionIntensity = 0f;
        else if (emissionIntensity == 0f && initialIntensity > 0f)
            // If intensity is 0 but we have an original emission color, use that intensity
            emissionIntensity = initialIntensity;

        // Apply initial emission state
        UpdateEmission();
    }

    public void UpdateEmission()
    {
        if (!hasEmission || materialInstance == null) return;

        if (emissionIntensity > 0f)
        {
            // Enable emission
            materialInstance.EnableKeyword("_EMISSION");

            // Calculate normalized base color (without intensity)
            var baseColor = originalEmissionColor;
            if (initialIntensity > 0f) baseColor = originalEmissionColor / initialIntensity;

            // Apply new intensity to the base color
            var newEmissionColor = baseColor * emissionIntensity;
            materialInstance.SetColor("_EmissionColor", newEmissionColor);
        }
        else
        {
            // Disable emission completely
            materialInstance.DisableKeyword("_EMISSION");
            materialInstance.SetColor("_EmissionColor", Color.black);
        }
    }

    // Public methods for runtime control
    public void SetIntensity(float newIntensity)
    {
        emissionIntensity = Mathf.Clamp(newIntensity, 0f, 10f);
        UpdateEmission();
    }

    public void RestoreInitialIntensity()
    {
        emissionIntensity = initialIntensity;
        UpdateEmission();
    }

    public void TurnOff()
    {
        emissionIntensity = 0f;
        UpdateEmission();
    }

    public void TurnOn()
    {
        if (emissionIntensity == 0f) emissionIntensity = initialIntensity > 0f ? initialIntensity : 1f;
        UpdateEmission();
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null) Destroy(materialInstance);
    }
}