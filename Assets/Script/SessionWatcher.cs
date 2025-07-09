using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class SessionWatcher : MonoBehaviour
{
    private FirebaseFirestore db;
    private Coroutine pollingCoroutine;

    private string currentPatientId;
    private string currentSessionId;
    private Dictionary<string, object> lastSessionData;

    public bool isGameRunning = false;

    public event Action<int> OnLevelChanged;
    public event Action<int> OnModeChanged;
    public event Action<int> OnCurlChanged;
    public event Action<int> OnHandChanged;
    public event Action OnSessionEnded;
    public event Action OnSessionStarted;

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        pollingCoroutine = StartCoroutine(PollDatabaseRoutine());
    }

    private IEnumerator PollDatabaseRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (!isGameRunning)
            {
                var task = CheckForActiveSessionAndStartGame();
                yield return new WaitUntil(() => task.IsCompleted);
            }
            else
            {
                var task = WatchCurrentSessionChanges();
                yield return new WaitUntil(() => task.IsCompleted);
            }
        }
    }


    private async Task CheckForActiveSessionAndStartGame()
    {
        var patientsSnapshot = await db.Collection("patients").GetSnapshotAsync();
        foreach (var patientDoc in patientsSnapshot.Documents)
        {
            var sessionsSnapshot = await db.Collection("patients")
                .Document(patientDoc.Id)
                .Collection("sessions")
                .WhereEqualTo("active", true)
                .GetSnapshotAsync();

            if (sessionsSnapshot.Count > 0)
            {
                var sessionDoc = sessionsSnapshot[0];
                currentPatientId = patientDoc.Id;
                currentSessionId = sessionDoc.Id;
                lastSessionData = sessionDoc.ToDictionary();

                Debug.Log($"Game starting with patient: {currentPatientId}, session: {currentSessionId}");

                isGameRunning = true;

                // Trigger game setup events
                OnSessionStarted?.Invoke();
                OnLevelChanged?.Invoke(Convert.ToInt32(lastSessionData["level"]));
                OnModeChanged?.Invoke(Convert.ToInt32(lastSessionData["mode"]));
                OnCurlChanged?.Invoke(Convert.ToInt32(lastSessionData["curl"]));
                OnHandChanged?.Invoke(Convert.ToInt32(lastSessionData["hand"]));
                break;
            }
        }
    }

    private async Task WatchCurrentSessionChanges()
    {
        if (string.IsNullOrEmpty(currentPatientId) || string.IsNullOrEmpty(currentSessionId))
            return;

        var sessionRef = db.Collection("patients")
            .Document(currentPatientId)
            .Collection("sessions")
            .Document(currentSessionId);

        var sessionSnapshot = await sessionRef.GetSnapshotAsync();
        if (!sessionSnapshot.Exists)
        {
            Debug.LogWarning("Session disappeared. Ending game.");
            EndGame();
            return;
        }

        var currentData = sessionSnapshot.ToDictionary();

        CheckAndTriggerUpdate("level", currentData, OnLevelChanged);
        CheckAndTriggerUpdate("mode", currentData, OnModeChanged);
        CheckAndTriggerUpdate("curl", currentData, OnCurlChanged);
        CheckAndTriggerUpdate("hand", currentData, OnHandChanged);

        // Check for session deactivation
        if (currentData.TryGetValue("active", out var activeObj) &&
            !(bool)activeObj)
        {
            Debug.Log("Session ended remotely.");
            EndGame();
        }
    }

    private void CheckAndTriggerUpdate(string key, Dictionary<string, object> currentData, Action<int> callback)
    {
        if (!currentData.ContainsKey(key) || !lastSessionData.ContainsKey(key))
            return;

        if (!Equals(currentData[key], lastSessionData[key]))
        {
            Debug.Log($"{key} changed: {lastSessionData[key]} → {currentData[key]}");
            lastSessionData[key] = currentData[key];
            callback?.Invoke(Convert.ToInt32(currentData[key]));
        }
    }

    private void EndGame()
    {
        isGameRunning = false;
        currentSessionId = null;
        currentPatientId = null;
        lastSessionData = null;

        OnSessionEnded?.Invoke();
    }
}
