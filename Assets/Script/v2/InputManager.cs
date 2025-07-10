using UnityEngine;

public class InputManager : MonoBehaviour
{
    ActivityManager Curr => GameModeController.Instance.Current;
    SessionWatcher sessionWatcher;

    private void Start()
    {
        sessionWatcher = FindObjectOfType<SessionWatcher>();

        if (sessionWatcher != null)
        {
            sessionWatcher.OnLevelChanged += HandleLevelChanged;
            sessionWatcher.OnModeChanged += HandleModeChanged;
            sessionWatcher.OnCurlChanged += HandleCurlChanged;
            sessionWatcher.OnHandChanged += HandleHandChanged;
            sessionWatcher.OnSessionStarted += HandleSessionStarted;
            sessionWatcher.OnSessionEnded += HandleSessionEnded;
        }
        else
        {
            Debug.LogError("SessionWatcher not found in scene.");
        }
    }

    private void OnDestroy()
    {
        if (sessionWatcher != null)
        {
            sessionWatcher.OnLevelChanged -= HandleLevelChanged;
            sessionWatcher.OnModeChanged -= HandleModeChanged;
            sessionWatcher.OnCurlChanged -= HandleCurlChanged;
            sessionWatcher.OnHandChanged -= HandleHandChanged;
            sessionWatcher.OnSessionStarted -= HandleSessionStarted;
            sessionWatcher.OnSessionEnded -= HandleSessionEnded;
        }
    }

    private void HandleLevelChanged(int levelIndex)
    {
        Curr?.StartLevelAt(levelIndex);
    }

    private void HandleModeChanged(int mode)
    {
        GameModeController.Instance.SwitchMode((TherapyMode)mode);
    }

    private void HandleCurlChanged(int curl)
    {
        Debug.Log($"Curl updated to {curl}");
        // You can add custom behavior if needed.
    }

    private void HandleHandChanged(int hand)
    {
        Debug.Log($"Hand updated to {hand}");
        // You can update visuals or logic for left/right hand preference.
    }

    private void HandleSessionStarted()
    {
        Debug.Log("Session started.");
        // Optional: Initialize UI, sounds, etc.
    }

    private void HandleSessionEnded()
    {
        Debug.Log("Session ended.");
        // Optional: Cleanup or reset the state.
        Curr?.Finish();
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
