using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Footstep Settings")]
    [Tooltip("The list of footstep sounds to be chosen from randomly.")]
    [SerializeField] private List<SoundDefinition> footstepSounds;
    
    [Header("Teleportation Setup")]
    [Tooltip("Reference to the TeleportationProvider on your XR Rig.")]
    [SerializeField] private TeleportationProvider teleportationProvider;

    [Tooltip("The time in seconds between each footstep sound while moving.")]
    [SerializeField] private float stepInterval = 0.5f;

    [Tooltip("The minimum speed the player must be moving at to trigger footsteps.")]
    [SerializeField] private float minVelocityThreshold = 0.1f;

    private CharacterController _characterController;
    private Vector3 _lastPosition;    
    private float _stepTimer;
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }
    
    private void OnEnable()
    {
        if (teleportationProvider != null)
        {
            teleportationProvider.locomotionEnded += PlayFootstepSound;
        }
    }
    
    private void OnDisable()
    {
        if (teleportationProvider != null)
        {
            teleportationProvider.locomotionEnded -= PlayFootstepSound;
        }
    }
    
    private void PlayFootstepSound(LocomotionProvider obj)
    {
        PlayFootstepSound();
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (!_characterController.isGrounded)
        {
            _stepTimer = 0f;
            _lastPosition = transform.position;
            return;
        }

        Vector3 currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = transform.position;

        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        if (horizontalVelocity.magnitude > minVelocityThreshold)
        {
            _stepTimer -= Time.deltaTime;

            if (_stepTimer <= 0f)
            {
                PlayFootstepSound();
                _stepTimer = stepInterval;
            }
        }
        else
        {
            _stepTimer = 0f;
        }
    }

    public void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, footstepSounds.Count);
        SoundDefinition soundToPlay = footstepSounds[randomIndex];

        AudioManager.Instance.PlaySound(soundToPlay, transform.position);
    }
}