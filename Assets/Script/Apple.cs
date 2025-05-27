using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Apple : MonoBehaviour
{
    public static event Action<Apple> PickedCorrectBasket;
    public static event Action<Apple> PickedWrongBasket;
    public AppleType appleType;
    public bool isGrabbed { get; private set; } = false;
    public bool isReleased { get; private set; } = false;

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
        Debug.Log("Hey");
        if (isReleased) return;
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;
        parentSpawner.OnReleased(transform.position, this);
        isGrabbed = false;
        isReleased = true;
    }
    
    public void Pick(bool isCorrectBasket)
    {
        if (isCorrectBasket) PickedCorrectBasket?.Invoke(this); else PickedWrongBasket?.Invoke(this);
        Destroy(GetComponent<Apple>());
    }
}
