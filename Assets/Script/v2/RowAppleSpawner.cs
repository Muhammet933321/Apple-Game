using UnityEngine;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Kamera uzayına dayalı elma spawner.
/// Layout: Line (eski sıra) veya ArcHorizontal (ayarlanabilir yay).
/// levelIndex (0-4) -> Y ve Z aralıklarından konum seçer.
/// Aktif el (sağ/sol) seçimine göre tüm dağılım kamera.right yönünde ±kaydırılır.
/// </summary>
public class RowAppleSpawner : MonoBehaviour
{
    /*──────────────── Prefabs & Materials ────────────────*/
    [Header("Prefabs & Materials")]
    [SerializeField] GameObject applePrefab;
    [SerializeField] Material   healthyMat;
    [SerializeField] Material   rottenMat; // Sort modunda

    /*──────────────── Camera Anchor ──────────────────────*/
    [Header("Camera Anchor")]
    [Tooltip("Elmaların spawn yönünü belirleyecek kamera. Boşsa Camera.main kullanılır.")]
    [SerializeField] Camera targetCamera;

    [Tooltip("Kamera orijinine eklenecek dünya uzayında offset. Örn: (0,-0.2,0) göz -> göğüs.")]
    [SerializeField] Vector3 camSpaceYOffset = Vector3.zero;

    public enum RotationMode
    {
        PrefabRotation,
        MatchCamera,
        FaceCameraBillboard
    }

    [SerializeField] RotationMode rotationMode = RotationMode.MatchCamera;

    /*──────────────── Hand Selection ─────────────────────*/
    public enum HandSide { Right, Left }

    [Header("Hand Selection")]
    [SerializeField] HandSide activeHand = HandSide.Right;

    [Tooltip("Satır / yay merkezinin kameradan yana kaydırma mesafesi (metre). Sağ el -> +X, Sol el -> -X.")]
    [SerializeField] float handSideOffset = 0.15f;

    /// <summary>Runtime'da etkin eli değiştir.</summary>
    public void SetActiveHand(HandSide hand) => activeHand = hand;

    /// <summary>Sağ/Sol arasında hızlı geçiş.</summary>
    public void ToggleHand() =>
        activeHand = (activeHand == HandSide.Right) ? HandSide.Left : HandSide.Right;

    /*──────────────── Layout Seçimi ─────────────────────*/
    public enum Layout
    {
        Line,          // xMin..xMax lineer sıra
        ArcHorizontal  // cam.up etrafında yatay yay
    }

    [Header("Layout")]
    [SerializeField] Layout layout = Layout.Line;

    [Tooltip("ArcHorizontal modunda toplam yay açısı (derece). Örn. 60 => -30..+30.")]
    [SerializeField] float arcAngleDeg = 60f;

    [Tooltip("ArcHorizontal modunda seçilen seviyenin Z mesafesine eklenecek ekstra ileri-geri offset (metre).")]
    [SerializeField] float arcCenterBias = 0f;

    /*──────────────── Grid Ranges (relative to camera axes) ─────*/
    [Header("Grid Ranges (relative to camera axes)")]
    [SerializeField] float xMin = -0.40f;
    [SerializeField] float xMax =  0.40f;
    [SerializeField] float yMin = -0.10f;
    [SerializeField] float yMax =  0.40f;
    [SerializeField] float zMin =  0.60f;
    [SerializeField] float zMax =  1.80f;

    /*──────────────── Grid Counts ───────────────────────*/
    [Header("Grid Counts")]
    [SerializeField, Min(1)] int xCount = 8;  // sabit
    [SerializeField, Min(1)] int yCount = 5;  // sabit
    [SerializeField, Min(1)] int zCount = 5;  // sabit

    /*──────────────── Spawn FX ──────────────────────────*/
    [Header("Spawn FX")]
    [SerializeField] float appleScale    = 0.04f;
    [SerializeField] float spawnTweenDur = 0.30f;
    [SerializeField] Ease  spawnEase     = Ease.OutBack;

    /*──────────────── Dahili ────────────────────────────*/
    enum SpawnMode { Reach, Grip, Carry, Sort }
    float[] _xs, _ys, _zs;

    void Awake()      => BuildGrid();
    void OnValidate() => BuildGrid();

    /*--------------------------------------------------------------
     * Grid hesaplama (lineer eşit aralık)
     *--------------------------------------------------------------*/
    void BuildGrid()
    {
        _xs = BuildAxis(xMin, xMax, xCount);
        _ys = BuildAxis(yMin, yMax, yCount);
        _zs = BuildAxis(zMin, zMax, zCount);
    }

