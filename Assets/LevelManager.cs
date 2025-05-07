using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{

    public AppleSpawner appleSpawner;
    public bool isCalibrationEnabled;
    private bool isCalibrating = false;

    private void Start()
    {
        if (isCalibrationEnabled)
            ActivateCalibration();
    }

    public void ActivateCalibration()

    {
        // Elma 0, 1, z de elmalar spawn et
        // Hangisine dokunulursa kalibre et
       StartCoroutine(Calibrate());
    }

    public void AppleInteracted()
    {
        if (isCalibrating)
        {
            isCalibrating = false;
            appleSpawner.SetLevel(AppleSpawner.SpawnLevel.Level1_XLine);
        }
    }

    IEnumerator Calibrate()
    {
        isCalibrating = true;
        GameObject go = appleSpawner.SpawnAppleCalibration();
        yield return new WaitForSeconds(5);
        appleSpawner.RemoveApple(go);
        if (isCalibrating)
        {
            go = appleSpawner.SpawnAppleCalibration();
        }
        yield return new WaitForSeconds(5);
        appleSpawner.RemoveApple(go);
        if (isCalibrating)
        {
            go = appleSpawner.SpawnAppleCalibration();
        }
        yield return new WaitForSeconds(5);
        appleSpawner.RemoveApple(go);
        if (isCalibrating)
        {
            go = appleSpawner.SpawnAppleCalibration();
        }
        yield return new WaitForSeconds(5);
        appleSpawner.RemoveApple(go); 
        isCalibrating = false;
        appleSpawner.SetLevel(AppleSpawner.SpawnLevel.Level1_XLine);
    }

    public void ActivateLevel1()
    {
        
    }
}
