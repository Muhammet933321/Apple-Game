using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Unity.XR.CoreUtils;

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
    /* ─────────── Inspector fields ─────────── */

    [Header("Prefab & Grid Settings")]
    public GameObject applePrefab;
    public int   range;
    public float spacing;
    public int startCoundown;
    [Header("Materials & Spawn Odds")]
    public Material healthyMaterial;
    public Material rottenMaterial;
    [Range(0f, 1f)]
    public float rottenChance = 0.3f;       // 30 % rotten by default
    
    [SerializeField] private XROrigin xrOrigin;

    public Vector3 healthyBasketOffset;
    public Vector3 rottenBasketOffset;
    public GameObject healthyBasket;
    public GameObject rottenBasket;
    public GrabEffect grabEffect;
    /* ─────────── Runtime data ─────────── */

    public List<GridPosition> positions = new();   // all legal cells

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


    /* ─────────── Unity lifecycle ─────────── */
    
    private void Awake()
    {
        Apple.Picked += HandleApplePicked;
    }
    private void OnDestroy()   => Apple.Picked -= HandleApplePicked;

    public void OnStartButton()
    {
        xrOrigin.MoveCameraToWorldLocation(new Vector3(0,1.36f,0f));
        float currentYaw = xrOrigin.Camera.transform.eulerAngles.y;
        xrOrigin.RotateAroundCameraUsingOriginUp(-currentYaw);
        transform.position = Camera.main.transform.position+new Vector3(0.2f,0,0.5f); // Adjust to headset position
        //healthyBasket.transform.position = Camera.main.transform.position+new Vector3(0.3f,-0.5f,0.5f);
        //rottenBasket.transform.position = Camera.main.transform.position+new Vector3(-0.3f,-0.5f,0.5f);
        GeneratePositions();
        SpawnAllApples();
        //SpawnRandomApple();
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

    public void OnReleased()
    {
        // Animate downward to y = 0
        healthyBasket.transform.DOMoveY(0f, basketMoveDuration).OnComplete(() =>
        {
            healthyBasket.SetActive(false);
        });

        rottenBasket.transform.DOMoveY(0f, basketMoveDuration).OnComplete(() =>
        {
            rottenBasket.SetActive(false);
        });
    }

    /* ─────────── Grid generation ─────────── */

    private void GeneratePositions()
    {
        positions.Clear();

        int layerCount       = 3;
        int horizontalCount  = 8;   // left-right resolution
        int verticalCount    = 4;    // up-down resolution
        float radiusStart    = 0.2f;
        float radiusStep     = 0.2f;
        float horizontalSpan = 120f; // in degrees
        float verticalSpan   = 60f;  // in degrees

        Transform cam = Camera.main.transform;
        Vector3 offset = new Vector3(0.2f, -0.2f, 0.2f);
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

        /* 1 — random cell */
        int index = rng.Next(positions.Count);
        currentGrid = positions[index].grid;
        Vector3 spawnPos = positions[index].world;

        /* 2 — instantiate */
        currentApple = Instantiate(
            applePrefab,
            transform.position + spawnPos,
            Quaternion.identity,
            transform);
        
        currentApple.transform.localScale = Vector3.zero;
        currentApple.transform.DOScale(new Vector3(0.05f, 0.05f, 0.05f), 0.5f);

        /* 3 — stamp birth-time for analytics */
        spawnTimestamp = Time.time;

        /* 4 — choose type & material */
        bool makeRotten = rng.NextDouble() < rottenChance;
        var apple       = currentApple.GetComponent<Apple>();
        var renderer    = currentApple.transform.GetChild(0).GetComponent<Renderer>();

        if (makeRotten)
        {
            apple.appleType   = AppleType.Rotten;
            renderer.material = rottenMaterial;
        }
        else
        {
            apple.appleType   = AppleType.Healthy;
            renderer.material = healthyMaterial;
        }
    }

    private void HandleApplePicked(Apple picked)
    {
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
            apple.transform.DOScale(new Vector3(0.02f, 0.02f, 0.02f), 0.5f);

            bool makeRotten = rng.NextDouble() < rottenChance;
            var appleScript = apple.GetComponent<Apple>();
            var renderer = apple.transform.GetChild(0).GetComponent<Renderer>();

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

}
