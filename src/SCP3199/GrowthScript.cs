using System;
using System.Collections;
using UnityEngine;

namespace SCP3199.SCP3199;

public class GrowthScript : MonoBehaviour
{

    public SCP3199AI mainScript;
    // Growth Egg
    public GameObject eggGameObject;
    private Vector3 initialScale1 = new Vector3(0.1f, 0.1f, 0.1f);
    private Vector3 targetScale1 = new Vector3(1.464502f, 1.464502f, 1.464502f);
    private float growthDuration1 = 100f; // Growth duration in seconds (30 seconds)

    // Growth 2 variables
    private Vector3 initialScale2;
    private Vector3 targetScale2 = new Vector3(1.5f, 1.5f, 1.9f);
    private float growthDuration2 = 60f; // Growth duration in seconds (1 minute)

    void Start()
    {
        // Start both coroutines
        StartCoroutine(GrowEgg());
        eggGameObject.transform.localScale = initialScale1;
    }

    IEnumerator GrowEgg()
    {
        float elapsedTime = 0f;

        while (elapsedTime < growthDuration1)
        {
            // Calculate the fraction of time passed
            float t = elapsedTime / growthDuration1;
            
            if (elapsedTime >= 30f && mainScript.stageOfGrowth == 0)
            {
                mainScript.ExitEggPhaseClientRpc();
            }
            if (elapsedTime >= 70f && mainScript.stageOfGrowth == 1)
            {
                mainScript.stageOfGrowth = 2;
            }
            // Lerp the scale
            mainScript.self.creatureAnimator.speed = Mathf.Lerp(2.3f, 1.3f,t);
            eggGameObject.transform.localScale = Vector3.Lerp(initialScale1, targetScale1, t);

            // Increment the elapsed time
            elapsedTime += Time.deltaTime;

            // Wait for the next frame before continuing the loop
            yield return null;
        }

        // Ensure final values are set correctly
        eggGameObject.transform.localScale = targetScale1;
        mainScript.stageOfGrowth=1;
    }
}