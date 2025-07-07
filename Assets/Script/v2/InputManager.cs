using UnityEngine;

public class InputManager : MonoBehaviour
{
    ActivityManager Mgr => GameModeController.Instance.Current;

    void Update()
    {
        /* mod seçimi */
        if (Input.GetKeyDown(KeyCode.Alpha1)) GameModeController.Instance.SwitchMode(TherapyMode.Reach);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GameModeController.Instance.SwitchMode(TherapyMode.Grip);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GameModeController.Instance.SwitchMode(TherapyMode.Carry);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GameModeController.Instance.SwitchMode(TherapyMode.Sort);

        /* ortak tuşlar */
        if (Input.GetKeyDown(KeyCode.R)) Mgr?.Restart();
        if (Input.GetKeyDown(KeyCode.Q)) Mgr?.Continue();
        if (Input.GetKeyDown(KeyCode.F)) Mgr?.FinishOrNext();
    }
}