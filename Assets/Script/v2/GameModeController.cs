using System.Linq;
using UnityEngine;

/// Yalnızca aktif ActivityManager’ı açıp kapatır.
/// Seviye otomatik başlamaz; 1-5 tuşlarıyla seçilir.
public class GameModeController : MonoBehaviour
{
    public static GameModeController Instance { get; private set; }

    [Header("Global FX")]
    [SerializeField] GrabEffect globalGrabEffect;   // ← VFX / SFX prefab
    public GrabEffect GrabFx => globalGrabEffect;   // Apple*Target’lar buradan okur

    [Tooltip("Geçerli modun manager’ı (henüz level yoksa null)")]
    public ActivityManager Current { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    /// Z X C V tuşlarından çağrılır.
    /// Sadece ilgili manager’ı aktive eder, Restart etmez.
    public void SwitchMode(TherapyMode mode)
    {
        var managers = GetComponentsInChildren<ActivityManager>(true);

        foreach (var m in managers)
            m.gameObject.SetActive(m.Mode == mode);   // yalnız bu mod açık

        Current = managers.FirstOrDefault(m => m.Mode == mode);

        Debug.Log($"◎ Mode switched to {mode}. 1-5 tuşuyla seviye seç.");
    }
}