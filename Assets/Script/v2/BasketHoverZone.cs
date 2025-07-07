using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Success trigger: an apple that is STILL BEING HELD enters the basket zone.
[RequireComponent(typeof(Collider))]
public class BasketHoverZone : MonoBehaviour
{
    GrabEffect fx;

    void Awake()
    {
        fx = GameModeController.Instance?.GrabFx;
    }

    void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponent<AppleGripTarget>();
        if (target == null) return;

        if (!other.TryGetComponent(out XRGrabInteractable gi) || !gi.isSelected)
            return;                         // not held → ignore

        // success!
        fx?.FireEffect(other.transform.position);

        target.MarkProcessed();             // prevent failure callback
        GripActivityManager.Instance?.NotifySuccess(true);

        Destroy(other.gameObject);          // remove apple
    }
}