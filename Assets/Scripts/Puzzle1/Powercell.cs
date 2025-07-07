using System;
using UnityEngine;
using UnityEngine.Events;

public class Powercell : MonoBehaviour
{
    public Transform respawnPoint;
    public GameObject Explosion;
    public float targetCharge = 4;
    public SoundDefinition ElectricSparkSound;
    public SoundDefinition ExplosionSound;
    public bool isRed = true; // true for red, false for blue

    private float currentCharge = -1;

    public UnityEvent OnCorrectChargeEvent;
    public UnityEvent OnWrongChargeEvent;

    private ParticleSystem correctChargeParticleSystem;

    private void Awake()
    {
        GetComponent<Light>().enabled = false;
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
            OnCorrectCharge();
        else
            OnUncorrectCharge();
    }


    public void OnCorrectCharge()
    {
        GetComponent<Light>().enabled = true;
        correctChargeParticleSystem.Play();
        OnCorrectChargeEvent.Invoke();
        AudioManager.Instance.PlayContinuousSound(ElectricSparkSound, gameObject);
        // set shader or other stuff etc
    }

    public bool CorrectCharge()
    {
        return Math.Abs(currentCharge - targetCharge) < 1e-3;
    }

    public void OnUncorrectCharge()
    {
        correctChargeParticleSystem.Stop();
        correctChargeParticleSystem.Clear();
        GetComponent<Light>().enabled = false;
        AudioManager.Instance.StopContinuousSound(gameObject);
        OnWrongChargeEvent.Invoke();
    }


    public void DestroyPowercellAndRespawn()
    {
        OnUncorrectCharge();
        currentCharge = -1;
        PlayExplosion();
        GetComponent<Rigidbody>().position = respawnPoint.position;
        GetComponent<Rigidbody>().rotation = respawnPoint.rotation;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    public void PlayExplosion()
    {
        AudioManager.Instance.PlaySound(ExplosionSound, transform.position);
        var explosion = Instantiate(Explosion);
        Destroy(explosion, 1);
        explosion.transform.position = transform.position;
    }
}