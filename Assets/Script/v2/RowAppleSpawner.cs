using UnityEngine;
using DG.Tweening;

public class RowAppleSpawner : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [SerializeField] GameObject applePrefab;
    [SerializeField] Material   healthyMat;

    public bool RowEmpty        => transform.childCount == 0;
    public int  CurrentRowCount => transform.childCount;

    /*────────── PUBLIC API ─────────*/

    public void ClearRow()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
    }

    public void SpawnRow(int count, float h, float d, float spanDeg)
    {
        ClearRow();
        SpawnRowInternal(count, h, d, spanDeg, null, SpawnMode.Reach);
    }

    public void SpawnRowGrip(ReachLevel lv, Transform basket)
    {
        ClearRow();
        SpawnRowInternal(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg,
                         basket, SpawnMode.Grip);
    }

    public void SpawnRowCarry(ReachLevel lv, Transform basket)
    {
        ClearRow();
        SpawnRowInternal(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg,
                         basket, SpawnMode.Carry);
    }

    /*────────── INTERNAL ─────────*/
    enum SpawnMode { Reach, Grip, Carry }

    void SpawnRowInternal(int count, float h, float d, float span,
                          Transform basket, SpawnMode mode)
    {
        if (!applePrefab || !healthyMat) { Debug.LogError("Spawner refs missing", this); return; }

        Transform cam    = Camera.main.transform;
        Vector3   fwd    = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3   basePos= cam.position + Vector3.up * h;

        float step = count == 1 ? 0f : span / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float ang = -span * .5f + step * i;
            Vector3 pos = basePos + Quaternion.AngleAxis(ang, Vector3.up) * (fwd * d);

            var apple = Instantiate(applePrefab, pos, Quaternion.identity, transform);
            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(Vector3.one * 0.04f, .3f).SetEase(Ease.OutBack);
            apple.transform.GetChild(0).GetComponent<Renderer>().material = healthyMat;

            if (basket == null) continue;

            switch (mode)
            {
                case SpawnMode.Grip:
                    var g = apple.AddComponent<AppleGripTarget>();
                    //g.Init(basket);
                    break;

                case SpawnMode.Carry:
                    var c = apple.AddComponent<AppleCarryTarget>();
                    c.Init(basket);
                    break;
            }
        }
    }
}
