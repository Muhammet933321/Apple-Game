using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[Serializable]
public struct GridPosition
{
    public Vector3Int grid;   // logical cell (–3 … +3 on each axis)
    public Vector3    world;  // world-space position (×100)

    public GridPosition(Vector3Int grid, Vector3 world)
    {
        this.grid  = grid;
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

    /* ─────────── Inspector fields ─────────── */

    [Header("Prefab & Grid Settings")]
    public GameObject applePrefab;
    public int   range;
    public float spacing;
    public int startCoundown;
    [Header("Materials & Spawn Odds")]
    public Material healthyMaterial;
    public Material rottenMaterial;
    public Material transparentMaterial;
    [Range(0f, 1f)]
    public float rottenChance = 0.3f;       // 30 % rotten by default
    
    public XROrigin xrOrigin;

    public Vector3 healthyBasketOffset;
    public Vector3 rottenBasketOffset;
    public GameObject healthyBasket;
    public GameObject rottenBasket;
    public GrabEffect grabEffect;
    /* ─────────── Runtime data ─────────── */

    public List<GridPosition> positions = new();   // all legal cells
    public List<GridPosition> calibratedPositions = new();   // all legal cells

    private GameObject  currentApple;
    private Vector3Int  currentGrid;               // grid of the active apple
    private float       spawnTimestamp;            // Time.time when it appeared
    private readonly System.Random rng = new();    // deterministic tests → seed
    public float basketMoveDuration = 0.5f;
    /* ─────────── Analytics — **requested** ─────────── */

    public float      lastPickSeconds { get; private set; } = -1f;
    public Vector3Int lastPickGrid    { get; private set; }
    
    [Header("Arc Settings")]
    [Range(-180f, 180f)]
    public float arcRotation = 0f; // Rotation offset in degrees
    public TextMeshProUGUI basketText;

    /* ─────────── Unity lifecycle ─────────── */
    
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        
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
        //xrOrigin = FindAnyObjectByType<XROrigin>();
        xrOrigin.MoveCameraToWorldLocation(new Vector3(0,1.36f,0f));
        float currentYaw = xrOrigin.Camera.transform.eulerAngles.y;
        xrOrigin.RotateAroundCameraUsingOriginUp(-currentYaw);
        transform.position = Camera.main.transform.position+new Vector3(0.2f,0,0.5f); // Adjust to headset position
    }
    public void OnStartButton()
    {
        AdjustToHeadset();
        //healthyBasket.transform.position = Camera.main.transform.position+new Vector3(0.3f,-0.5f,0.5f);
        //rottenBasket.transform.position = Camera.main.transform.position+new Vector3(-0.3f,-0.5f,0.5f);
        GeneratePositions();
        SpawnAllApples();
        //SpawnRandomApple();

        StartCoroutine(CalibrationCountdown());
    }

