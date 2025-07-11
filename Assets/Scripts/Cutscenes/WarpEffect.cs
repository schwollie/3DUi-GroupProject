using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class WarpEffect : MonoBehaviour
{
    public VisualEffect warpSpeedVFX;
    public float rate = 0.02f;

    [SerializeField] private float warpDuration = 20f;
    [SerializeField] private string nextSceneName = "CutsceneEnd";

    private bool warpActive;

    private void Start()
    {
        warpSpeedVFX.Stop();
        warpSpeedVFX.SetFloat("WarpAmount", 0);
    }

    public void ActivateWarpEffect()
    {
        warpActive = true;
        StartCoroutine(ActivateParticles());
        StartCoroutine(WarpTimer());
    }

    public void DeactivateWarpEffect()
    {
        warpActive = false;
        StartCoroutine(ActivateParticles());
    }

    IEnumerator ActivateParticles()
    {
        if (warpActive)
        {
            warpSpeedVFX.Play();

            float amount = warpSpeedVFX.GetFloat("WarpAmount");

            while (amount < 1 & warpActive)
            {
                amount += rate;
                warpSpeedVFX.SetFloat("WarpAmount", amount);
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            float amount = warpSpeedVFX.GetFloat("WarpAmount");

            while (amount > 0 & !warpActive)
            {
                amount -= rate;
                warpSpeedVFX.SetFloat("WarpAmount", amount);
                yield return new WaitForSeconds(0.1f);

                if(amount <= 0 - rate)
                {
                    amount = 0;
                    warpSpeedVFX.SetFloat("WarpAmount", amount);

                    warpSpeedVFX.Stop();
                }
            }
        }
    }

    IEnumerator WarpTimer()
    {
        yield return new WaitForSeconds(warpDuration);
        SceneManager.LoadScene(nextSceneName);
    }
}
