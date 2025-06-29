using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source Pool")]
    [SerializeField]
    private int initialPoolSize = 20;
    private List<AudioSource> _pooledSources;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pooledSources = new List<AudioSource>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledSource();
        }
    }

    /// <summary>
    /// Plays a sound defined by a SoundDefinition at a specific world position.
    /// This is the primary method for playing 3D "fire-and-forget" sound effects.
    /// </summary>
    /// <param name="soundDef">The Sound Definition asset to play.</param>
    /// <param name="position">The world position to play the sound at.</param>
    public void PlaySound(SoundDefinition soundDef, Vector3 position)
    {
        if (!soundDef || !soundDef.clip)
        {
            Debug.LogWarning("Tried to play a null Sound Definition or clip.");
            return;
        }

        AudioSource source = GetAvailableSource();
        if (!source)
        {
            source = CreatePooledSource();
            Debug.LogWarning("Audio pool exhausted. Growing pool size.");
        }

        source.transform.position = position;
        soundDef.ApplyTo(source);

        source.Play();
        
        if (!soundDef.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlaying(source, soundDef.clip.length / source.pitch));
        }
    }
    
    public void PlayUISound(SoundDefinition soundDef)
    {
        PlaySound(soundDef, transform.position);
    }


    private AudioSource GetAvailableSource()
    {
        foreach (var source in _pooledSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return null;
    }

    private AudioSource CreatePooledSource()
    {
        GameObject newSourceGo = new GameObject("PooledAudioSource");
        newSourceGo.transform.SetParent(this.transform);
        AudioSource newSource = newSourceGo.AddComponent<AudioSource>();
        _pooledSources.Add(newSource);
        return newSource;
    }
    
    private IEnumerator ReturnSourceToPoolAfterPlaying(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        source.Stop();
        source.clip = null;
    }
}