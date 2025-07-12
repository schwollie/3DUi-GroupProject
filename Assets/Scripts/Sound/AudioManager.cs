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

        CreateMusicSources();

        if (defaultAmbientMusic != null) PlayDefaultAmbientMusic();
    }

    private void OnDestroy()
    {
        StopAllContinuousSounds();
    }

    private void Update()
    {
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in _continuousSounds)
            if (!kvp.Key)
            {
                kvp.Value.Stop();
                kvp.Value.clip = null;
                kvp.Value.transform.SetParent(transform);
                kvp.Value.transform.localPosition = Vector3.zero;
                keysToRemove.Add(kvp.Key);
            }

        foreach (var key in keysToRemove) _continuousSounds.Remove(key);
    }


    private void CreateMusicSources()
    {
        var ambientGo = new GameObject("AmbientMusicSource");
        ambientGo.transform.SetParent(transform);
        _ambientMusicSource = ambientGo.AddComponent<AudioSource>();
        _ambientMusicSource.loop = true;
        _ambientMusicSource.spatialBlend = 0f;

        var roomGo = new GameObject("RoomMusicSource");
        roomGo.transform.SetParent(transform);
        _roomMusicSource = roomGo.AddComponent<AudioSource>();
        _roomMusicSource.loop = true;
        _roomMusicSource.spatialBlend = 0f;
    }

    public void PlayDefaultAmbientMusic()
    {
        if (defaultAmbientMusic && _ambientMusicSource)
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
        if (roomMusic)
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

            if (roomMusic) _roomMusicSource.volume = Mathf.Lerp(0f, roomVolume, t);

            if (fadeOutAmbient) _ambientMusicSource.volume = Mathf.Lerp(startAmbientVolume, targetAmbientVolume, t);

            yield return null;
        }

        if (roomMusic) _roomMusicSource.volume = roomVolume;
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
        var targetAmbientVolume = defaultAmbientMusic ? defaultAmbientMusic.volume : 1f;

        if (!_ambientMusicSource.isPlaying && defaultAmbientMusic) _ambientMusicSource.UnPause();

        while (elapsed < musicTransitionDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / musicTransitionDuration;

            _roomMusicSource.volume = Mathf.Lerp(startRoomVolume, 0f, t);

            if (defaultAmbientMusic)
                _ambientMusicSource.volume = Mathf.Lerp(_ambientMusicSource.volume, targetAmbientVolume, t);

            yield return null;
        }

        _roomMusicSource.Stop();
        _roomMusicSource.clip = null;
        if (defaultAmbientMusic) _ambientMusicSource.volume = targetAmbientVolume;
    }

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

    public void PlayContinuousSound(SoundDefinition soundDef, GameObject target)
    {
        if (!soundDef || !soundDef.clip || !target)
        {
            Debug.LogWarning("Tried to play a null Sound Definition, clip, or target GameObject.");
            return;
        }

        if (_continuousSounds.ContainsKey(target))
            StopContinuousSound(target);

        var source = GetAvailableSource();
        if (!source)
        {
            source = CreatePooledSource();
            Debug.LogWarning("Audio pool exhausted. Growing pool size.");
        }

        source.transform.SetParent(target.transform);
        source.transform.localPosition = Vector3.zero;

        soundDef.ApplyTo(source);
        source.loop = true;
        source.Play();

        _continuousSounds[target] = source;
    }

    public void StopContinuousSound(GameObject target)
    {
        if (!target)
        {
            Debug.LogWarning("Tried to stop sound on null GameObject.");
            return;
        }

        if (_continuousSounds.TryGetValue(target, out var source) && transform)
        {
            source.Stop();
            source.clip = null;
            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            _continuousSounds.Remove(target);
        }
    }

    public void StopAllContinuousSounds()
    {
        var targets = new List<GameObject>(_continuousSounds.Keys);
        foreach (var target in targets) StopContinuousSound(target);
    }

    public bool HasContinuousSound(GameObject target)
    {
        return target != null && _continuousSounds.ContainsKey(target);
    }

    #endregion
}