using System.Collections.Generic;
using UnityEngine;

/// Custom grid tabanlı aktivite.
/// Level kavramı yok; dışarıdan verilen grid indeks listesine göre elma spawnlar.
/// interactionType => Reach / Grip / Carry / Sort etkileşimleri.
/// ActivityManager akışıyla uyumlu (geri sayım, F ile bitirme, progress vb.).
public class CustomActivityManager : ActivityManager
{
    public static CustomActivityManager Instance { get; private set; }
    public override TherapyMode Mode => TherapyMode.Custom;

    [Header("Scene refs")]
    [SerializeField] RowAppleSpawner   spawner;
    [SerializeField] BasketSpawner     basketSpawner;     // Grip & Carry
    [SerializeField] DualBasketSpawner dualBasketSpawner; // Sort

    [Header("Custom Config")]
    [Tooltip("Reach / Grip / Carry / Sort davranışından hangisi?")]
    public TherapyMode interactionType = TherapyMode.Reach;

    [Tooltip("Grid indeks listesi. x:[0..xCount-1], y:[0..yCount-1], z:[0..zCount-1].")]
    public List<Vector3Int> gridCoords = new();

    [Tooltip("Sort modunda opsiyonel elma türleri. Boşsa random.")]
    public List<AppleKind> sortKinds = new();

    /* runtime basket refs (spawn sonrası sakla) */
    Transform basketA; // tek sepet | healthy
    Transform basketB; // rotten

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /*──────────────── Level içerik ─────────────────*/
    // Custom modda her zaman tek 'level' var; ActivityManager lv param'ını gözardı ediyoruz.
    protected override void SpawnLevelContent(int _)
    {
        applesProcessed = 0;
        applesSuccess   = 0;
        applesTotal     = (gridCoords != null) ? gridCoords.Count : 0;

        basketA = basketB = null;

        // Sepet(ler)i interaction tipine göre kur
        switch (interactionType)
        {
            case TherapyMode.Grip:
            case TherapyMode.Carry:
                if (basketSpawner)
                    basketA = basketSpawner.SpawnOrMoveBasket(); // tek sepet
                break;

            case TherapyMode.Sort:
                if (dualBasketSpawner)
                {
                    (basketA, basketB) = dualBasketSpawner.SpawnOrMove(); // healthy, rotten
                }
                break;
        }

        if (!spawner)
        {
            Debug.LogError("CustomActivityManager: RowAppleSpawner eksik!", this);
            return;
        }

        // Elmalar
        switch (interactionType)
        {
            default:
            case TherapyMode.Reach:
                spawner.SpawnCustomReach(gridCoords);
                break;

            case TherapyMode.Grip:
                spawner.SpawnCustomGrip(gridCoords, basketA);
                break;

            case TherapyMode.Carry:
                spawner.SpawnCustomCarry(gridCoords, basketA);
                break;

            case TherapyMode.Sort:
                spawner.SpawnCustomSort(gridCoords, basketA, basketB, sortKinds);
                break;
        }
    }

    /*──────────────── Success callback ─────────────────*/
    public override void NotifySuccess(bool succeeded)
    {
        if (!levelActive) return;

        applesProcessed++;
        if (succeeded) applesSuccess++;
        // Seviye oyuncu F tuşuyla bitirir (ActivityManager temel davranış)
    }

    /*──────────────── Cleanup ─────────────────*/
    public override void Cleanup()
    {
        if (spawner)           spawner.ClearRow();
        if (basketSpawner)     basketSpawner.ClearBasket();
        if (dualBasketSpawner) dualBasketSpawner.ClearBaskets();
    }

    protected override void OnLevelEndCleanup()
    {
        if (spawner)           spawner.ClearRow();
        if (basketSpawner)     basketSpawner.ClearBasket();
        if (dualBasketSpawner) dualBasketSpawner.ClearBaskets();
    }
}
