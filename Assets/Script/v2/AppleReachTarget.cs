using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AppleReachTarget : MonoBehaviour
{
    RowAppleSpawner owner;
    GrabEffect      fx;

    public void Init(RowAppleSpawner sp, GrabEffect effect)
    {
        owner = sp;
        fx = effect;
        GetComponent<Collider>().isTrigger = true;    // ensure trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Controller")) return;     // tag your finger colliders

        fx?.FireEffect(transform.position);           // pop VFX
        owner.OnAppleCollected(gameObject);
    }
}