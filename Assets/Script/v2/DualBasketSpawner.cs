using UnityEngine;

/// Spawns two baskets (left = healthy, right = rotten) using THE SAME prefab.
/// Only the material on the visible mesh is swapped.
public class DualBasketSpawner : MonoBehaviour
{
    [Header("Common basket prefab")]
    [SerializeField] GameObject basketPrefab;          // single prefab

    [Header("Materials")]
    [SerializeField] Material healthyMat;
    [SerializeField] Material rottenMat;

    [Header("Offsets (camera-relative)")]
    [SerializeField] Vector3 centreOffset = new(0f, -0.4f, 0.7f);
    [SerializeField] float   halfGap      = 0.25f;     // ±X from centre

    GameObject healthyGO, rottenGO;

    /// Returns (healthyBasket, rottenBasket) transforms
    public (Transform healthy, Transform rotten) SpawnOrMove()
    {
        Transform cam = Camera.main.transform;

        Vector3 fwd   = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        Vector3 centre = cam.position
                       + right * centreOffset.x
                       + Vector3.up * centreOffset.y
                       + fwd    * centreOffset.z;

        Vector3 posHealthy = centre - right * halfGap;
        Vector3 posRotten  = centre + right * halfGap;

        /* Healthy basket */
        if (healthyGO == null)
            healthyGO = Instantiate(basketPrefab, posHealthy, Quaternion.identity, transform);
        else
            healthyGO.transform.position = posHealthy;

        /* Rotten basket */
        if (rottenGO == null)
            rottenGO = Instantiate(basketPrefab, posRotten, Quaternion.identity, transform);
        else
            rottenGO.transform.position = posRotten;

        /* keep upright */
        healthyGO.transform.rotation = Quaternion.identity;
        rottenGO.transform.rotation  = Quaternion.identity;

        /* apply materials */
        ApplyMat(healthyGO, healthyMat);
        ApplyMat(rottenGO,  rottenMat);

        return (healthyGO.transform, rottenGO.transform);
    }

    void ApplyMat(GameObject go, Material mat)
    {
        if (!mat) return;
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend) rend.material = mat;
    }

    public void ClearBaskets()
    {
        if (healthyGO) Destroy(healthyGO);
        if (rottenGO)  Destroy(rottenGO);
    }
}
