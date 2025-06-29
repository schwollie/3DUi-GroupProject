using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewSoundDefinition", menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    [Header("Sound Properties")]
    public AudioClip clip;
    
    [Tooltip("The mixer group to route this sound through.")]
    public AudioMixerGroup mixerGroup;

    [Header("Volume and Pitch")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Tooltip("A random variation added to the pitch. (e.g., 0.1 for +/- 10%)")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0f;

    [Header("3D Sound Settings")]
    [Range(0f, 1f)]
    public float spatialBlend = 1.0f;
    public float minDistance = 1.0f;
    public float maxDistance = 500f;

    [Header("Looping")]
    public bool loop = false;

    public void ApplyTo(AudioSource audioSource)
    {
        if (!audioSource) return;

        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = mixerGroup;
        audioSource.volume = volume;
        
        audioSource.pitch = pitch * (1 + Random.Range(-pitchVariation, pitchVariation));
        
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;
    }
}