using System.Linq;
using UnityEngine;

public class GameModeController : MonoBehaviour
{
    public static GameModeController Instance { get; private set; }
    public ActivityManager Current { get; private set; }

    public GrabEffect GrabFx; 

    void Awake() { if (Instance == null) Instance = this; }

    public void SwitchMode(TherapyMode mode)
    {
        foreach (var m in GetComponentsInChildren<ActivityManager>(true))
            m.gameObject.SetActive(m.Mode == mode);

        Current = GetComponentsInChildren<ActivityManager>(true)
            .FirstOrDefault(m => m.Mode == mode);

        Current?.Restart();
        Debug.Log("Switched to mode: " + mode);
    }
}