    IEnumerator CalibrationCountdown()
    {
        yield return new WaitForSecondsRealtime(10);
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        //AdjustToHeadset();
        SpawnRandomApple();
    }
    public void OnGrabbed()
    {
       Vector3 basePos = Camera.main.transform.position;

        Vector3 healthyTarget = basePos + healthyBasketOffset;
        Vector3 rottenTarget  = basePos + rottenBasketOffset;

        // Start at ground level
        healthyBasket.transform.position = new Vector3(healthyTarget.x, 0f, healthyTarget.z);
        rottenBasket.transform.position  = new Vector3(rottenTarget.x, 0f, rottenTarget.z);

        healthyBasket.SetActive(true);
        rottenBasket.SetActive(true);

        // Animate upward
        healthyBasket.transform.DOMoveY(healthyTarget.y, basketMoveDuration);
        rottenBasket.transform.DOMoveY(rottenTarget.y, basketMoveDuration);
    }

public void OnReleased(Vector3 appleReleasePosition, Apple apple)
{
    Bounds healthyZone = new Bounds(
        healthyBasket.transform.position + Vector3.up * 1f, 
        Vector3.one);
    Bounds rottenZone = new Bounds(
        rottenBasket.transform.position + Vector3.up * 1f, 
        Vector3.one);

    if (apple == null)
    {
        Debug.LogWarning("No current apple found.");
        return;
    }

    bool releasedInHealthyZone = healthyZone.Contains(appleReleasePosition);
    bool releasedInRottenZone  = rottenZone.Contains(appleReleasePosition);

    if (releasedInHealthyZone || releasedInRottenZone)
    {
        Debug.Log("Released");
        bool isCorrectBasket = (releasedInHealthyZone && apple.appleType == AppleType.Healthy)
                               || (releasedInRottenZone  && apple.appleType == AppleType.Rotten);

        // Basket transform reference
        Transform targetBasket = releasedInHealthyZone ? healthyBasket.transform : rottenBasket.transform;

        // Get random local offset inside a small cube (e.g. 0.2 units in each direction)
        Vector3 randomLocalOffset = new Vector3(
            UnityEngine.Random.Range(-0.1f, 0.1f),
            UnityEngine.Random.Range( -0.05f, 0.1f),  // keep it slightly above the base
            UnityEngine.Random.Range(-0.1f, 0.1f)
        );

        // Final target position inside the basket
        Vector3 targetPosition = targetBasket.position + randomLocalOffset;

        // Tween the apple to the target position
        apple.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            apple.Pick(isCorrectBasket);
        });
    }
    else
    {
        Debug.Log("Apple was released outside any basket.");
        apple.Pick(false);
        // Optional: Let the apple fall naturally
    }
}


    /* ─────────── Grid generation ─────────── */

    private void GeneratePositions()
    {
        positions.Clear();

        int layerCount       = 3;
        int horizontalCount  = 8;   // left-right resolution
        int verticalCount    = 4;    // up-down resolution
        float radiusStart    = 0.4f;
        float radiusStep     = 0.2f;
        float horizontalSpan = 90f; // in degrees
        float verticalSpan   = 40f;  // in degrees

        Transform cam = Camera.main.transform;
        Vector3 offset = new Vector3(0.2f, 0f, -0.2f);
        Vector3 arcCenter = cam.position + offset; 

        Vector3 baseForward = Quaternion.Euler(0f, arcRotation, 0f) * cam.forward;
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
                    positions.Add(new GridPosition(Vector3Int.zero, pos));
                }
            }
        }

        Debug.Log($"✅ Spawned {positions.Count} apples in {layerCount} spherical shell layers.");
    }








    /* ─────────── Spawning & picking ─────────── */

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

        // 1 — Choose a random calibrated position
        int index = rng.Next(calibratedPositions.Count);
        GridPosition selected = calibratedPositions[index];
        currentGrid = selected.grid;
        Vector3 spawnPos = selected.world;

        // 2 — Instantiate apple at that position (world-space)
        currentApple = Instantiate(
            applePrefab,
            spawnPos,
            Quaternion.identity,
            transform);

        currentApple.transform.localScale = Vector3.zero;
        currentApple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);

        // 3 — Record timestamp
        spawnTimestamp = Time.time;

        // 4 — Choose apple type & material
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


    private void CorrectBasket(Apple picked)
    {
        basketText.text = "Dogru";
    }
    
    private void WrongBasket(Apple picked)
    {
        basketText.text = "Yanlis";
    }
    
    private void HandleApplePicked(Apple picked)
    {
        Debug.Log("Apple picked!");
        if (picked.gameObject != currentApple) return;

        /* ─── analytics ─── */
        lastPickSeconds = Time.time - spawnTimestamp;
        lastPickGrid    = currentGrid;
        Debug.Log($"Apple picked in {lastPickSeconds:F2}s at {lastPickGrid}");
        
        // todo: don't use find
        FirestoreAppointmentManager mgr = FindAnyObjectByType<FirestoreAppointmentManager>();
        if (mgr != null)
            mgr.SavePickAnalytics(currentGrid, lastPickSeconds);


        /* ─── spawn replacement ─── */
        SpawnRandomApple();
    }
    
    public void SpawnAllApples()
    {
        if (applePrefab == null || healthyMaterial == null || rottenMaterial == null)
        {
            Debug.LogError($"{name}: Prefab or materials not assigned!", this);
            return;
        }

        foreach (var pos in positions)
        {
            GameObject apple = Instantiate(
                applePrefab,
                pos.world,  // Remove transform.position offset
                Quaternion.identity,
                transform);

            apple.transform.localScale = Vector3.zero;
            apple.transform.DOScale(new Vector3(0.04f, 0.04f, 0.04f), 0.5f);

            bool makeRotten = rng.NextDouble() < rottenChance;
            var appleScript = apple.GetComponent<Apple>();
            var renderer = apple.transform.GetChild(0).GetComponent<Renderer>();
            appleScript.position = pos;
            appleScript.isCalibrating = true;
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

}
