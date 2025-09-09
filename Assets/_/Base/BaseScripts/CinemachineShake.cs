using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class CinemachineShake : MonoBehaviour {

    public static CinemachineShake Instance { get; private set; }

    public static void ScreenShake_Static(float intensity = 1f, float timer = .1f) {
        if (Instance == null) {
            
        }
        Instance.ScreenShake(intensity, timer);
    }





    private void Awake() {
        Instance = this;
    }

    public void ScreenShake(float intensity = 1f, float timer = .1f) {
       
    }

}
