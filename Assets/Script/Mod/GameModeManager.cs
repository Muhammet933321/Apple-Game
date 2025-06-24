using UnityEngine;

public enum GameMode { Measurement, WrongBasket, DropOnly, Unreachable, Static }

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }
    
    public GameMode CurrentMode { get; private set; } = GameMode.Measurement;

    [Header("Scene References")]
    public MeasurementData data;                 // GameSystems objesindeki component
    public GridAppleSpawner spawner;             // sahnedeki mevcut spawner
    // (insp.’dan sürükle bırak)

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
    

    // ——  MODE ENTRY  ————————————————————————————————
    public void StartMeasurement()
    {
        CurrentMode = GameMode.Measurement;
        data.ResetData();

        spawner.isMeasureMode = true;
    }

    public void StartWrongBasket()
    {
        CurrentMode = GameMode.WrongBasket;

        // Ölçümden gelen sonuçlar arasından sadece WrongBasket olan grid’leri çek
        var wrongGrids = data.Filter(AppleOutcome.WrongBasket);

        // Hiç yanlış elma yoksa UI’den uyarı göstermek isteyebilirsin
        if (wrongGrids.Count == 0)
        {
            Debug.Log("Ölçüm sonuçlarında yanlış sepete konan elma yok.");
            return;
        }

        // Spawner’a sadece bu grid’lerde tekrar elma oluşturmasını söyle
        spawner.SpawnCustomApples(wrongGrids.ToArray());
    }

    public void StartDropOnly()
    {
        CurrentMode = GameMode.DropOnly;
        SpawnFiltered(AppleOutcome.Drop);
    }

    public void StartUnreachable()
    {
        CurrentMode = GameMode.Unreachable;
        SpawnFiltered(AppleOutcome.Unreachable);
    }

    public void StartStatic(Vector3Int[] manualGrids)
    {
        CurrentMode = GameMode.Static;
        spawner.SpawnCustomApples(manualGrids);  // bu helper’ı spawner’a az sonra ekleyeceğiz
    }

    // ——  HELPERS  ————————————————————————————————————
    private void SpawnFiltered(AppleOutcome outcome)
    {
        var grids = data.Filter(outcome);
        spawner.SpawnCustomApples(grids.ToArray());
    }
}