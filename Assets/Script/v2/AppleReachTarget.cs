using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AppleReachTarget : MonoBehaviour
{
    GrabEffect fx;     // artık Inspector’da değil

    void Awake()
    {
        fx = GameModeController.Instance?.GrabFx;

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        /* 0️⃣  Only care in Reach mode */
        if (GameModeController.Instance == null ||
            GameModeController.Instance.Current.Mode != TherapyMode.Reach)
            return;

        if (!other.CompareTag("Controller")) return;

        fx?.FireEffect(transform.position);
        ReachActivityManager.Instance?.NotifySuccess(true);
        Destroy(gameObject);
    }

}