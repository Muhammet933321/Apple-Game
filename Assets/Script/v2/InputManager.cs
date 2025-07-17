using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    ActivityManager Curr => GameModeController.Instance.Current;
    public CustomActivityManager customActivityManager;

    public void SetCustomModeSettings(List<Vector3Int> customPositions,TherapyMode mode)
    {
        customActivityManager.gridCoords = customPositions;
        customActivityManager.interactionType = mode;
    }
    void Update()
    {
        /*── MOD seçimi : Z X C V ─────────────────────*/
        if (Input.GetKeyDown(KeyCode.Z))
            GameModeController.Instance.SwitchMode(TherapyMode.Reach);
        else if (Input.GetKeyDown(KeyCode.X))
            GameModeController.Instance.SwitchMode(TherapyMode.Grip);
        else if (Input.GetKeyDown(KeyCode.C))
            GameModeController.Instance.SwitchMode(TherapyMode.Carry);
        else if (Input.GetKeyDown(KeyCode.V))
            GameModeController.Instance.SwitchMode(TherapyMode.Sort);
        else if (Input.GetKeyDown(KeyCode.B))
        {
            GameModeController.Instance.SwitchMode(TherapyMode.Custom); 
            Curr?.StartLevelAt(0);
        }

        /*── LEVEL seçimi : 1-5 ───────────────────────*/
        if (Input.GetKeyDown(KeyCode.Alpha1)) Curr?.StartLevelAt(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Curr?.StartLevelAt(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Curr?.StartLevelAt(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Curr?.StartLevelAt(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Curr?.StartLevelAt(4);

        /*── Ortak tuşlar ─────────────────────────────*/
        if (Input.GetKeyDown(KeyCode.R)) Curr?.Restart();
        if (Input.GetKeyDown(KeyCode.Q)) Curr?.Continue();
        if (Input.GetKeyDown(KeyCode.F)) Curr?.Finish();
    }
}
