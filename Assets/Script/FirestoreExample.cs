using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirestoreAppointmentManager : MonoBehaviour
{
    FirebaseFirestore db;
    FirebaseAuth auth;

    [Header("UI Elements")]
    public TMP_Text statusText;

    public GameObject appointmentPanel;
    public GameObject loginPanel;

    public TMP_Text caregiverText;
    
    public Button joinButton;
    public Button refreshButton;
    
    public bool freePlay = false;
    public float checkInterval = 5f; // Check every 5 seconds
    private float timer = 0f;    
    
    private bool gameStarted = false;
    private string firstAcceptedAppointmentId = null;
    private object lastKnownLevel = null;
    
    
    private Vector3 lastApplePosition = Vector3.zero;
    private Vector3 lastBasketPosition = Vector3.zero;
    
    private AppleSpawner spawner;
    void Start()
    {
        //StartCoroutine(WriteAppointment());
        //StartCoroutine(ReadAppointments());
    }

    public void StartFireStore(FirebaseUser user)
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        
        StartCoroutine(ReadAppointments(user));
    }

    public void StartLevel9()
    {
        StartCoroutine(CheckPositionsRoutine());
    }

    IEnumerator WriteAppointment()
    {
        yield return new WaitUntil(() => auth.CurrentUser != null);

        DocumentReference docRef = db.Collection("appointments").Document();

        Dictionary<string, object> appointment = new Dictionary<string, object>
        {
            { "caregiverId", auth.CurrentUser.UserId },
            { "caregiverName", "Bakıcı 1" },
            { "createdAt", Timestamp.GetCurrentTimestamp() },
            { "date", "2025-05-16T13:24:00" },
            { "location", "Robotik Club" },
            { "notes", "Test randevusu notu" },
            { "patientId", "PrTIpC78ZgeuLPZiCUjaboMOggT2" },
            { "patientName", "Muhammetalp" },
            { "status", "accepted" },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        };

        var writeTask = docRef.SetAsync(appointment);

        yield return new WaitUntil(() => writeTask.IsCompleted);

        if (writeTask.Exception != null)
        {
            Debug.LogError("Write failed: " + writeTask.Exception.Message);
            statusText.text = "Write failed.";
        }
        else
        {
            Debug.Log("Appointment written successfully.");
            statusText.text = "Appointment written!";
        }
    }

    IEnumerator ReadAppointments(FirebaseUser firebaseUser)
    {
        if (firebaseUser == null)
        {
            Debug.LogWarning("No user is logged in.");
            statusText.text = "User not logged in.";
            yield break;
        }

        string userId = firebaseUser.UserId;
        CollectionReference appointmentsRef = db.Collection("appointments");

        // Apply both filters: patientId == userId AND status == "accepted"
        Query query = appointmentsRef
            .WhereEqualTo("patientId", userId)
            .WhereEqualTo("status", "accepted");

        var readTask = query.GetSnapshotAsync();

        yield return new WaitUntil(() => readTask.IsCompleted);

        if (readTask.Exception != null)
        {
            Debug.LogError("Read failed: " + readTask.Exception.Message);
            statusText.text = "Read failed.";
        }
        else
        {
            QuerySnapshot snapshot = readTask.Result;
            Debug.Log("List of Appointments read."); 
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();
                Debug.Log($"Accepted Appointment: {doc.Id} => Patient: {data["patientName"]}, Location: {data["location"]}");
                ActivateAppointment(data["caregiverName"].ToString(), doc.Id);
                yield return null;
            }

            ChangePanel();
            statusText.text = $"Found {snapshot.Count} accepted appointments.";
        }
    }

    private void ChangePanel()
    {
        loginPanel.SetActive(false);
        appointmentPanel.SetActive(true);
    }
    
    private void ActivateAppointment(string caregiverName, string appointmentId)
    {
        firstAcceptedAppointmentId = appointmentId;
        ChangePanel();

        caregiverText.text = caregiverName;
        joinButton.interactable = true;
    }

    public void RefreshList()
    {
        StartCoroutine(ReadAppointments(auth.CurrentUser));
    }

    public void StartFreePlay()
    {
        freePlay = true;
        gameStarted = true;
        SceneManager.LoadScene("Game");
    }
    
    public void StartAppointmentPlay()
    {
        freePlay = false;
        gameStarted = true;
        SceneManager.LoadScene("Game");
    }
    
    private void Update()
    {
        if (string.IsNullOrEmpty(firstAcceptedAppointmentId))
            return;
        if (gameStarted)
        {
            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                StartCoroutine(CheckForCurrentLevelChange());
            }  
        }
    }
    
    private IEnumerator ReadFirstAcceptedAppointment(FirebaseUser user)
    {
        Query query = db.Collection("appointments")
            .WhereEqualTo("patientId", user.UserId)
            .WhereEqualTo("status", "accepted");

        var task = query.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Failed to fetch appointment: " + task.Exception.Message);
        }
        else
        {
            var snapshot = task.Result;
            if (snapshot.Count > 0)
            {
                DocumentSnapshot doc = snapshot[0];
                firstAcceptedAppointmentId = doc.Id;

                var data = doc.ToDictionary();
                lastKnownLevel = data.ContainsKey("currentLevel") ? data["currentLevel"] : null;

                Debug.Log($"First appointment set: {firstAcceptedAppointmentId}, currentLevel: {lastKnownLevel}");
            }
        }
    }

