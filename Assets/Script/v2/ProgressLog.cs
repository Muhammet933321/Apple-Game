using System;
using System.Collections.Generic;
using UnityEngine;

/// Tüm aktiviteler (Reach, Grip, …) için seviye-bazlı başarı yüzdelerini saklar
/// ve Inspector’da okunabilir kılar.
public class ProgressLog : MonoBehaviour
{
    [Serializable]
    public struct LevelEntry
    {
        public string activity;   // örn. "Reach"
        public int    level;      // 0,1,2,…
        public int    percent;    // 0-100
        public string timestamp;  // "2025-07-07 20:15"
        public BothHandLog handLogs;
        public LevelEntry(string act, int lvl, int pct)
        {
            activity  = act;
            level     = lvl;
            percent   = pct;
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            handLogs = new BothHandLog();
        }
    }

    public List<LevelEntry> history = new();   // Inspector’da tablo gibi görünür

    /// Çağırıldığında yeni kayıt ekler
    public void AddEntry(string activity, int level, int percent)
    {
        history.Add(new LevelEntry(activity, level, percent));
    }
}