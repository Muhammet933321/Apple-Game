using System.Collections.Generic;
using UnityEngine;

/// Mod-bağımsız ortak akış (Restart / Continue / FinishOrNext tuşları aynıdır)
public abstract class ActivityManager : MonoBehaviour
{
    [Header("Level setup")]
    public List<ReachLevel> levels;              // tüm modlar aynı seviye tanımını paylaşıyor

    /* runtime */
    protected int  levelIdx;
    protected int  applesTotal, applesSuccess;
    protected bool levelActive;
    protected int  lastPercent;                  // 0-100

    /* alt sınıf enum’unu döndürür */
    public abstract TherapyMode Mode { get; }

    /*────────── API (InputManager R/Q/F) ─────────*/
    public void Restart()  => StartActivity(fromLast:false);
    public void Continue() => StartActivity(fromLast:true);
    public void FinishOrNext()
    {
        if (levelActive) FinishLevel();
        else if (lastPercent == 100 && levelIdx + 1 < levels.Count)
        {
            levelIdx++;
            StartLevel();
        }
    }

    /*────────── template akış ─────────*/
    void StartActivity(bool fromLast)
    {
        levelIdx = fromLast ? Mathf.Max(0, GlobalData.progress.LastFullIndex(Mode)) : 0;
        StartLevel();
    }

    void StartLevel()
    {
        if (levels.Count == 0) { Debug.LogError($"{Mode}: level list empty"); return; }

        applesSuccess = 0;
        applesTotal   = levels[levelIdx].appleCount;
        levelActive   = true;
        lastPercent   = 0;

        SpawnLevelContent(levels[levelIdx]);     // ← alt sınıf sağlar
        Debug.Log($"► {Mode} L{levelIdx} start ({applesTotal} apple)");
    }

    protected void FinishLevel()
    {
        if (!levelActive) return;
        levelActive = false;

        lastPercent = Mathf.RoundToInt((float)applesSuccess / applesTotal * 100f);
        GlobalData.progress.SetPercent(Mode, levelIdx, lastPercent);

        Debug.Log($"■ {Mode} L{levelIdx}  %{lastPercent}");
    }

    /*────────── override edilmesi gerekenler ─────────*/
    protected abstract void SpawnLevelContent(ReachLevel lv);
    public    abstract void NotifySuccess(bool succeeded);   // elma geri bildirim
}
