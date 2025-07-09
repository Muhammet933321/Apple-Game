using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public enum AppleKind { Healthy, Rotten }

[RequireComponent(typeof(XRGrabInteractable))]
public class AppleSortTarget : MonoBehaviour
{
    const float SUCCESS_DIST = 0.15f;

    XRGrabInteractable grab;
    Transform healthyBasket, rottenBasket;
    AppleKind kind;
    bool processed;
    GrabEffect fx;

    public void Init(Transform healthy, Transform rotten, AppleKind k)
    {
        healthyBasket = healthy;
        rottenBasket  = rotten;
        kind          = k;
    }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectExited.AddListener(OnSelectExit);
        fx = GameModeController.Instance?.GrabFx;
    }

    void OnSelectExit(SelectExitEventArgs _)
    {
        if (processed) return;
        processed = true;

        Transform targetBasket = (kind == AppleKind.Healthy) ? healthyBasket : rottenBasket;
        bool success = targetBasket &&
                       Vector3.Distance(transform.position, targetBasket.position) <= SUCCESS_DIST;

        if (success) fx?.FireEffect(transform.position);

        SortActivityManager.Instance?.NotifySuccess(success);
        Destroy(gameObject);
    }
}