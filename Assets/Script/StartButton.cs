using System;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    public GridAppleSpawner spawner;
    private void OnTriggerEnter(Collider other)
    {
        spawner.OnStartButton();
        gameObject.SetActive(false);
    }
}
