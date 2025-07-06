using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source Pool")] [SerializeField]
    private int initialPoolSize = 20;

    private List<AudioSource> _pooledSources;

    private Dictionary<GameObject, AudioSource> _continuousSounds;

    [Header("Ambient Music Settings")] [SerializeField]
    private SoundDefinition defaultAmbientMusic;

    [SerializeField] private float musicTransitionDuration = 2f;

    private AudioSource _ambientMusicSource;
    private AudioSource _roomMusicSource;
    private Coroutine _musicTransitionCoroutine;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _continuousSounds = new Dictionary<GameObject, AudioSource>();


        _pooledSources = new List<AudioSource>();
        for (var i = 0; i < initialPoolSize; i++) CreatePooledSource();

        // Create dedicated audio sources for music
        CreateMusicSources();

        // Start default ambient music if specified
        if (defaultAmbientMusic != null) PlayDefaultAmbientMusic();
    }

    private void OnDestroy()
    {
        // Clean up all continuous sounds when AudioManager is destroyed
        StopAllContinuousSounds();
    }

    // Handle cleanup when GameObjects with continuous sounds are destroyed
    private void Update()
    {
        // Check for destroyed GameObjects
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in _continuousSounds)
            if (kvp.Key == null)
            {
                // GameObject was destroyed, clean up the audio source
                kvp.Value.Stop();
                kvp.Value.clip = null;
                kvp.Value.transform.SetParent(transform);
                kvp.Value.transform.localPosition = Vector3.zero;
                keysToRemove.Add(kvp.Key);
            }

        // Remove null entries
        foreach (var key in keysToRemove) _continuousSounds.Remove(key);
    }


    private void CreateMusicSources()
    {
        // Create ambient music source
        var ambientGo = new GameObject("AmbientMusicSource");
        ambientGo.transform.SetParent(transform);
        _ambientMusicSource = ambientGo.AddComponent<AudioSource>();
        _ambientMusicSource.loop = true;
        _ambientMusicSource.spatialBlend = 0f; // 2D sound

        // Create room music source
        var roomGo = new GameObject("RoomMusicSource");
        roomGo.transform.SetParent(transform);
        _roomMusicSource = roomGo.AddComponent<AudioSource>();
        _roomMusicSource.loop = true;
        _roomMusicSource.spatialBlend = 0f; // 2D sound
    }

    public void PlayDefaultAmbientMusic()
    {
        if (defaultAmbientMusic != null && _ambientMusicSource != null)
        {
            defaultAmbientMusic.ApplyTo(_ambientMusicSource);
            _ambientMusicSource.Play();
        }
    }

    public void TransitionToRoomMusic(SoundDefinition roomMusic, float roomVolume, bool fadeOutAmbient)
    {
        if (_musicTransitionCoroutine != null) StopCoroutine(_musicTransitionCoroutine);
        _musicTransitionCoroutine = StartCoroutine(TransitionMusicCoroutine(roomMusic, roomVolume, fadeOutAmbient));
    }

    public void TransitionBackToAmbient()
    {
        if (_musicTransitionCoroutine != null) StopCoroutine(_musicTransitionCoroutine);
        _musicTransitionCoroutine = StartCoroutine(TransitionBackToAmbientCoroutine());
    }

    private IEnumerator TransitionMusicCoroutine(SoundDefinition roomMusic, float roomVolume, bool fadeOutAmbient)
    {
        // Setup room music
        if (roomMusic != null)
        {
            roomMusic.ApplyTo(_roomMusicSource);
            _roomMusicSource.volume = 0f;
            _roomMusicSource.Play();
        }

        var elapsed = 0f;
        var startAmbientVolume = _ambientMusicSource.volume;
        var targetAmbientVolume = fadeOutAmbient ? 0f : startAmbientVolume;

        while (elapsed < musicTransitionDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / musicTransitionDuration;

            // Fade in room music
            if (roomMusic != null) _roomMusicSource.volume = Mathf.Lerp(0f, roomVolume, t);

            // Fade out ambient if requested
            if (fadeOutAmbient) _ambientMusicSource.volume = Mathf.Lerp(startAmbientVolume, targetAmbientVolume, t);

            yield return null;
        }

        // Ensure final values
        if (roomMusic != null) _roomMusicSource.volume = roomVolume;
        if (fadeOutAmbient)
        {
            _ambientMusicSource.volume = 0f;
            _ambientMusicSource.Pause();
        }
    }

    private IEnumerator TransitionBackToAmbientCoroutine()
    {
        var elapsed = 0f;
        var startRoomVolume = _roomMusicSource.volume;
        var targetAmbientVolume = defaultAmbientMusic != null ? defaultAmbientMusic.volume : 1f;

        // Resume ambient if it was paused
        if (!_ambientMusicSource.isPlaying && defaultAmbientMusic != null) _ambientMusicSource.UnPause();

        while (elapsed < musicTransitionDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / musicTransitionDuration;

            // Fade out room music
            _roomMusicSource.volume = Mathf.Lerp(startRoomVolume, 0f, t);

            // Fade in ambient music
            if (defaultAmbientMusic != null)
                _ambientMusicSource.volume = Mathf.Lerp(_ambientMusicSource.volume, targetAmbientVolume, t);

            yield return null;
        }

        // Ensure final values and stop room music
        _roomMusicSource.Stop();
        _roomMusicSource.clip = null;
        if (defaultAmbientMusic != null) _ambientMusicSource.volume = targetAmbientVolume;
    }

    // Original methods remain unchanged
    public void PlaySound(SoundDefinition soundDef, Vector3 position)
    {
        if (!soundDef || !soundDef.clip)
        {
            Debug.LogWarning("Tried to play a null Sound Definition or clip.");
            return;
        }

        var source = GetAvailableSource();
        if (!source)
        {
            source = CreatePooledSource();
            Debug.LogWarning("Audio pool exhausted. Growing pool size.");
        }

        source.transform.position = position;
        soundDef.ApplyTo(source);

        source.Play();

        if (!soundDef.loop) StartCoroutine(ReturnSourceToPoolAfterPlaying(source, soundDef.clip.length / source.pitch));
    }

    public void PlayUISound(SoundDefinition soundDef)
    {
        PlaySound(soundDef, transform.position);
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in _pooledSources)
            if (!source.isPlaying && !_continuousSounds.ContainsValue(source))
                return source;
        return null;
    }

    private AudioSource CreatePooledSource()
    {
        var newSourceGo = new GameObject("PooledAudioSource");
        newSourceGo.transform.SetParent(transform);
        var newSource = newSourceGo.AddComponent<AudioSource>();
        _pooledSources.Add(newSource);
        return newSource;
    }

    private IEnumerator ReturnSourceToPoolAfterPlaying(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        source.Stop();
        source.clip = null;
    }

    #region continuous sound

    // New method to play continuous sound attached to a moving GameObject
    public void PlayContinuousSound(SoundDefinition soundDef, GameObject target)
    {
        if (!soundDef || !soundDef.clip || !target)
        {
            Debug.LogWarning("Tried to play a null Sound Definition, clip, or target GameObject.");
            return;
        }

        // Check if this GameObject already has a continuous sound
        if (_continuousSounds.ContainsKey(target))
            // Stop the existing sound first
            StopContinuousSound(target);

        var source = GetAvailableSource();
        if (!source)
        {
            source = CreatePooledSource();
            Debug.LogWarning("Audio pool exhausted. Growing pool size.");
        }

        // Parent the audio source to the target GameObject
        source.transform.SetParent(target.transform);
        source.transform.localPosition = Vector3.zero;

        soundDef.ApplyTo(source);
        source.loop = true; // Ensure continuous sounds loop
        source.Play();

        // Track this continuous sound
        _continuousSounds[target] = source;
    }

    // New method to stop continuous sound on a GameObject
    public void StopContinuousSound(GameObject target)
    {
        if (!target)
        {
            Debug.LogWarning("Tried to stop sound on null GameObject.");
            return;
        }

        // Check if this GameObject has a continuous sound
        if (_continuousSounds.TryGetValue(target, out var source))
        {
            // Stop and clean up the audio source
            source.Stop();
            source.clip = null;
            source.transform.SetParent(transform); // Return to AudioManager
            source.transform.localPosition = Vector3.zero;

            // Remove from tracking dictionary
            _continuousSounds.Remove(target);
        }
        // If no sound is playing on this GameObject, nothing happens (as requested)
    }

    // Optional: Stop all continuous sounds
    public void StopAllContinuousSounds()
    {
        var targets = new List<GameObject>(_continuousSounds.Keys);
        foreach (var target in targets) StopContinuousSound(target);
    }

    // Optional: Check if a GameObject has a continuous sound playing
    public bool HasContinuousSound(GameObject target)
    {
        return target != null && _continuousSounds.ContainsKey(target);
    }

    #endregion
}