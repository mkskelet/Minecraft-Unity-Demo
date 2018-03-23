using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class displays current fps.
/// </summary>
public class FPSCounter : MonoBehaviour {
    public Text fpsCounter;
    int frames = 0;
    float nextFpsUpdate = 0;
    float fpsUpdateTimer = 0.25f;
    
	void Update () {
        if (nextFpsUpdate < Time.realtimeSinceStartup) {
            fpsCounter.text = (1 / fpsUpdateTimer) * frames + " fps";
            frames = 0;

            nextFpsUpdate = Time.realtimeSinceStartup + fpsUpdateTimer;
        }
        frames++;
    }
}
