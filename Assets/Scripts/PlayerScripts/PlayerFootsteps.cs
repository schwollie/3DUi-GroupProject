using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Continuous Movement Settings")]
    [Tooltip("The list of footstep sounds to be chosen from randomly for continuous movement.")]
    [SerializeField] private List<SoundDefinition> footstepSounds;

    [Tooltip("The speed at which the Step Interval is calculated (e.g., your average walking speed).")]
    [SerializeField] private float referenceSpeed = 2.0f;
    [Tooltip("The time in seconds between each footstep when moving at the Reference Speed.")]
    [SerializeField] private float stepIntervalAtReferenceSpeed = 0.5f;

    [Tooltip("The minimum speed the player must be moving at to trigger footsteps.")]
    [SerializeField] private float minVelocityThreshold = 0.1f;

    [Header("Teleportation Settings")]
    [Tooltip("Reference to the TeleportationProvider on your XR Rig.")]
    [SerializeField] private TeleportationProvider teleportationProvider;

    private CharacterController _characterController;
    private Vector3 _lastPosition;
    private float _stepTimer;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (teleportationProvider)
        {
            teleportationProvider.locomotionEnded += OnTeleportationEnded;
        }
    }

    private void OnDisable()
    {
        if (teleportationProvider)
        {
            teleportationProvider.locomotionEnded -= OnTeleportationEnded;
        }
    }

    private void OnTeleportationEnded(LocomotionProvider provider)
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
            _stepTimer = 0.2f; 
            _lastPosition = transform.position;
            return;
        }

        Vector3 currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = transform.position;

        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (currentSpeed > minVelocityThreshold)
        {
            _stepTimer -= Time.deltaTime;

            if (_stepTimer <= 0f)
            {
                PlayFootstepSound();
                _stepTimer = stepIntervalAtReferenceSpeed * (referenceSpeed / currentSpeed);
            }
        }
        else
        {
            _stepTimer = 0.2f;
        }
    }

    private void PlayFootstepSound()
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