    static float[] BuildAxis(float min, float max, int count)
    {
        if (count <= 1) return new[] { (min + max) * 0.5f };
        float[] arr = new float[count];
        float step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++) arr[i] = min + step * i;
        return arr;
    }

    /*--------------------------------------------------------------
     * Temizlik
     *--------------------------------------------------------------*/
    public void ClearRow()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    /*--------------------------------------------------------------
     * Public API - Mod başına convenience fonksiyonlar
     *--------------------------------------------------------------*/
    public void SpawnReachLevel(int levelIndex)
        => SpawnLevel(levelIndex, SpawnMode.Reach, null, null);

    public void SpawnGripLevel(int levelIndex, Transform basket)
        => SpawnLevel(levelIndex, SpawnMode.Grip, basket, null);

    public void SpawnCarryLevel(int levelIndex, Transform basket)
        => SpawnLevel(levelIndex, SpawnMode.Carry, basket, null);

    public void SpawnSortLevel(int levelIndex, Transform healthyBasket, Transform rottenBasket)
        => SpawnLevel(levelIndex, SpawnMode.Sort, healthyBasket, rottenBasket);

    /*--------------------------------------------------------------
     * Ana spawn metodu
     *--------------------------------------------------------------*/
    void SpawnLevel(int levelIndex, SpawnMode mode, Transform basketA, Transform basketB)
    {
        if (!applePrefab || !healthyMat)
        {
            Debug.LogError("RowAppleSpawner: Prefab / materials missing.", this);
            return;
        }

        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam)
        {
            Debug.LogError("RowAppleSpawner: No camera available to anchor grid.", this);
            return;
        }

        ClearRow();
        BuildGrid(); // inspector değişikliklerini yakala

        // Level 0..(min(yCount,zCount)-1)
        int li = Mathf.Clamp(levelIndex, 0, Mathf.Min(yCount, zCount) - 1);
        float y = _ys[li];
        float z = _zs[li];

        // Kamera baz
        Transform ct = cam.transform;
        Vector3 camPos = ct.position + camSpaceYOffset;

        // El-ofseti uygula (sağ/sol)
        float sign = (activeHand == HandSide.Right) ? 1f : -1f;
        camPos += ct.right * handSideOffset * sign;

        Vector3 camRight   = ct.right;
        Vector3 camUp      = ct.up;
        Vector3 camForward = ct.forward;

        if (layout == Layout.Line)
        {
            SpawnLine(mode, basketA, basketB, li, y, z, camPos, camRight, camUp, camForward, ct);
        }
        else // ArcHorizontal
        {
            SpawnArcHorizontal(mode, basketA, basketB, li, y, z, camPos, camRight, camUp, camForward, ct);
        }
    }

    /*--------------------------------------------------------------
     * LINE yerleşimi
     *--------------------------------------------------------------*/
    void SpawnLine(SpawnMode mode,
                   Transform basketA,
                   Transform basketB,
                   int li,
                   float y, float z,
                   Vector3 camPos,
                   Vector3 camRight,
                   Vector3 camUp,
                   Vector3 camForward,
                   Transform camT)
    {
        for (int xi = 0; xi < xCount; xi++)
        {
            float x = _xs[xi];
            Vector3 worldPos = camPos + camRight * x + camUp * y + camForward * z;
            SpawnApple(worldPos, camPos, camT, mode, basketA, basketB);
        }
    }

    /*--------------------------------------------------------------
     * ARC yerleşimi (yatay)
     *--------------------------------------------------------------*/
    void SpawnArcHorizontal(SpawnMode mode,
                            Transform basketA,
                            Transform basketB,
                            int li,
                            float y, float z,
                            Vector3 camPos,
                            Vector3 camRight,
                            Vector3 camUp,
                            Vector3 camForward,
                            Transform camT)
    {
        float radius = Mathf.Max(0.0f, z + arcCenterBias);

        float totalAng = arcAngleDeg;
        if (xCount < 2) totalAng = 0f;
        float stepAng = (xCount > 1) ? (totalAng / (xCount - 1)) : 0f;
        float startAng = -totalAng * 0.5f;

        for (int i = 0; i < xCount; i++)
        {
            float ang = startAng + stepAng * i;
            Quaternion yaw = Quaternion.AngleAxis(ang, camUp);
            Vector3 dir = yaw * camForward;

            Vector3 worldPos = camPos + camUp * y + dir * radius;
            SpawnApple(worldPos, camPos, camT, mode, basketA, basketB);
        }
    }

    /*--------------------------------------------------------------
     * Ortak elma oluşturma
     *--------------------------------------------------------------*/
    void SpawnApple(Vector3 worldPos,
                    Vector3 camPos,
                    Transform camT,
                    SpawnMode mode,
                    Transform basketA,
                    Transform basketB)
    {
        Quaternion worldRot = GetSpawnRotation(rotationMode, camT, worldPos, camPos);

        var apple = Instantiate(applePrefab, worldPos, worldRot, transform);

        // açılış animasyonu
        apple.transform.localScale = Vector3.zero;
        apple.transform
             .DOScale(Vector3.one * appleScale, spawnTweenDur)
             .SetEase(spawnEase);

        // varsayılan materyal
        var rend = apple.transform.GetChild(0).GetComponent<Renderer>();
        rend.material = healthyMat;

        switch (mode)
        {
            case SpawnMode.Reach:
                apple.AddComponent<AppleReachTarget>();
                break;

            case SpawnMode.Grip:
                {
                    var grip = apple.AddComponent<AppleGripTarget>();
                    // grip.Init(basketA); // hazır olduğunda aç
                }
                break;

            case SpawnMode.Carry:
                {
                    var carry = apple.AddComponent<AppleCarryTarget>();
                    carry.Init(basketA);
                }
                break;

            case SpawnMode.Sort:
                {
                    AppleKind kind = (Random.value < 0.5f) ? AppleKind.Healthy : AppleKind.Rotten;
                    var sort = apple.AddComponent<AppleSortTarget>();
                    sort.Init(basketA, basketB, kind);
                    if (kind == AppleKind.Rotten && rottenMat != null)
                        rend.material = rottenMat;
                }
                break;
        }
    }

    Quaternion GetSpawnRotation(RotationMode mode, Transform camT, Vector3 applePos, Vector3 camPos)
    {
        switch (mode)
        {
            default:
            case RotationMode.PrefabRotation:
                return Quaternion.identity; // Prefab'ın kendi rotasyonu
            case RotationMode.MatchCamera:
                return camT.rotation;
            case RotationMode.FaceCameraBillboard:
                Vector3 dirToCam = (camPos - applePos).sqrMagnitude < 1e-6f
                    ? -camT.forward
                    : (camPos - applePos).normalized;
                return Quaternion.LookRotation(dirToCam, camT.up);
        }
    }

