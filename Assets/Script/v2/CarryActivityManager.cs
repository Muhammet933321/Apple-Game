using UnityEngine;

/// Mod-3 : Elmayı sepete TAŞI ve BIRAK
public class CarryActivityManager : ActivityManager
{
    public static CarryActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Carry;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner spawner;
    [SerializeField] BasketSpawner   basketSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /*────────── Level spawn ─────────*/
    protected override void SpawnLevelContent(ReachLevel lv)
    {
        Transform basket = basketSpawner.SpawnOrMoveBasket();
        spawner.SpawnRowCarry(lv, basket);

        applesProcessed = 0;
        applesTotal     = lv.appleCount;
    }

    /*────────── Success / fail from AppleCarryTarget ─────────*/
    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;
        // level ends with F key only
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