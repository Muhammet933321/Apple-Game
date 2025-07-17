using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// Kamera uzayına dayalı elma spawner.
/// ------------------------------------------------------------
/// Özellikler:
/// • Grid: X (vars 8), Y (5), Z (5) aralığı; inspector’dan min/max.
/// • Layout: Line (sağ‑sol sıra) veya ArcHorizontal (ayarlanabilir yatay yay).
/// • Level spawn: levelIndex 0..4 → Y & Z eksenlerinden nokta.
/// • Custom spawn: harici List<Vector3Int> grid indeksleri.
/// • Sağ / Sol el seçimine göre tüm dağılım yan tarafa kayar.
/// • Sort modunda sağlıklı / çürük materyal desteği.
/// ------------------------------------------------------------
public class RowAppleSpawner : MonoBehaviour
{
    /*──────────────── Prefabs & Materials ────────────────*/
    [Header("Prefabs & Materials")]
    [SerializeField] GameObject applePrefab;
    [SerializeField] Material   healthyMat;
    [SerializeField] Material   rottenMat;     // Sort

    /*──────────────── Camera Anchor ──────────────────────*/
    [Header("Camera Anchor")]
    [Tooltip("Elmaların hizalanacağı kamera. Boşsa Camera.main bulunur.")]
    [SerializeField] Camera targetCamera;
    [Tooltip("Kamera merkezine eklenecek dünya uzayı offset (örn. göz -> göğüs).")]
    [SerializeField] Vector3 camSpaceYOffset = Vector3.zero;

    /*──────────────── El / Taraf Seçimi ─────────────────*/
    public enum HandSide { Right, Left }
    [Header("Hand Selection")]
    [SerializeField] HandSide activeHand = HandSide.Right;
    [Tooltip("Kameradan yana kaydırma (m). Sağ el +X, sol el -X.")]
    [SerializeField] float handSideOffset = 0.15f;

    public void SetActiveHand(HandSide h) => activeHand = h;
    public void ToggleHand() =>
        activeHand = (activeHand == HandSide.Right) ? HandSide.Left : HandSide.Right;

    /*──────────────── Layout ─────────────────────────────*/
    public enum Layout { Line, ArcHorizontal }
    [Header("Layout")]
    [SerializeField] Layout layout = Layout.Line;
    [Tooltip("Arc toplam açısı (derece). Örn. 60 => -30 .. +30.")]
    [SerializeField] float arcAngleDeg = 60f;
    [Tooltip("Arc modunda Z mesafesine eklenecek ileri/geri sapma (m).")]
    [SerializeField] float arcCenterBias = 0f;

    public enum RotationMode { PrefabRotation, MatchCamera, FaceCameraBillboard }
    [Tooltip("Elmanın rotasyonu.")]
    [SerializeField] RotationMode rotationMode = RotationMode.MatchCamera;

    /*──────────────── Grid Aralıkları (kamera eksenleri) ─*/
    [Header("Grid Ranges (relative to camera axes)")]
    [SerializeField] float xMin = -0.40f;
    [SerializeField] float xMax =  0.40f;
    [SerializeField] float yMin = -0.10f;
    [SerializeField] float yMax =  0.40f;
    [SerializeField] float zMin =  0.60f;
    [SerializeField] float zMax =  1.80f;

    /*──────────────── Grid Counts ───────────────────────*/
    [Header("Grid Counts")]
    [SerializeField, Min(1)] int xCount = 8;  // 0..7
    [SerializeField, Min(1)] int yCount = 5;  // 0..4
    [SerializeField, Min(1)] int zCount = 5;  // 0..4

    /*──────────────── Spawn FX ──────────────────────────*/
    [Header("Spawn FX")]
    [SerializeField] float appleScale    = 0.04f;
    [SerializeField] float spawnTweenDur = 0.30f;
    [SerializeField] Ease  spawnEase     = Ease.OutBack;

    /*──────────────── Dahili ────────────────────────────*/
    enum SpawnMode { Reach, Grip, Carry, Sort }
    float[] xs, ys, zs;    // grid değerleri (metre)

    void Awake()      => BuildGrid();
    void OnValidate() => BuildGrid();

    /*──────────────── PUBLIC TEMİZLİK ───────────────────*/
    public void ClearRow()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    /*──────────────── PUBLIC LEVEL SPAWN API ────────────*/
    public void SpawnReachLevel(int level)                                  => SpawnLevel(level, SpawnMode.Reach, null, null);
    public void SpawnGripLevel (int level, Transform basket)                => SpawnLevel(level, SpawnMode.Grip,  basket, null);
    public void SpawnCarryLevel(int level, Transform basket)                => SpawnLevel(level, SpawnMode.Carry, basket, null);
    public void SpawnSortLevel (int level, Transform healthy, Transform ro) => SpawnLevel(level, SpawnMode.Sort,  healthy, ro);

