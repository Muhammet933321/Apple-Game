using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DG.Tweening;

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
    public int   range   = 3;     // → cells from –3 to +3
    public float spacing = 100f;  // → –300 … +300 world units
    public int startCoundown;
    [Header("Materials & Spawn Odds")]
    public Material healthyMaterial;
    public Material rottenMaterial;
    [Range(0f, 1f)]
    public float rottenChance = 0.3f;       // 30 % rotten by default

    public GameObject healthyBasket;
    public GameObject rottenBasket;
    /* ─────────── Runtime data ─────────── */

    public List<GridPosition> positions = new();   // all legal cells

    private GameObject  currentApple;
    private Vector3Int  currentGrid;               // grid of the active apple
    private float       spawnTimestamp;            // Time.time when it appeared
    private readonly System.Random rng = new();    // deterministic tests → seed

    /* ─────────── Analytics — **requested** ─────────── */

    public float      lastPickSeconds { get; private set; } = -1f;
    public Vector3Int lastPickGrid    { get; private set; }

    /* ─────────── Unity lifecycle ─────────── */

    private void OnValidate()  => GeneratePositions();
    private void Awake()
    {
        GeneratePositions();
        Apple.Picked += HandleApplePicked;
    }
    private void OnDestroy()   => Apple.Picked -= HandleApplePicked;

    private void Start()
    {
        //StartCoroutine(Countdown());
        //StartCoroutine(AdjustHeadset());
    }

    public void OnStartButton()
    {
        Camera.main.transform.position = new Vector3(0, 0, 0);
        transform.position = Camera.main.transform.position+new Vector3(0,1.36f,0.5f); // Adjust to headset position
        //healthyBasket.transform.position = Camera.main.transform.position+new Vector3(0.3f,-0.5f,0.5f);
        //rottenBasket.transform.position = Camera.main.transform.position+new Vector3(-0.3f,-0.5f,0.5f);
        SpawnRandomApple();
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(startCoundown);
        SpawnRandomApple();
    }
    
    IEnumerator AdjustHeadset()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);
            transform.position = Camera.main.transform.position+new Vector3(0,0,0.5f); // Adjust to headset position 
        }
    }

    /* ─────────── Grid generation ─────────── */

    private void GeneratePositions()
    {
        positions.Clear();

        for (int x = -range; x <= range; x++)
        for (int y = -range; y <= range; y++)
        for (int z = -range; z <= range; z++)
        {
            Vector3Int grid  = new(x, y, z);
            Vector3    world = new(x * spacing, y * spacing, z * spacing);
            positions.Add(new GridPosition(grid, world));
        }
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
        currentApple.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f);

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
}
