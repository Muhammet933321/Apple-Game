using System.Collections.Generic;
using UnityEngine;

public class MeasurementData : MonoBehaviour
{
    // <grid, outcome>
    public Dictionary<Vector3Int, AppleOutcome> Results { get; private set; } = new();

    public void Record(Vector3Int grid, AppleOutcome outcome)
    {
        Results[grid] = outcome;            // son çıkan sonucu yazar
        Debug.Log($"Recorded {outcome} for grid {grid}");
    }

    public List<Vector3Int> Filter(AppleOutcome outcome)
    {
        List<Vector3Int> list = new();
        foreach (var kv in Results)
            if (kv.Value == outcome) list.Add(kv.Key);
        return list;
    }

    public void ResetData() => Results.Clear();
}


[System.Serializable]
public enum AppleOutcome
{
    Unreachable,   // hiç temas yok
    GrabMiss,      // temas var ama tutulamadı  (opsiyonel – isterseniz ekleyin)
    Drop,          // tutuldu ama sepete gitmeden düştü
    WrongBasket,   // yanlış sepete bırakıldı
    Success        // doğru sepete bırakıldı
}
