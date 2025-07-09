using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class AppleCarryTarget : MonoBehaviour
{
    const float SUCCESS_DIST = 0.15f;       // sepete yakınlık eşiği

    XRGrabInteractable grab;
    Transform basket;
    GrabEffect fx;
    bool processed;

    public void Init(Transform basketTf) => basket = basketTf;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectExited.AddListener(OnSelectExit);
        fx   = GameModeController.Instance?.GrabFx;
    }

    void OnSelectExit(SelectExitEventArgs _)
    {
        if (processed) return;
        processed = true;

        bool success = basket &&
                       Vector3.Distance(transform.position, basket.position) <= SUCCESS_DIST;

        if (success) fx?.FireEffect(transform.position);

        CarryActivityManager.Instance?.NotifySuccess(success);
        Destroy(gameObject);
    }
}