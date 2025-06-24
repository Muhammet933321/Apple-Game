using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[Serializable]
public struct GridPosition
{
    public Vector3Int grid;
    public Vector3 world;

    public GridPosition(Vector3Int grid, Vector3 world)
    {
        this.grid = grid;
        this.world = world;
    }
}

public class GridAppleSpawner : MonoBehaviour
{
    [Header("Hand Joints")]
    public List<Transform> leftHandTips = new();
    public List<Transform> rightHandTips = new();
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;

    [Header("Prefab & Grid Settings")]
    public GameObject applePrefab;
    public int measureTimeSeconds = 10;

    [Header("Materials & Spawn Odds")]
    public Material healthyMaterial;
    public Material rottenMaterial;
    public Material transparentMaterial;
    [Range(0f, 1f)]
    public float rottenChance = 0.3f;

    public XROrigin xrOrigin;
    public Vector3 healthyBasketOffset;
    public Vector3 rottenBasketOffset;
    public GameObject healthyBasket;
    public GameObject rottenBasket;
    public GrabEffect grabEffect;

    [Header("Arc Settings")]
    [Range(-180f, 180f)]
    public float arcRotation = 0f;
    public Text basketText;

    public bool isMeasureMode;

    public float basketMoveDuration = 0.5f;

    public List<GridPosition> positions = new();
    public List<GridPosition> calibratedPositions = new();

    private GameObject currentApple;
    private Vector3Int currentGrid;
    private float spawnTimestamp;
    private readonly System.Random rng = new();

    public float lastPickSeconds { get; private set; } = -1f;
    public Vector3Int lastPickGrid { get; private set; }

    private List<Vector3Int> pickedGridsInMeasureMode = new();
    private Coroutine measureTimerCoroutine;

    private void Awake()
    {
        Apple.PickedCorrectBasket += HandleApplePicked;
        Apple.PickedWrongBasket += HandleApplePicked;
        Apple.PickedCorrectBasket += CorrectBasket;
        Apple.PickedWrongBasket += WrongBasket;
    }

    private void OnDestroy()
    {
        Apple.PickedCorrectBasket -= HandleApplePicked;
        Apple.PickedWrongBasket -= HandleApplePicked;
        Apple.PickedCorrectBasket -= CorrectBasket;
        Apple.PickedWrongBasket -= WrongBasket;
    }

    private void AdjustToHeadset()
    {
        xrOrigin.MoveCameraToWorldLocation(new Vector3(0, 1.36f, 0f));
        float camYaw = xrOrigin.Camera.transform.eulerAngles.y;
        xrOrigin.RotateAroundCameraPosition(Vector3.up, -camYaw);

        Vector3 basePos = Camera.main.transform.position;
        transform.position = basePos + new Vector3(0.2f, -0.5f, 0.5f);

        Vector3 healthyTarget = basePos + healthyBasketOffset;
        Vector3 rottenTarget = basePos + rottenBasketOffset;
        healthyBasket.transform.position = healthyTarget;
        rottenBasket.transform.position = rottenTarget;
    }

    public void OnStartButton()
    {
        GameModeManager.Instance.StartMeasurement();
        pickedGridsInMeasureMode.Clear();
        calibratedPositions.Clear();
        positions.Clear();

        AdjustToHeadset();
        GeneratePositions();
        SpawnAllApples();

        if (isMeasureMode)
        {
            if (measureTimerCoroutine != null)
                StopCoroutine(measureTimerCoroutine);
            measureTimerCoroutine = StartCoroutine(MeasureCountdown());
        }
        else
        {
            StartCoroutine(CalibrationCountdown());
        }
    }

    IEnumerator CalibrationCountdown()
    {
        yield return new WaitForSecondsRealtime(10);
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        SpawnRandomApple();
    }

