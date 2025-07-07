using System.Collections.Generic;
using UnityEngine;

public enum TherapyMode { Reach = 1, Grip, Carry, Sort }

[System.Serializable]
public class TherapyProgressData
{
    // mode ⇒ list-of-% per level
    public Dictionary<TherapyMode, List<int>> results =
        new Dictionary<TherapyMode, List<int>>();

    /* basic helpers */
    public int GetPercent(TherapyMode m, int lvl) =>
        results.TryGetValue(m, out var list) && lvl < list.Count ? list[lvl] : 0;

    public void SetPercent(TherapyMode m, int lvl, int pct)
    {
        if (!results.TryGetValue(m, out var list))
            results[m] = list = new List<int>();

        while (list.Count <= lvl) list.Add(0);
        list[lvl] = Mathf.Clamp(pct, 0, 100);
        Save();
    }

    public int LastFullIndex(TherapyMode m) =>
        results.TryGetValue(m, out var list) ? list.FindLastIndex(p => p == 100) : -1;

    /* persistence */
    const string KEY = "TherapyProgressJson";
    void Save()               => PlayerPrefs.SetString(KEY, JsonUtility.ToJson(this));
    public static TherapyProgressData Load() =>
        JsonUtility.FromJson<TherapyProgressData>(
            PlayerPrefs.GetString(KEY, JsonUtility.ToJson(new TherapyProgressData())));
}

/* static handle used everywhere */
public static class GlobalData
{
    public static readonly TherapyProgressData progress = TherapyProgressData.Load();
}