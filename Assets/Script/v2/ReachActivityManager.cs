using UnityEngine;

public class ReachActivityManager : ActivityManager
{
    /*──── singleton ────*/
    public static ReachActivityManager Instance { get; private set; }

    public override TherapyMode Mode => TherapyMode.Reach;

    [SerializeField] RowAppleSpawner spawner;

    void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    /*──────── spawn / success────────*/
    protected override void SpawnLevelContent(ReachLevel lv) =>
        spawner.SpawnRow(lv.appleCount, lv.height, lv.distance, lv.arcSpanDeg);

    public override void NotifySuccess(bool _)
    {
        if (!levelActive) return;
        applesSuccess++;

        if (spawner.RowEmpty)   // tüm elmalar toplandı
            Debug.Log($"Reach L{levelIdx}  %{lastPercent} success");
    }
}