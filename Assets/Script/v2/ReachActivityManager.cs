using System.Collections.Generic;
using UnityEngine;

public class ReachActivityManager : MonoBehaviour
{
    public static ReachActivityManager Instance { get; private set; }

    /*────────── Inspector ──────────*/
    [Header("Level setup")]
    public List<ReachLevel> levels;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner spawner;

    /*────────── Runtime ──────────*/
    int   levelIndex;
    int   applesCollected;
    bool  levelActive = false;
    int   lastPercent = 0;                 // %00 – %100
    ReachProgressData progress;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance  = this;
        progress  = ReachProgressData.Load();
    }

    /*────────── PUBLIC ENTRY ───────*/
    public void StartReachActivity(bool continueFromLast)
    {
        int start = continueFromLast ? progress.lastFullIndex : 0;

        // Hiç %100 seviye yoksa veya listenin dışındaysa → 0’dan başla
        if (start < 0 || start >= levels.Count) start = 0;

        StartLevel(start);
    }

    /*────────── Level lifecycle ────*/
    void StartLevel(int idx)
    {
        // Yine de son kontrol – boş level listesi hatasına karşı
        if (levels.Count == 0)
        {
            Debug.LogError("ReachActivityManager: 'levels' list is empty!");
            return;
        }

        if (idx < 0 || idx >= levels.Count)
        {
            Debug.LogError($"ReachActivityManager: level index {idx} is out of range (0–{levels.Count-1}).");
            return;
        }

        applesCollected = 0;
        levelIndex      = idx;
        levelActive     = true;
        lastPercent     = 0;

        var lv = levels[idx];
        spawner.SpawnRow(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg);

        Debug.Log($"► Level {idx} started");
    }

    public void NotifyAppleCollected() => applesCollected++;

    public void LevelFinished()
    {
        if (!levelActive) return;
        levelActive = false;
        spawner.ClearRow();
        /* Liste sınırı yine kontrol: */
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError($"ReachActivityManager: level index {levelIndex} invalid during finish.");
            return;
        }

        int total   = levels[levelIndex].appleCount;
        float ratio = (float)applesCollected / total;
        lastPercent = Mathf.RoundToInt(ratio * 100f);

        progress.Store(levelIndex, lastPercent);
        Debug.Log($"■ Level {levelIndex} finished — Başarı: %{lastPercent}");
    }

    /*────────── Therapist keys ─────*/
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))      OnRestart();
        else if (Input.GetKeyDown(KeyCode.Q)) OnContinue();
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (levelActive)
            {
                // Seviye hâlâ sürüyor → bitir
                LevelFinished();
            }
            else
            {
                // Seviye bitmiş; %100 ise ve sıradaki seviye varsa → ilerle
                bool success = lastPercent == 100;
                bool hasNext = levelIndex + 1 < levels.Count;

                if (success && hasNext)
                {
                    levelIndex++;
                    StartLevel(levelIndex);
                }
            }
        }
    }

    /*────────── Shortcuts ───────────*/
    public void OnRestart()  => StartReachActivity(false);   // R
    public void OnContinue() => StartReachActivity(true);    // Q
}
