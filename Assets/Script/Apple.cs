using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Apple : MonoBehaviour
{
    public static event Action<Apple> Picked;
    private AppleSpawner _appleSpawner = null;
    public AppleType appleType;
    public GameObject appleAnim;

    private float minTouch = 3f;
    private float touchStarted = 0f;

    private bool isAnimPlaying = false;
    public bool isGrabbed { get; private set; } = false;

    private XRGrabInteractable grabInteractable;

    private GridAppleSpawner parentSpawner;

    private void Start()
    {
        parentSpawner = transform.parent.GetComponent<GridAppleSpawner>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to XR Interaction events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        // Clean up the event listeners to avoid memory leaks
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        parentSpawner.grabEffect.FireEffect(transform.position);
        parentSpawner.OnGrabbed();
        isGrabbed = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        parentSpawner.OnReleased();
        isGrabbed = false;
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
    }

    public void HeldEnough()
    {
        if (_appleSpawner.spawnLevel == AppleSpawner.SpawnLevel.Level6)
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
        Picked?.Invoke(this);
        Destroy(gameObject);
    }
}
