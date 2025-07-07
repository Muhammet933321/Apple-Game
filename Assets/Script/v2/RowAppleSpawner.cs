using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;                // ★ DOTween animation utilities

/// Spawns a single arc-shaped row of apples in front of the headset and
/// keeps track of the live apples. When an apple is collected it notifies
/// ReachActivityManager and destroys itself.
public class RowAppleSpawner : MonoBehaviour
{
    // ─────────────────────────── Inspector ────────────────────────────
    [Header("Prefabs & FX")]
    [SerializeField] private GameObject applePrefab;   // root prefab (mesh under child 0)
    [SerializeField] private Material   healthyMat;    // simple green / red …
    [SerializeField] private GrabEffect grabEffect;    // optional VFX/SFX on touch

    // ─────────────────────────── Runtime ──────────────────────────────
    private readonly List<GameObject> liveApples = new();   // current row apples

    /*──────────────────────────────────────────────────────────────────*/
    /// Spawn an arc row relative to the headset
    ///  count       : number of apples
    ///  height (m)  : offset from HMD in Y (+ up / - down)
    ///  distance(m) : forward distance from HMD
    ///  arcSpanDeg  : total arc angle (e.g., 60° → -30° … +30°)
    /*──────────────────────────────────────────────────────────────────*/
    public void SpawnRow(int count, float height, float distance, float arcSpanDeg)
    {
        ClearRow();

        if (applePrefab == null || healthyMat == null)
        {
            Debug.LogError("RowAppleSpawner: Prefab or material not assigned.", this);
            return;
        }

        Transform cam       = Camera.main.transform;
        Vector3   camFwd    = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3   camPos    = cam.position;

        float stepDeg  = (count == 1) ? 0f : arcSpanDeg / (count - 1);
        float startDeg = -arcSpanDeg * 0.5f;

        for (int i = 0; i < count; i++)
        {
            /* 1️⃣  Arc position */
            float      angle = startDeg + i * stepDeg;
            Quaternion rot   = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3    offset = rot * (camFwd * distance);          // rotate “forward” by Y angle
            Vector3    pos    = camPos + Vector3.up * height + offset;

            /* 2️⃣  Instantiate apple */
            GameObject apple = Instantiate(applePrefab, pos, Quaternion.identity, transform);

            // Pop-in animation
            apple.transform.localScale = Vector3.zero;
            apple.transform
                 .DOScale(Vector3.one * 0.04f, 0.3f)
                 .SetEase(Ease.OutBack);

            // Basic look
            Renderer rend = apple.transform.GetChild(0).GetComponent<Renderer>();
            rend.material = healthyMat;

            /* 3️⃣  Touch behaviour */
            var target = apple.AddComponent<AppleReachTarget>();
            target.Init(this, grabEffect);

            liveApples.Add(apple);
        }
    }

    /// Destroy all current apples (e.g., when leaving the activity)
    public void ClearRow()
    {
        foreach (var a in liveApples)
            if (a) Destroy(a);
        liveApples.Clear();
    }

    /*────────────── called by AppleReachTarget when touched ───────────*/
    public void OnAppleCollected(GameObject apple)
    {
        liveApples.Remove(apple);
        Destroy(apple);

        ReachActivityManager.Instance?.NotifyAppleCollected();

        // If row is empty, tell manager the level is done
        if (liveApples.Count == 0)
            Debug.Log("RowAppleSpawner: All apples collected, notifying manager.");
    }
}
