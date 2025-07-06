using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnvironmentController : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public static EnvironmentController Instance { get; private set; }

    [Header("Light Groups")]
    [Tooltip("All the main ceiling lights that turn on with full power.")]
    [SerializeField] private List<Light> ceilingLights;
    [Tooltip("The single ceiling light that flickers during emergency power.")]
    [SerializeField] private Light flickeringCeilingLight;
    [SerializeField] private Light engineRoomCeilingLight;

    [Header("Emissive Materials")]
    [Tooltip("The materials for the blue emergency floor lights.")]
    [SerializeField] private List<Material> emergencyLightMaterials;

    [Header("Flicker Settings")]
    [SerializeField] private float minFlickerIntensity = 0.1f;
    [SerializeField] private float maxFlickerIntensity = 0.5f;
    [SerializeField] private float minFlickerTime = 0.05f;
    [SerializeField] private float maxFlickerTime = 0.2f;

    [Header("Power Up Settings")]
    [SerializeField] private float powerUpDuration = 1.5f;
    [SerializeField] private float powerDownDuration = 2.0f;

    [Header("Sound Effects")]
    [SerializeField] private SoundDefinition powerUpSuccessSound;
    [SerializeField] private SoundDefinition powerDownFailureSound;
    [SerializeField] private SoundDefinition lightsOnSfxDefinition;

    private Coroutine _flickerCoroutine;
    private List<float> _originalCeilingIntensities = new List<float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var ceilingLight in ceilingLights)
        {
            _originalCeilingIntensities.Add(ceilingLight.intensity);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPowerRestored += HandlePowerRestored;
    }

    private void OnDisable()
    {
        GameEvents.OnPowerRestored -= HandlePowerRestored;
    }

    private void Start()
    {
        SetEmergencyPowerState();
        //StartCoroutine(TestCoroutine());
    }

    private IEnumerator TestCoroutine()
    {
        yield return new WaitForSeconds(10f);
        // Simulate power restoration with success
        GameEvents.TriggerPowerRestored(false);

        yield return new WaitForSeconds(10f);

        GameEvents.TriggerPowerRestored(true);
    }

    private void HandlePowerRestored(bool success)
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }

        StartCoroutine(PowerSequenceCoroutine(success));
    }

    public void SetEmergencyPowerState()
    {
        foreach (var ceilingLight in ceilingLights)
        {
            if (ceilingLight == engineRoomCeilingLight) continue;
            
            ceilingLight.enabled = false;
        }

        flickeringCeilingLight.enabled = true;
        _flickerCoroutine = StartCoroutine(FlickerLightCoroutine());
    }

    private IEnumerator FlickerLightCoroutine()
    {
        while (true)
        {
            flickeringCeilingLight.intensity = Random.Range(minFlickerIntensity, maxFlickerIntensity);
            yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));
        }
    }

    private IEnumerator PowerSequenceCoroutine(bool success)
    {
        if (success)
        {
            //AudioManager.Instance.PlaySound(powerUpSuccessSound, transform.position);
            
            flickeringCeilingLight.color = Color.white;

            for (int i = 0; i < ceilingLights.Count; i++)
            {
                ceilingLights[i].enabled = true;
                ceilingLights[i].intensity = 0;
                
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
                
                AudioManager.Instance.PlaySound(lightsOnSfxDefinition, ceilingLights[i].transform.position);
            }

            float elapsedTime = 0f;
            while (elapsedTime < powerUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / powerUpDuration;
                for (int i = 0; i < ceilingLights.Count; i++)
                {
                    if (ceilingLights[i] == engineRoomCeilingLight)
                    {
                        ceilingLights[i].intensity = Mathf.Lerp(0, 2f, progress);
                        continue;
                    }
                    ceilingLights[i].intensity = Mathf.Lerp(0, _originalCeilingIntensities[i], progress);
                }
                yield return null;
            }
        }
        else
        {
            // Flash all lights on to full power instantly.
            foreach (var ceilingLight in ceilingLights)
            {
                ceilingLight.enabled = true;
                ceilingLight.intensity = _originalCeilingIntensities[ceilingLights.IndexOf(ceilingLight)];
            }

            yield return new WaitForSeconds(0.5f);
            
            //AudioManager.Instance.PlaySound(powerDownFailureSound, transform.position);

            // Fade all lights out.
            float elapsedTime = 0f;
            while (elapsedTime < powerDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = 1 - (elapsedTime / powerDownDuration); // Fade out
                for (int i = 0; i < ceilingLights.Count; i++)
                {
                    if (engineRoomCeilingLight)
                    {
                        float currentIntensity = ceilingLights[i].intensity;
                        ceilingLights[i].intensity = Mathf.Lerp(_originalCeilingIntensities[i], currentIntensity, progress);
                    }
                    ceilingLights[i].intensity = Mathf.Lerp(0, _originalCeilingIntensities[i], progress);
                }
                yield return null;
            }

            SetEmergencyPowerState();
        }
    }
}