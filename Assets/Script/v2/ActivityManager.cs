using System.Collections.Generic;
using UnityEngine;

/// Ortak akış: Restart, Continue, Finish, SeçiliLevel.
public abstract class ActivityManager : MonoBehaviour
{
    [Header("Level setup")]
    public List<ReachLevel> levels;

    /* runtime */
    protected int   levelIdx;
    protected int   applesTotal, applesSuccess, applesProcessed;
    protected bool  levelActive;
    protected int   lastPercent;

    public abstract TherapyMode Mode { get; }

    /*────────── ortak API ─────────*/
    public void Restart()        => StartLevelAt(0);
    public void Continue()
    {
        int idx = GlobalData.progress.LastFullIndex(Mode);
        StartLevelAt(Mathf.Max(0, idx));
    }

    /// Klavye 1-5 ile doğrudan seviye seçimi
    public void StartLevelAt(int idx)
    {
        if (idx < 0 || idx >= levels.Count)
        {
            Debug.LogWarning($"{Mode}: level {idx} yok.");
            return;
        }
        levelIdx = idx;
        StartLevel();
    }

    public void Finish()            // yalnızca bitir
    {
        if (!levelActive) return;
        FinishLevel();
    }

    /*────────── template ─────────*/
    void StartLevel()
    {
        applesSuccess = applesProcessed = 0;
        applesTotal   = levels[levelIdx].appleCount;
        lastPercent   = 0;
        levelActive   = true;

        SpawnLevelContent(levels[levelIdx]);
        Debug.Log($"► {Mode} L{levelIdx} start ({applesTotal} apples)");
    }

    protected void FinishLevel()
    {
        levelActive = false;
        lastPercent = Mathf.RoundToInt((float)applesSuccess / applesTotal * 100f);
        GlobalData.progress.SetPercent(Mode, levelIdx, lastPercent);
        Debug.Log($"■ {Mode} L{levelIdx}  %{lastPercent}  (processed {applesProcessed})");
    }

    /*────────── çocuklar sağlar ─────────*/
    protected abstract void SpawnLevelContent(ReachLevel lv);
    public    abstract void NotifySuccess(bool succeeded);
}