    IEnumerator MeasureCountdown()
    {
        Debug.Log("🟢 Measure mode started.");
        yield return new WaitForSecondsRealtime(measureTimeSeconds);
        Debug.Log("🔴 Measure mode ended. Filtering future apples.");

        // Save picked positions
        HashSet<Vector3Int> exclude = new(pickedGridsInMeasureMode);
        Debug.Log($"📦 Excluded grid positions ({exclude.Count}):");

        foreach (var grid in pickedGridsInMeasureMode)
        {
            Debug.Log($"❌ Exclude Grid: {grid}");
        }
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        calibratedPositions = positions
            .Where(pos => !exclude.Contains(pos.grid))
            .ToList();

        //ölçüm bitince, sonuç yazılmamış grid’leri Unreachable olarak işaretle
        foreach (var pos in positions)
        {
            if (!GameModeManager.Instance.data.Results.ContainsKey(pos.grid))
                GameModeManager.Instance.data.Record(pos.grid, AppleOutcome.Unreachable);
        }
        
        //SpawnRemainingApplesAfterMeasurement();
        
        GameModeManager.Instance.StartWrongBasket();
    }
    private void SpawnRemainingApplesAfterMeasurement()
    {
        foreach (var pos in calibratedPositions)
        {
            GameObject apple = Instantiate(applePrefab, pos.world, Quaternion.identity, transform);
            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);

            bool makeRotten = rng.NextDouble() < rottenChance;
            var appleScript = apple.GetComponent<Apple>();
            var renderer = apple.transform.GetChild(0).GetComponent<Renderer>();

            appleScript.position = pos;
            appleScript.isCalibrating = false;

            if (makeRotten)
            {
                appleScript.appleType = AppleType.Rotten;
                renderer.material = rottenMaterial;
            }
            else
            {
                appleScript.appleType = AppleType.Healthy;
                renderer.material = healthyMaterial;
            }
        }

