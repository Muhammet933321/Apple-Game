using UnityEngine;

public class SortActivityManager : ActivityManager
{
    public static SortActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Sort;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner      spawner;
    [SerializeField] DualBasketSpawner    dualSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /*────────── Level spawn ─────────*/
    protected override void SpawnLevelContent(int lv)
    {
        var (healthy, rotten) = dualSpawner.SpawnOrMove();
        spawner.SpawnSortLevel(lv, healthy, rotten);

        applesProcessed = 0;
        applesTotal     = 8;
    }

    /*────────── Success / fail ─────────*/
    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;
        /* player ends with F key */
    }

    /*────────── Cleanup ─────────*/
    public override void Cleanup()
    {
        spawner.ClearRow();
        dualSpawner.ClearBaskets();
    }

    protected override void OnLevelEndCleanup()
    {
        spawner.ClearRow();
        dualSpawner.ClearBaskets();
    }
}