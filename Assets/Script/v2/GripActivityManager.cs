using UnityEngine;

public class GripActivityManager : ActivityManager
{
    public static GripActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Grip;

    [SerializeField] RowAppleSpawner spawner;
    [SerializeField] BasketSpawner   basketSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    protected override void SpawnLevelContent(ReachLevel lv)
    {
        Transform basket = basketSpawner.SpawnOrMoveBasket();
        spawner.SpawnRowGrip(lv, basket);
    }

    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;
        /* otomatik Finish kaldırıldı */
    }
}