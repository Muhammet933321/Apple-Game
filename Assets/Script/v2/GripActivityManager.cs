using UnityEngine;

///  ────────────────────────────────────────────────────────────
///  Kavrama (Grip) Aktivitesi Manager’ı
///  - ActivityManager temel sınıfından türemiştir.
///  - Elmayı sepete “eldeyken” getirmek başarıdır.
///  - Droplar başarısız sayılır.
///  - Seviye, sahnede spawn edilen gerçek elma sayısı kadar
///    başarı-başarısız olayı işlendiğinde biter.
///  ────────────────────────────────────────────────────────────
public class GripActivityManager : ActivityManager
{
    public static GripActivityManager Instance { get; private set; }

    public override TherapyMode Mode => TherapyMode.Grip;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner spawner;
    [SerializeField] BasketSpawner   basketSpawner;

    int applesProcessed;                 // başarı + başarısız toplamı

    void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    /*────────── Seviye içeriğini oluştur ─────────*/
    protected override void SpawnLevelContent(ReachLevel lv)
    {
        Transform basket = basketSpawner.SpawnOrMoveBasket();
        
        applesTotal = spawner.SpawnRowGrip(lv, basket);
        applesProcessed = 0;                       // sıfırla

        Debug.Log($"► Grip L{levelIdx}  spawned {applesTotal} apples");
    }

    /*────────── AppleGripTarget -> NotifySuccess() çağırır ─────────*/
    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;

        if (applesProcessed >= applesTotal)        // hepsi işlendi
            FinishLevel();
    }
}