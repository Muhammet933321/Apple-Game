using UnityEngine;

/// Kameraya göre konumlanan tek sepet.
public class BasketSpawner : MonoBehaviour
{
    [SerializeField] GameObject basketPrefab;
    [SerializeField] Vector3    localOffset = new(0f, -0.4f, 0.6f); // x-right, y-up, z-forward

    GameObject current;

    public Transform SpawnOrMoveBasket()
    {
        Transform cam = Camera.main.transform;

        Vector3 camFwd  = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight= Vector3.Cross(Vector3.up, camFwd).normalized;

        Vector3 worldPos =
            cam.position
            + camRight * localOffset.x
            + Vector3.up * localOffset.y
            + camFwd    * localOffset.z;

        if (current == null)
            current = Instantiate(basketPrefab, worldPos, Quaternion.identity, transform);
        else
            current.transform.position = worldPos;

        return current.transform;
    }
}