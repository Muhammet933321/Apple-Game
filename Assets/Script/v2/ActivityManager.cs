using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// Base class shared by every therapy mode (Reach, Grip, Carry, Sort).
/// * Handles Restart / Continue / Finish / Level-select (keys R Q F 1-5)
/// * Adds an optional on-screen countdown before gameplay begins.
public abstract class ActivityManager : MonoBehaviour
{
   
    [Header("Countdown UI")]
    [Tooltip("Panel that contains a single TMP_Text child; disabled by default.")]
    [SerializeField] GameObject countdownUI;
    [Tooltip("Seconds to count down before each level starts.")]
    [Range(0.5f, 10f)]
    [SerializeField] float countdownTime = 3f;

    /* runtime – shared for all modes */
    protected int   levelIdx;
    protected int   applesTotal, applesSuccess, applesProcessed;
    protected bool  levelActive;
    protected int   lastPercent;                    // 0–100 %

    public abstract TherapyMode Mode { get; }

    TMP_Text countdownText;
    Coroutine countdownCo;

    /*──────────────── PUBLIC API (called from InputManager / UI) ─────────────*/
    public void Restart()        => StartLevelAt(0);

    public void Continue()
    {
        int idx = GlobalData.progress.LastFullIndex(Mode);
        StartLevelAt(Mathf.Max(0, idx));
    }

    public void StartLevelAt(int idx)
    {
        if (idx < 0)
        {
            Debug.LogWarning($"{Mode}: Level {idx} is not defined.");
            return;
        }

        /* cancel any running countdown */
        if (countdownCo != null)
        {
            StopCoroutine(countdownCo);
            countdownCo = null;
        }

        levelIdx = idx;
        SetupLevel();                 // initialise counters, maybe start countdown
    }

    public void Finish()              // player manually ends level
    {
        if (levelActive) FinishLevel();
    }

    /*──────────────── TEMPLATE FLOW ─────────────────────────────────────────*/
    void SetupLevel()
    {
        applesSuccess   = 0;
        applesProcessed = 0;
        lastPercent     = 0;
        levelActive     = false;      // gameplay not active yet

        if (countdownUI != null)
        {
            if (countdownText == null)
                countdownText = countdownUI.GetComponentInChildren<TMP_Text>();

            countdownCo = StartCoroutine(LevelCountdownRoutine());
        }
        else
        {
            BeginGameplay();
        }
    }

    IEnumerator LevelCountdownRoutine()
    {
        countdownUI.SetActive(true);
        float t = countdownTime;

        while (t > 0f)
        {
            countdownText.text = Mathf.CeilToInt(t).ToString();
            yield return null;
            t -= Time.deltaTime;
        }

        countdownUI.SetActive(false);
        countdownCo = null;
        BeginGameplay();
    }

    void BeginGameplay()
    {
        levelActive = true;
        SpawnLevelContent(levelIdx);

        Debug.Log($"► {Mode} L{levelIdx} start  ({applesTotal} apples)");
    }

    protected void FinishLevel()
    {
        levelActive   = false;
        lastPercent   = Mathf.RoundToInt((float)applesSuccess / applesTotal * 100f);
        GlobalData.progress.SetPercent(Mode, levelIdx, lastPercent);

        // Optional: write to on-screen progress log
        FindObjectOfType<ProgressLog>()?.
            AddEntry(Mode.ToString(), levelIdx, lastPercent);

        Debug.Log($"■ {Mode} L{levelIdx}  %{lastPercent}  "
                + $"({applesSuccess}/{applesTotal})");

        OnLevelEndCleanup();          // remove leftover apples / basket
    }

    /*──────────────── ABSTRACT METHODS ─────────────────────────────────────*/
    protected abstract void SpawnLevelContent(int lv);
    public    abstract void NotifySuccess(bool succeeded);

    /*──────────────── HOOKS ────────────────────────────────────────────────*/
    /// Called when player switches away from this mode (Z X C V).
    public virtual void Cleanup() { }

    /// Called right after FinishLevel saves the result.
    protected virtual void OnLevelEndCleanup() { }
}
