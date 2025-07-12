using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [Header("Audio Settings")] [Tooltip("The sound to play")]
    public SoundDefinition soundToPlay;

    [Tooltip("Where to play the sound (leave empty to use this GameObject's position)")]
    public Transform playPosition;

    /// <summary>
    ///     Play the configured sound at the specified position
    /// </summary>
    public void PlayAudioAt()
    {
        if (soundToPlay == null)
        {
            Debug.LogWarning($"AudioPlayer on {gameObject.name}: No sound definition assigned!");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }

        // Use the specified transform position, or this GameObject's position if none specified
        var position = playPosition != null ? playPosition.position : transform.position;

        // If the sound is set to loop, use continuous sound so we can stop it later
        if (soundToPlay.loop)
            // Use this GameObject as the target for continuous sounds
            AudioManager.Instance.PlayContinuousSound(soundToPlay, gameObject);
        else
            // For non-looping sounds, just play normally
            AudioManager.Instance.PlaySound(soundToPlay, position);
    }

    /// <summary>
    ///     Stop any audio playing from this AudioPlayer
    /// </summary>
    public void StopAudio()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager instance not found!");
            return;
        }

        // Stop any continuous sound attached to this GameObject
        AudioManager.Instance.StopContinuousSound(gameObject);
    }
}