    /*──────────────── PUBLIC CUSTOM SPAWN API ───────────*/
    public void SpawnCustomReach(List<Vector3Int> coords)                                               => SpawnCustom(coords, SpawnMode.Reach, null,     null,     null);
    public void SpawnCustomGrip (List<Vector3Int> coords, Transform basket)                             => SpawnCustom(coords, SpawnMode.Grip,  basket,   null,     null);
    public void SpawnCustomCarry(List<Vector3Int> coords, Transform basket)                             => SpawnCustom(coords, SpawnMode.Carry, basket,   null,     null);
    public void SpawnCustomSort (List<Vector3Int> coords, Transform healthy, Transform ro, List<AppleKind> kinds = null)
                                                                                                        => SpawnCustom(coords, SpawnMode.Sort,  healthy, ro, kinds);

    /*──────────────── GRID HESAP ────────────────────────*/
    void BuildGrid()
    {
        xs = BuildAxis(xMin, xMax, xCount);
        ys = BuildAxis(yMin, yMax, yCount);
        zs = BuildAxis(zMin, zMax, zCount);
    }

    static float[] BuildAxis(float min, float max, int count)
    {
        if (count <= 1) return new[] { (min + max) * 0.5f };
        float[] arr = new float[count];
        float step  = (max - min) / (count - 1);
        for (int i = 0; i < count; i++) arr[i] = min + step * i;
        return arr;
    }

    /*──────────────── LEVEL SPAWN İÇ ────────────────────*/
    void SpawnLevel(int level, SpawnMode mode, Transform basketA, Transform basketB)
    {
        if (!ValidatePrefab()) return;
        var cam = GetCam();   if (!cam) return;

        ClearRow();
        BuildGrid();

        int li = Mathf.Clamp(level, 0, Mathf.Min(yCount, zCount) - 1);
        float yy = ys[li];
        float zz = zs[li];

        // kamera baz & el offset
        GetSpawnBasis(cam.transform, out var basisPos, out var right, out var up, out var fwd);

        if (layout == Layout.Line)
        {
            for (int xi = 0; xi < xCount; xi++)
            {
                Vector3 pos = basisPos + right * xs[xi] + up * yy + fwd * zz;
                SpawnApple(pos, cam.transform, mode, basketA, basketB, AppleKind.Healthy);
            }
        }
        else // Arc
        {
            float radius = Mathf.Max(0f, zz + arcCenterBias);
            for (int xi = 0; xi < xCount; xi++)
            {
                float ang = IndexToAngle(xi, xCount, arcAngleDeg);
                Vector3 dir = Quaternion.AngleAxis(ang, up) * fwd;
                Vector3 pos = basisPos + up * yy + dir * radius;
                SpawnApple(pos, cam.transform, mode, basketA, basketB, AppleKind.Healthy);
            }
        }
    }

    /*──────────────── CUSTOM SPAWN İÇ ───────────────────*/
    void SpawnCustom(List<Vector3Int> coords, SpawnMode mode, Transform basketA, Transform basketB, List<AppleKind> kinds)
    {
        if (!ValidatePrefab()) return;
        var cam = GetCam();   if (!cam) return;
        if (coords == null || coords.Count == 0)
        {
            Debug.LogWarning("RowAppleSpawner: SpawnCustom empty list.");
            return;
        }

        ClearRow();
        BuildGrid();

        GetSpawnBasis(cam.transform, out var basisPos, out var right, out var up, out var fwd);

        for (int i = 0; i < coords.Count; i++)
        {
            Vector3Int gi = coords[i];
            int xi = Mathf.Clamp(gi.x, 0, xCount - 1);
            int yi = Mathf.Clamp(gi.y, 0, yCount - 1);
            int zi = Mathf.Clamp(gi.z, 0, zCount - 1);

            Vector3 pos;
            if (layout == Layout.Line)
            {
                pos = basisPos + right * xs[xi] + up * ys[yi] + fwd * zs[zi];
            }
            else // Arc
            {
                float radius = Mathf.Max(0f, zs[zi] + arcCenterBias);
                float ang    = IndexToAngle(xi, xCount, arcAngleDeg);
                Vector3 dir  = Quaternion.AngleAxis(ang, up) * fwd;
                pos = basisPos + up * ys[yi] + dir * radius;
            }

            AppleKind kind = AppleKind.Healthy;
            if (mode == SpawnMode.Sort)
            {
                if (kinds != null && i < kinds.Count) kind = kinds[i];
                else kind = (Random.value < 0.5f) ? AppleKind.Healthy : AppleKind.Rotten;
            }

            SpawnApple(pos, cam.transform, mode, basketA, basketB, kind);
        }
    }

