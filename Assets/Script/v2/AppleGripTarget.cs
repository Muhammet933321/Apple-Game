using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Grip-mode apple logic.
/// • Holds an XRGrabInteractable so it can be picked up.
/// • If the player RELEASES the apple before a success trigger fires,
///   it reports failure.
/// • Success is reported by BasketHoverZone; this script then never fires.
[RequireComponent(typeof(XRGrabInteractable))]
public class AppleGripTarget : MonoBehaviour
{
    XRGrabInteractable grab;
    bool processed;                         // success or fail already sent?

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectExited.AddListener(OnSelectExit);
    }

    void OnSelectExit(SelectExitEventArgs _)
    {
        if (processed) return;              // success already handled
        processed = true;

        GripActivityManager.Instance?.NotifySuccess(false);   // failure
        Destroy(gameObject);
    }

    /// Called by BasketHoverZone when success happens
    public void MarkProcessed() => processed = true;
}