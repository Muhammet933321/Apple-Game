using System;
using UnityEngine;
public enum AppleType
{
    Healthy,
    Rotten
}
public class Basket : MonoBehaviour
{
    public AppleType basketType;
    private AppleSpawner _appleSpawner = null;
    
    private void Start()
    {
        _appleSpawner = FindAnyObjectByType<AppleSpawner>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Apple>().appleType == basketType)
            _appleSpawner.InteractApple(other.gameObject);
    }
}
