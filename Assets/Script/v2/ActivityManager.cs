using System.Collections.Generic;
using UnityEngine;

/// Shared flow for all therapy modes.
/// – Restart / Continue / Finish / Level-select (1-5)
/// – Each derived manager only spawns its content + counts successes.
public abstract class ActivityManager : MonoBehaviour
{
    public ProgressLog progressLog;
    [Header("Level setup")]
    public List<ReachLevel> levels;

    /* runtime */
    protected int   levelIdx;
    protected int   applesTotal, applesSuccess, applesProcessed;
    protected bool  levelActive;
    protected int   lastPercent;                    // 0–100

    public abstract TherapyMode Mode { get; }

    /*────────── Public API (called from Input/UI) ─────────*/
    public void Restart()        => StartLevelAt(0);

    public void Continue()
    {
        int idx = GlobalData.progress.LastFullIndex(Mode);
        StartLevelAt(Mathf.Max(0, idx));
    }

    public void StartLevelAt(int idx)
    {
        if (idx < 0 || idx >= levels.Count)
        {
            Debug.LogWarning($"{Mode}: Level {idx} not defined.");
            return;
        }
        levelIdx = idx;
        StartLevel();
    }

    public void Finish()         // Finish only – no auto “next”
    {
        if (levelActive) FinishLevel();
    }

    /*────────── Template flow ─────────*/
    void StartLevel()
    {
        applesSuccess = applesProcessed = 0;
        applesTotal   = levels[levelIdx].appleCount;
        lastPercent   = 0;
        levelActive   = true;

        SpawnLevelContent(levels[levelIdx]);
        Debug.Log($"► {Mode} L{levelIdx} start  ({applesTotal} apples)");
    }

    protected void FinishLevel()
    {
        levelActive  = false;
        lastPercent  = Mathf.RoundToInt((float)applesSuccess / applesTotal * 100f);
        GlobalData.progress.SetPercent(Mode, levelIdx, lastPercent);

        Debug.Log($"■ {Mode} L{levelIdx}  %{lastPercent}  "
                + $"({applesSuccess}/{applesTotal})");
        progressLog?.AddEntry(Mode.ToString(), levelIdx, lastPercent);
        OnLevelEndCleanup();             // ← NEW: remove leftover items
    }

    /*────────── Must be implemented by derived classes ─────────*/
    protected abstract void SpawnLevelContent(ReachLevel lv);
    public    abstract void NotifySuccess(bool succeeded);

    /*────────── Optional hooks ─────────*/
    public virtual void Cleanup()        { }        // called when leaving mode
    protected virtual void OnLevelEndCleanup() { }  // called when level finishes
}
