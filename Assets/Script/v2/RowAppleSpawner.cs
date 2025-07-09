using UnityEngine;
using DG.Tweening;

/// Spawns a single arc “row” of apples in front of the headset.
/// Supports four modes:
///   • Reach  – touch to collect
///   • Grip   – carry while holding
///   • Carry  – carry & release near basket
///   • Sort   – drop healthy vs rotten into matching baskets
///
/// All public methods clear any previous row before spawning.
public class RowAppleSpawner : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [SerializeField] GameObject applePrefab;
    [SerializeField] Material   healthyMat;
    [SerializeField] Material   rottenMat;            // used in Sort mode

    public bool RowEmpty        => transform.childCount == 0;
    public int  CurrentRowCount => transform.childCount;

    /*──────────── PUBLIC API ────────────*/

    public void ClearRow()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
    }

    /* Reach (Mod-1) */
    public void SpawnRow(int count, float h, float d, float spanDeg)
    {
        ClearRow();
        SpawnRowInternal(count, h, d, spanDeg, null, SpawnMode.Reach);
    }

    /* Grip (Mod-2) */
    public void SpawnRowGrip(ReachLevel lv, Transform basket)
    {
        ClearRow();
        SpawnRowInternal(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg,
                         basket, SpawnMode.Grip);
    }

    /* Carry (Mod-3) */
    public void SpawnRowCarry(ReachLevel lv, Transform basket)
    {
        ClearRow();
        SpawnRowInternal(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg,
                         basket, SpawnMode.Carry);
    }

    /* Sort (Mod-4) : need two baskets */
    public void SpawnRowSort(ReachLevel lv,
                             Transform healthyBasket, Transform rottenBasket)
    {
        ClearRow();
        SpawnRowInternal(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg,
                         healthyBasket, SpawnMode.Sort, rottenBasket);
    }

    /*──────────── INTERNAL IMPLEMENTATION ────────────*/

    enum SpawnMode { Reach, Grip, Carry, Sort }

    void SpawnRowInternal(int count, float h, float dist, float spanDeg,
                          Transform basketA, SpawnMode mode,
                          Transform basketB = null)
    {
        if (!applePrefab || !healthyMat)
        {
            Debug.LogError("RowAppleSpawner: Prefab / materials missing.", this);
            return;
        }

        /* camera-relative positions */
        Transform cam    = Camera.main.transform;
        Vector3   fwd    = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3   basePos= cam.position + Vector3.up * h;

        float stepDeg = count == 1 ? 0f : spanDeg / (count - 1);

        for (int i = 0; i < count; i++)
        {
            /* position along arc */
            float ang  = -spanDeg * 0.5f + stepDeg * i;
            Vector3 pos = basePos + Quaternion.AngleAxis(ang, Vector3.up) * (fwd * dist);

            /* instantiate */
            var apple = Instantiate(applePrefab, pos, Quaternion.identity, transform);

            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(Vector3.one * 0.04f, 0.3f)
                 .SetEase(Ease.OutBack);

            /* default material */
            apple.transform.GetChild(0).GetComponent<Renderer>().material = healthyMat;

            /* attach mode-specific behaviour */
            switch (mode)
            {
                case SpawnMode.Reach:
                    apple.AddComponent<AppleReachTarget>();
                    break;

                case SpawnMode.Grip:
                    var grip = apple.AddComponent<AppleGripTarget>();
                    //grip.Init(basketA);
                    break;

                case SpawnMode.Carry:
                    var carry = apple.AddComponent<AppleCarryTarget>();
                    carry.Init(basketA);
                    break;

                case SpawnMode.Sort:
                    /* randomise type */
                    AppleKind kind = (Random.value < 0.5f)
                                   ? AppleKind.Healthy
                                   : AppleKind.Rotten;

                    var sort = apple.AddComponent<AppleSortTarget>();
                    sort.Init(basketA, basketB, kind);

                    /* colour by type */
                    var rend = apple.transform.GetChild(0).GetComponent<Renderer>();
                    rend.material = (kind == AppleKind.Healthy || rottenMat == null)
                                    ? healthyMat
                                    : rottenMat;
                    break;
            }
        }
    }
}
