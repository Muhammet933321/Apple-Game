using System;
using System.Collections.Generic;
using UnityEngine;

public enum ReachResult { Tam, Yari, Ceyrek, Sifir }

public class ReachActivityManager : MonoBehaviour
{
    public static ReachActivityManager Instance { get; private set; }

    [Header("Level setup")]
    public List<ReachLevel> levels;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner spawner;     

    int   levelIndex;
    int   applesCollected;
    bool levelActive = false; 
    ReachProgressData progress;                   // heat-map / save-file holder

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        progress = ReachProgressData.Load();      // your own persistence
    }

    /*────────── PUBLIC ENTRY POINTS ──────────*/

    // therapist hits “Başlat” – choose start = 0 or last complete
    public void StartReachActivity(bool continueFromLast)
    {
        levelIndex = continueFromLast ? progress.lastFullIndex : 0;
        StartLevel(levelIndex);
    }

    /*────────── Level lifecycle ──────────*/

    void StartLevel(int idx)
    {
        applesCollected = 0;
        levelIndex      = idx;
        levelActive     = true;         // seviye başladı
        var lv = levels[idx];

        spawner.SpawnRow(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg);
    }

    public void NotifyAppleCollected() => applesCollected++;

    public void LevelFinished()
    {
        // simple grading rule example
        if (!levelActive) return;       // ikinci çağrıyı YOK say
        levelActive = false;
        ReachResult res;
        int total = levels[levelIndex].appleCount;
        float ratio = (float)applesCollected / total;

        if      (ratio >= 1f)   res = ReachResult.Tam;
        else if (ratio >= .75f) res = ReachResult.Yari;
        else if (ratio >= .50f) res = ReachResult.Ceyrek;
        else                    res = ReachResult.Sifir;

        progress.Store(levelIndex, res);             // update heat-map

        // prepare next level or stop
        levelIndex++;
        bool more = levelIndex < levels.Count;

        if (more)
            StartLevel(levelIndex);
        else
        {
            spawner.ClearRow();            // show summary / restart
        }
    }

    /*────────── Therapist UI buttons ──────────*/

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnRestart();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            OnContinue();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (levelActive)
            {
                LevelFinished();
            }
        }
        
    }

    public void OnRestart()  => StartReachActivity(false);  // “Baştan Başlat”
    public void OnContinue() => StartReachActivity(true);   // “Kaldığı Yerden”
}