#if UNITY_EDITOR
    /*--------------------------------------------------------------
     * Editörde debug çizimi
     *--------------------------------------------------------------*/
    void OnDrawGizmosSelected()
    {
        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam) return;

        BuildGrid();

        Transform ct = cam.transform;
        Vector3 camPos = ct.position + camSpaceYOffset;

        // Hand offset de editörde gösterilsin:
        float sign = (activeHand == HandSide.Right) ? 1f : -1f;
        camPos += ct.right * handSideOffset * sign;

        Gizmos.color = Color.yellow;
        const float gizRadius = 0.01f;

        if (layout == Layout.Line)
        {
            Vector3 camRight   = ct.right;
            Vector3 camUp      = ct.up;
            Vector3 camForward = ct.forward;

            for (int yi = 0; yi < yCount; yi++)
            {
                for (int zi = 0; zi < zCount; zi++)
                {
                    for (int xi = 0; xi < xCount; xi++)
                    {
                        Vector3 world =
                            camPos +
                            camRight   * _xs[xi] +
                            camUp      * _ys[yi] +
                            camForward * _zs[zi];
                        Gizmos.DrawWireSphere(world, gizRadius);
                    }
                }
            }
        }
        else // ArcHorizontal
        {
            for (int yi = 0; yi < yCount; yi++)
            {
                float y = _ys[yi];
                for (int zi = 0; zi < zCount; zi++)
                {
                    float radius = Mathf.Max(0.0f, _zs[zi] + arcCenterBias);
                    float totalAng = arcAngleDeg;
                    float stepAng = (xCount > 1) ? (totalAng / (xCount - 1)) : 0f;
                    float startAng = -totalAng * 0.5f;

                    for (int xi = 0; xi < xCount; xi++)
                    {
                        float ang = startAng + stepAng * xi;
                        Quaternion yaw = Quaternion.AngleAxis(ang, ct.up);
                        Vector3 dir = yaw * ct.forward;
                        Vector3 world = camPos + ct.up * y + dir * radius;
                        Gizmos.DrawWireSphere(world, gizRadius);
                    }
                }
            }
        }
    }
#endif
}
