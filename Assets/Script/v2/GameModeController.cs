using System.Linq;
using UnityEngine;

public class GameModeController : MonoBehaviour
{
    public static GameModeController Instance { get; private set; }

    [Header("Global FX")]
    [SerializeField] GrabEffect globalGrabEffect;
    public GrabEffect GrabFx => globalGrabEffect;

    public ActivityManager Current { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void SwitchMode(TherapyMode mode)
    {
        /* 1)  Eski modu temizle */
        if (Current != null)
            Current.Cleanup();

        /* 2)  Yenisini aktive et */
        var managers = GetComponentsInChildren<ActivityManager>(true);

        foreach (var m in managers)
            m.gameObject.SetActive(m.Mode == mode);

        Current = managers.FirstOrDefault(m => m.Mode == mode);

        Debug.Log($"◎ Mode switched to {mode}. 1-5 ile seviye seç.");
    }
}