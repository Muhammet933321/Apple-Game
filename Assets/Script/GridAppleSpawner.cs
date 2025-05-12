using System;
using System.Collections.Generic;
using UnityEngine;

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
    [Header("Prefab & Grid Settings")]
    public GameObject applePrefab;
    public int   range   = 3;     // → cells from –3 to +3
    public float spacing = 100f;  // → –300 … +300 world units

    public List<GridPosition> positions = new();

    private GameObject currentApple;          // the one that’s currently in the scene
    private readonly System.Random rng = new();   // for deterministic unit tests, seed here

    /* ─────────── Unity lifecycle ─────────── */

    private void OnValidate() => GeneratePositions();
    private void Awake()
    {
        GeneratePositions();
        Apple.Picked += HandleApplePicked;    // subscribe to the event
    }

    private void OnDestroy() =>
        Apple.Picked -= HandleApplePicked;    // clean up

    private void Start() => SpawnRandomApple();

    /* ─────────── Core logic ─────────── */

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

    private void SpawnRandomApple()
    {
        if (applePrefab == null) { Debug.LogError("Apple prefab missing!", this); return; }

        int index = rng.Next(positions.Count);          // pick a random cell
        currentApple = Instantiate(applePrefab,
                                   transform.position+positions[index].world,
                                   Quaternion.identity,
                                   transform);
    }

    private void HandleApplePicked(Apple picked)
    {
        if (picked.gameObject == currentApple)          // make sure it’s our apple
            SpawnRandomApple();                         // replace it with a new one
    }
}
