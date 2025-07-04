using System;
using UnityEngine;
using UnityEngine.Events;

public class Powercell : MonoBehaviour
{
    public Transform respawnPoint;
    public GameObject Explosion;
    public float targetCharge = 4;

    private float currentCharge = -1;

    public UnityEvent OnCorrectChargeEvent;
    public UnityEvent OnWrongChargeEvent;

    private ParticleSystem correctChargeParticleSystem;

    private void Awake()
    {
        correctChargeParticleSystem = GetComponentInChildren<ParticleSystem>();
        if (correctChargeParticleSystem == null)
            Debug.LogError("Correct charge particle system not found in children!");
        correctChargeParticleSystem.Stop();
    }

    public void SetCharge(float charge)
    {
        Debug.Log($"Setting charge to {charge} for {gameObject.name}");
        currentCharge = charge;

        if (Math.Abs(targetCharge - charge) < 1e-3)
        {
            OnCorrectCharge();
        }
        else
        {
            PlayExplosion();
            OnWrongChargeEvent.Invoke();
        }
    }


    public void OnCorrectCharge()
    {
        correctChargeParticleSystem.Play();
        OnCorrectChargeEvent.Invoke();
        // set shader or other stuff etc
    }

    public bool CorrectCharge()
    {
        return Math.Abs(currentCharge - targetCharge) < 1e-3;
    }

    public void DestroyPowercellAndRespawn()
    {
        currentCharge = -1;
        correctChargeParticleSystem.Stop();
        PlayExplosion();
        transform.position = respawnPoint.position;
    }

    public void PlayExplosion()
    {
        var explosion = Instantiate(Explosion);
        Destroy(explosion, 2);
        explosion.transform.position = transform.position;
    }
}