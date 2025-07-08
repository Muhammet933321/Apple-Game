using UnityEngine;

public class GripActivityManager : ActivityManager
{
    public static GripActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Grip;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner spawner;
    [SerializeField] BasketSpawner   basketSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /*────────── Level content ─────────*/
    protected override void SpawnLevelContent(ReachLevel lv)
    {
        Transform basket = basketSpawner.SpawnOrMoveBasket();
        spawner.SpawnRowGrip(lv, basket);

        applesProcessed = 0;          // counters reset here
        applesTotal     = lv.appleCount;
    }

    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;
        /* player finishes with F */
    }

    /*────────── Cleanup hooks ─────────*/
    public override void Cleanup()
    {
        spawner.ClearRow();
        basketSpawner.ClearBasket();
    }

    protected override void OnLevelEndCleanup()
    {
        spawner.ClearRow();
        basketSpawner.ClearBasket();
    }
}