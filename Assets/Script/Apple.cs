using System;
using UnityEngine;

public class Apple : MonoBehaviour
{
    private AppleSpawner _appleSpawner = null;
    public AppleType appleType;
    
    private float minTouch = 3f;
    private float touchStarted = 0f;

    private void Start()
    {
        _appleSpawner = FindAnyObjectByType<AppleSpawner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            Debug.Log("Apple entered");
            if(_appleSpawner.spawnLevel < AppleSpawner.SpawnLevel.Level5)
                _appleSpawner.InteractApple(gameObject);
            touchStarted = Time.time;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            if (Time.time - touchStarted > minTouch)
            {
                if(_appleSpawner.spawnLevel == AppleSpawner.SpawnLevel.Level5 && _appleSpawner.spawnLevel == AppleSpawner.SpawnLevel.Level6 )
                    _appleSpawner.InteractApple(gameObject);
            }
                
        }
    }

    public void HeldEnough()
    {
        if(_appleSpawner.spawnLevel == AppleSpawner.SpawnLevel.Level6)
            _appleSpawner.InteractApple(gameObject);
    }
    
}
