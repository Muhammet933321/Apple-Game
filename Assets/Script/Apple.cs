using System;
using UnityEngine;

public class Apple : MonoBehaviour
{
    public static event Action<Apple> Picked;
    private AppleSpawner _appleSpawner = null;
    public AppleType appleType;
    public GameObject appleAnim;
    
    private float minTouch = 3f;
    private float touchStarted = 0f;
    
    private bool isAnimPlaying = false;

    private void Start()
    {
        _appleSpawner = FindAnyObjectByType<AppleSpawner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        isAnimPlaying = true;
        if (other.CompareTag("Controller"))
        {
            touchStarted = Time.time;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        isAnimPlaying = false;
        if (other.CompareTag("Controller"))
        {
                
        }
    }

    public void HeldEnough()
    {
        if(_appleSpawner.spawnLevel == AppleSpawner.SpawnLevel.Level6)
            _appleSpawner.InteractApple(gameObject);
    }

    private void Update()
    {
        if (isAnimPlaying && !appleAnim.activeSelf)
        {
            appleAnim.SetActive(true);
        }
        if (!isAnimPlaying && appleAnim.activeSelf)
        {
            appleAnim.SetActive(false);
        }
    }
    
    public void Pick()
    {
        Picked?.Invoke(this);   // fire the event
        Destroy(gameObject);    // remove the old apple
    }
}
