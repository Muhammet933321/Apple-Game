using UnityEngine;

/// Spawns one basket in front of the headset; keeps reference to remove later.
public class BasketSpawner : MonoBehaviour
{
    [SerializeField] GameObject basketPrefab;
    [SerializeField] Vector3    localOffset = new(0f, -0.4f, 0.6f);

    GameObject current;

    public Transform SpawnOrMoveBasket()
    {
        Transform cam = Camera.main.transform;

        Vector3 fwd   = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        Vector3 pos =
            cam.position
            + right * localOffset.x
            + Vector3.up * localOffset.y
            + fwd   * localOffset.z;

        if (current == null)
            current = Instantiate(basketPrefab, pos, Quaternion.identity, transform);
        else
            current.transform.position = pos;

        current.transform.rotation = Quaternion.identity;   // keep upright
        return current.transform;
    }

    /*────────── remove existing basket ─────────*/
    public void ClearBasket()
    {
        if (current != null)
            Destroy(current);
    }
}