        Debug.Log($"🍎 Spawned {calibratedPositions.Count} apples after measurement.");
    }


    public void OnReleased(Vector3 appleReleasePosition, GridPosition grid, Apple apple)
    {
        if (apple == null)
        {
            Debug.LogWarning("OnReleased - apple is null!");
            return;
        }

        // 1) Sepet bölgeleri
        Bounds healthyZone = new(healthyBasket.transform.position, Vector3.one * 0.5f);
        Bounds rottenZone  = new(rottenBasket.transform.position,  Vector3.one * 0.5f);

        bool inHealthy = healthyZone.Contains(appleReleasePosition);
        bool inRotten  = rottenZone.Contains(appleReleasePosition);

        AppleOutcome outcome;

        // 2) Sepete mi girdi?
        if (inHealthy || inRotten)
        {
            bool isCorrect =
                (inHealthy && apple.appleType == AppleType.Healthy) ||
                (inRotten  && apple.appleType == AppleType.Rotten);

            outcome = isCorrect ? AppleOutcome.Success : AppleOutcome.WrongBasket;

            // Hedef sepet ve küçük rastgele offset
            Transform targetBasket = inHealthy ? healthyBasket.transform : rottenBasket.transform;
            Vector3 offset = new(
                UnityEngine.Random.Range(-0.05f, 0.05f),
                UnityEngine.Random.Range(-0.05f, 0f),
                UnityEngine.Random.Range(-0.05f, 0.05f)
            );

            apple.transform
                 .DOMove(targetBasket.position + offset, basketMoveDuration)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() => apple.Pick(isCorrect));
        }
        else             // 3) Sepete girmedi → düştü
        {
            Debug.Log("Apple dropped outside baskets.");
            outcome = AppleOutcome.Drop;

            apple.Pick(false);          // başarısız işaretle
            Destroy(apple.gameObject);  // yerde kalmasını istemiyorsanız
        }

        // 4) Ölçüm modundaysa sonucu kaydet
        if (isMeasureMode && GameModeManager.Instance != null)
        {
            GameModeManager.Instance.data.Record(grid.grid, outcome);

            // (İsterseniz) eski listenizi hâlâ kullanıyorsanız:
            if (!pickedGridsInMeasureMode.Contains(grid.grid))
                pickedGridsInMeasureMode.Add(grid.grid);
        }
        
        
    }


    private void GeneratePositions()
    {
        positions.Clear(); // Clear before filling
        HashSet<Vector3Int> uniqueGrids = new();

        int layerCount = 3;
        int horizontalCount = 8;
        int verticalCount = 4;
        float radiusStart = 0.4f;
        float radiusStep = 0.2f;
        float horizontalSpan = 90f;
        float verticalSpan = 40f;

        Transform cam = Camera.main.transform;
        Vector3 offset = new(0.2f, 0f, -0.2f);
        Vector3 arcCenter = cam.position + offset;

        Vector3 baseForward = Quaternion.Euler(0f, arcRotation, 0f) * Vector3.forward;
        Vector3 baseRight = Quaternion.AngleAxis(90f, Vector3.up) * baseForward;

        for (int layer = 0; layer < layerCount; layer++)
        {
            float radius = radiusStart + layer * radiusStep;

            for (int y = 0; y < verticalCount; y++)
            {
                float vStep = verticalSpan / (verticalCount - 1);
                float vAngle = -verticalSpan / 2f + y * vStep;

                for (int x = 0; x < horizontalCount; x++)
                {
                    float hStep = horizontalSpan / (horizontalCount - 1);
                    float hAngle = -horizontalSpan / 2f + x * hStep;

                    Quaternion rotH = Quaternion.AngleAxis(hAngle, Vector3.up);
                    Quaternion rotV = Quaternion.AngleAxis(vAngle, baseRight);

                    Vector3 direction = rotV * rotH * baseForward;
                    Vector3 pos = arcCenter + direction.normalized * radius;

                    Vector3Int gridKey = new(x, y, layer);

                    if (!uniqueGrids.Contains(gridKey))
                    {
                        uniqueGrids.Add(gridKey);
                        positions.Add(new GridPosition(gridKey, pos));
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Duplicate grid key detected: {gridKey}");
                    }
                }
            }
        }

        Debug.Log($"✅ Spawned {positions.Count} apples in {layerCount} spherical shell layers.");
    }


    private void SpawnRandomApple()
    {
        if (applePrefab == null || healthyMaterial == null || rottenMaterial == null)
        {
            Debug.LogError($"{name}: Prefab or materials not assigned!", this);
            return;
        }

        if (calibratedPositions.Count == 0)
        {
            Debug.LogWarning("No calibrated positions available to spawn apple.");
            return;
        }

        int index = rng.Next(calibratedPositions.Count);
        GridPosition selected = calibratedPositions[index];
        currentGrid = selected.grid;
        Vector3 spawnPos = selected.world;

        currentApple = Instantiate(applePrefab, spawnPos, Quaternion.identity, transform);
        currentApple.transform.localScale = Vector3.zero;
        currentApple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);
        spawnTimestamp = Time.time;

        bool makeRotten = rng.NextDouble() < rottenChance;
        Apple apple = currentApple.GetComponent<Apple>();
        Renderer renderer = currentApple.transform.GetChild(0).GetComponent<Renderer>();

        apple.position = selected;
        apple.isCalibrating = false;

        if (makeRotten)
        {
            apple.appleType = AppleType.Rotten;
            renderer.material = rottenMaterial;
        }
        else
        {
            apple.appleType = AppleType.Healthy;
            renderer.material = healthyMaterial;
        }
    }

    private void HandleApplePicked(Apple picked)
    {
        Debug.Log("Apple picked!");

        if (picked.gameObject != currentApple) return;

        lastPickSeconds = Time.time - spawnTimestamp;
        lastPickGrid = currentGrid;
        
        /*FirestoreAppointmentManager mgr = FindAnyObjectByType<FirestoreAppointmentManager>();
        if (mgr != null)
            mgr.SavePickAnalytics(currentGrid, lastPickSeconds);
        */
        //SpawnRandomApple();
    }

    private void CorrectBasket(Apple picked) => basketText.text = "Dogru";
    private void WrongBasket(Apple picked) => basketText.text = "Yanlis";

    public void SpawnAllApples()
    {
        if (applePrefab == null || healthyMaterial == null || rottenMaterial == null)
        {
            Debug.LogError($"{name}: Prefab or materials not assigned!", this);
            return;
        }

        foreach (var pos in positions)
        {
            GameObject apple = Instantiate(applePrefab, pos.world, Quaternion.identity, transform);
            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);

            bool makeRotten = rng.NextDouble() < rottenChance;
            var appleScript = apple.GetComponent<Apple>();
            var renderer = apple.transform.GetChild(0).GetComponent<Renderer>();

            appleScript.position = pos;
            appleScript.isCalibrating = !isMeasureMode;

            if (makeRotten)
            {
                appleScript.appleType = AppleType.Rotten;
                renderer.material = rottenMaterial;
            }
            else
            {
                appleScript.appleType = AppleType.Healthy;
                renderer.material = healthyMaterial;
            }
        }

        Debug.Log($"Spawned {positions.Count} apples in arc.");
    }

    public Material OnCalibrationTouched(GridPosition position)
    {
        calibratedPositions.Add(position);
        return transparentMaterial;
    }
    
    // GridAppleSpawner.cs içine:
    public void SpawnCustomApples(Vector3Int[] grids)
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        foreach (var grid in grids)
        {
            var pos = positions.Find(p => p.grid == grid);
            if (pos.grid == Vector3Int.zero) continue;   // eşleşme yoksa atla

            GameObject apple = Instantiate(applePrefab, pos.world, Quaternion.identity, transform);
            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);

            var appleScript = apple.GetComponent<Apple>();
            appleScript.position = pos;
            appleScript.isCalibrating = false;

            // materyal vs. seçim (ölçümdekine benzer)
            bool makeRotten = rng.NextDouble() < rottenChance;
            var renderer = apple.transform.GetChild(0).GetComponent<Renderer>();
            appleScript.appleType = makeRotten ? AppleType.Rotten : AppleType.Healthy;
            renderer.material = makeRotten ? rottenMaterial : healthyMaterial;
        }
    }

}
