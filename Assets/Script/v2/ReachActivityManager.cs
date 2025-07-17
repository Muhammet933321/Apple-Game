using UnityEngine;

public class ReachActivityManager : ActivityManager
{
    public static ReachActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Reach;

    [SerializeField] RowAppleSpawner spawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /*────────── Level content ─────────*/
    protected override void SpawnLevelContent(int lv)
    {
        spawner.SpawnReachLevel(lv);
        applesProcessed = 0;          // reset counters
        applesTotal     = 8;          // 8 apples per level
    }

    public override void NotifySuccess(bool _)
    {
        if (!levelActive) return;

        applesProcessed++;     // each touch counts
        applesSuccess++;
        /* no auto-finish – player must press F */
    }

    /*────────── Cleanup hooks ─────────*/
    public override void Cleanup()             => spawner.ClearRow();
    protected override void OnLevelEndCleanup() => spawner.ClearRow();
}