    /*──────────────── ELMA YARATMA ──────────────────────*/
    void SpawnApple(Vector3 worldPos, Transform camT, SpawnMode mode, Transform basketA, Transform basketB, AppleKind kind)
    {
        Quaternion rot = GetSpawnRotation(rotationMode, camT, worldPos);

        var apple = Instantiate(applePrefab, worldPos, rot, transform);

        // spawn FX
        apple.transform.localScale = Vector3.zero;
        apple.transform.DOScale(Vector3.one * appleScale, spawnTweenDur).SetEase(spawnEase);

        // varsayılan materyal (Sort'ta override edilecek)
        var rend = apple.transform.GetChild(0).GetComponent<Renderer>();
        if (rend) rend.material = healthyMat;

        switch (mode)
        {
            case SpawnMode.Reach:
                apple.AddComponent<AppleReachTarget>();
                break;

            case SpawnMode.Grip:
                apple.AddComponent<AppleGripTarget>();      // BasketHoverZone ile çalışır
                break;

            case SpawnMode.Carry:
                var carry = apple.AddComponent<AppleCarryTarget>();
                carry.Init(basketA);
                break;

            case SpawnMode.Sort:
                var sort = apple.AddComponent<AppleSortTarget>();
                sort.Init(basketA, basketB, kind);
                if (kind == AppleKind.Rotten && rottenMat)
                    rend.material = rottenMat;
                break;
        }
    }

    /*──────────────── HELPERLAR ─────────────────────────*/
    bool ValidatePrefab()
    {
        if (!applePrefab)
        {
            Debug.LogError("RowAppleSpawner: applePrefab missing.", this);
            return false;
        }
        if (!healthyMat)
        {
            Debug.LogError("RowAppleSpawner: healthyMat missing.", this);
            return false;
        }
        return true;
    }

    Camera GetCam()
    {
        if (targetCamera) return targetCamera;
        if (Camera.main)  return Camera.main;
        Debug.LogError("RowAppleSpawner: no camera found.", this);
        return null;
    }

    void GetSpawnBasis(Transform camT, out Vector3 pos, out Vector3 right, out Vector3 up, out Vector3 fwd)
    {
        pos   = camT.position + camSpaceYOffset;
        right = camT.right;
        up    = camT.up;
        fwd   = camT.forward;

        // el offset uygulama
        float sign = (activeHand == HandSide.Right) ? 1f : -1f;
        pos += right * handSideOffset * sign;
    }

    static float IndexToAngle(int i, int count, float totalAng)
    {
        if (count <= 1) return 0f;
        float start = -totalAng * 0.5f;
        float step  = totalAng / (count - 1);
        return start + step * i;
    }

    Quaternion GetSpawnRotation(RotationMode mode, Transform camT, Vector3 applePos)
    {
        switch (mode)
        {
            default:
            case RotationMode.PrefabRotation:
                return Quaternion.identity;

            case RotationMode.MatchCamera:
                return camT.rotation;

            case RotationMode.FaceCameraBillboard:
                Vector3 dir = camT.position - applePos;
                if (dir.sqrMagnitude < 1e-6f) dir = -camT.forward;
                return Quaternion.LookRotation(dir.normalized, camT.up);
        }
    }

#if UNITY_EDITOR
    /*──────────────── GİZMO ─────────────────────────────*/
    void OnDrawGizmosSelected()
    {
        var cam = GetCam();
        if (!cam) return;

        BuildGrid();
        GetSpawnBasis(cam.transform, out var basisPos, out var right, out var up, out var fwd);

        Gizmos.color = Color.yellow;
        const float R = 0.01f;

        if (layout == Layout.Line)
        {
            for (int yi = 0; yi < yCount; yi++)
            for (int zi = 0; zi < zCount; zi++)
            for (int xi = 0; xi < xCount; xi++)
            {
                Vector3 p = basisPos + right * xs[xi] + up * ys[yi] + fwd * zs[zi];
                Gizmos.DrawWireSphere(p, R);
            }
        }
        else
        {
            for (int yi = 0; yi < yCount; yi++)
            for (int zi = 0; zi < zCount; zi++)
            {
                float radius = Mathf.Max(0f, zs[zi] + arcCenterBias);
                for (int xi = 0; xi < xCount; xi++)
                {
                    float ang = IndexToAngle(xi, xCount, arcAngleDeg);
                    Vector3 dir = Quaternion.AngleAxis(ang, up) * fwd;
                    Vector3 p = basisPos + up * ys[yi] + dir * radius;
                    Gizmos.DrawWireSphere(p, R);
                }
            }
        }
    }
#endif
}
