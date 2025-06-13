using System;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    public GridAppleSpawner spawner;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Controller")
        {
            spawner.OnStartButton();
            gameObject.SetActive(false);
        }
    }
}
