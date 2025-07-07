using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReachProgressData
{
    public List<int> percentResults = new();     // her seviye için %00–%100

    public int lastFullIndex =>
        percentResults.FindLastIndex(p => p == 100);

    /*────────── Persistence ─────────*/
    const string KEY = "ReachProgressJson";

    public void Store(int level, int percent)
    {
        while (percentResults.Count <= level) percentResults.Add(0);
        percentResults[level] = Mathf.Clamp(percent, 0, 100);
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