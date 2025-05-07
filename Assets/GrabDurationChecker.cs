using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using System.Linq;

public class GrabDurationChecker : MonoBehaviour
{
    private float requiredGrabTime = 1f; // seconds
    private XRGrabInteractable grabInteractable;
    private float grabStartTime;
    private bool isGrabbed = false;

    private Apple _apple;

    private List<float> grabDurations = new List<float>(); // Stores all grab durations

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        _apple = GetComponent<Apple>();
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        grabStartTime = Time.time;
        isGrabbed = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        float grabDuration = Time.time - grabStartTime;
        Debug.Log($"Grabbed for {grabDuration} seconds");

        grabDurations.Add(grabDuration); // Add to the list

        if (grabDuration >= requiredGrabTime)
        {
            Debug.Log("Held long enough!");
            _apple.HeldEnough();
        }
    }

    // 🔍 Public method to get average grab duration
    public float GetAverageGrabDuration()
    {
        if (grabDurations.Count == 0)
            return 0f;

        return grabDurations.Average();
    }
}