private IEnumerator CheckForCurrentLevelChange()
{
    DocumentReference docRef = db.Collection("appointments").Document(firstAcceptedAppointmentId);

    var task = docRef.GetSnapshotAsync();
    yield return new WaitUntil(() => task.IsCompleted);

    if (task.Exception != null)
    {
        Debug.LogError("Failed to check appointment: " + task.Exception.Message);
        yield break;
    }

    DocumentSnapshot snapshot = task.Result;
    if (!snapshot.Exists)
        yield break;

    Dictionary<string, object> data = snapshot.ToDictionary();

    // ✅ Check for 'cancelled' status
    if (data.TryGetValue("status", out object statusObj) && statusObj.ToString() == "cancelled")
    {
        Debug.LogWarning("Appointment has been cancelled by the caregiver.");
        statusText.text = "Randevu iptal edildi. Ana menüye dönülüyor.";

        // Optionally, load a different scene or show a UI
        yield return new WaitForSeconds(3f); // Give user time to read message
        SceneManager.LoadScene("MainMenu"); // Replace with your actual menu scene
        yield break;
    }

    // ✅ Check for currentLevel change
    if (data.TryGetValue("currentLevel", out object newLevel))
    {
        if (lastKnownLevel == null || !lastKnownLevel.Equals(newLevel))
        {
            Debug.Log($"currentLevel changed! Old: {lastKnownLevel}, New: {newLevel}");
            lastKnownLevel = newLevel;

            if (spawner == null)
            {
                spawner = FindAnyObjectByType<AppleSpawner>();
            }

            if (newLevel is long longVal)
            {
                int lvl = (int)longVal;

                if (System.Enum.IsDefined(typeof(AppleSpawner.SpawnLevel), lvl))
                {
                    AppleSpawner.SpawnLevel level = (AppleSpawner.SpawnLevel)lvl;
                    spawner.SetLevel(level);
                }
                else
                {
                    Debug.LogWarning("Invalid level int from Firestore.");
                }
            }
            else
            {
                Debug.LogWarning("currentLevel is not a number.");
            }
        }
    }
}
      IEnumerator CheckPositionsRoutine()
    {
        while (true)
        {
            if (spawner == null)
            {
                spawner = FindAnyObjectByType<AppleSpawner>();
            }
            yield return new WaitForSeconds(checkInterval);
            
            Debug.Log("9999999");

            if (spawner.spawnLevel != AppleSpawner.SpawnLevel.Level9)
            {
                continue;
            }
            DocumentReference positionRef = db
                .Collection("appointments")
                .Document(firstAcceptedAppointmentId)
                .Collection("position")
                .Document("data");

            var task = positionRef.GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                Debug.LogError("Failed to read position: " + task.Exception.Message);
            }
            else
            {
                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();

                    if (data.TryGetValue("applePosition", out object appleObj) &&
                        data.TryGetValue("basketPosition", out object basketObj))
                    {
                        Dictionary<string, object> appleDict = appleObj as Dictionary<string, object>;
                        Dictionary<string, object> basketDict = basketObj as Dictionary<string, object>;

                        Vector3 newApplePos = new Vector3(
                            Convert.ToSingle(appleDict["x"]),
                            Convert.ToSingle(appleDict["y"]),
                            Convert.ToSingle(appleDict["z"])
                        );

                        Vector3 newBasketPos = new Vector3(
                            Convert.ToSingle(basketDict["x"]),
                            Convert.ToSingle(basketDict["y"]),
                            Convert.ToSingle(basketDict["z"])
                        );

                        if (newApplePos != lastApplePosition || newBasketPos != lastBasketPosition)
                        {
                            Debug.Log($"🍎 Apple Position Changed: {newApplePos}");
                            Debug.Log($"🧺 Basket Position Changed: {newBasketPos}");

                            lastApplePosition = newApplePos;
                            lastBasketPosition = newBasketPos;

                            // TODO: Use new positions (e.g., move objects)
                        }
                    }
                }
            }
        }
    }
      
    public async void SendProgress()
    {
        if (string.IsNullOrEmpty(firstAcceptedAppointmentId))
        {
            Debug.LogWarning("No appointment ID available to save progress.");
            return;
        }

        try
        {
            string progressId = auth.CurrentUser.UserId.ToString(); // assuming progress doc is linked to appointment ID
            int dayOffset = 0; // today

            int holdDuration = Mathf.RoundToInt(FindAnyObjectByType<GrabDurationChecker>().GetAverageGrabDuration());

            await AppendProgressDataAsync(progressId, spawner.GetProgress(),  holdDuration, dayOffset);

            Debug.Log("✅ Progress saved successfully.");
            statusText.text = "Progress saved!";
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to save progress: " + ex.Message);
            statusText.text = "Failed to save progress.";
        }
    }
      
    public async Task AppendProgressDataAsync(string progressId, int progress, int holdDuration, int dayOffset = 0)
    {
        DocumentReference docRef = db.Collection("progress").Document(progressId);

        Dictionary<string, object> newEntry = new Dictionary<string, object>
        {
            { "date", DateTime.UtcNow.AddDays(dayOffset).ToString("dd.MM.yyyy") },
            { "progress", progress },
            { "holdDuration", holdDuration },
            { "rom", spawner.rom },
            { "updatedAt", Timestamp.GetCurrentTimestamp().ToDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
        };

        // Append to array field 'data' using arrayUnion
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            { "data", FieldValue.ArrayUnion(newEntry) }
        };

        await docRef.UpdateAsync(update);
    }
      
    
}
