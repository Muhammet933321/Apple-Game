using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReachProgressData
{
    public List<ReachResult> results = new();   // one entry per level

    public int lastFullIndex
        => results.FindLastIndex(r => r == ReachResult.Tam);

    /*──── persistence helpers ────*/
    const string KEY = "ReachProgressJson";

    public void Store(int level, ReachResult res)
    {
        while (results.Count <= level) results.Add(ReachResult.Sifir);
        results[level] = res;
        Save();
    }

    void Save() => PlayerPrefs.SetString(KEY, JsonUtility.ToJson(this));

    public static ReachProgressData Load()
    {
        string json = PlayerPrefs.GetString(KEY, "");
        return string.IsNullOrEmpty(json)
            ? new ReachProgressData()
            : JsonUtility.FromJson<ReachProgressData>(json);
    }
}