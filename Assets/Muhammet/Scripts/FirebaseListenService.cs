using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseListenService : MonoBehaviour
{
    private DatabaseReference dbRef;
    private DatabaseReference gameConfigRef;
    private DatabaseReference patientSceneRef;

    private DatabaseReference patientIDRef;

    private FirebaseDatabaseService databaseService;
    private DataBase dataBase;
    AppleGameController appleGameController;
    AppleGameConfig gameConfigSC;
    void Start()
    {
        appleGameController = GetComponent<AppleGameController>();
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        dataBase = GetComponent<DataBase>();
        databaseService = GetComponent<FirebaseDatabaseService>();
        StartListenToPatientID();
    }


    private void StartListenToPatientID()
    {
        if (string.IsNullOrEmpty(dataBase.deviceID))
        {
            Debug.LogError("DataBase DeviceID NULL!");
            return;
        }

        patientIDRef = dbRef.Child("devices").Child(dataBase.deviceID).Child("patientID");
        patientIDRef.ValueChanged += OnPatientIDChanged;

        Debug.Log("PatientID dinlenmeye baþlandý: " + patientIDRef.ToString());
    }
    private void OnPatientIDChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("Firebase error: " + e.DatabaseError.Message);
            return;
        }

        if (!e.Snapshot.Exists || e.Snapshot.Value == null)
        {
            Debug.LogWarning("PatientID alaný boþ.");
            return;
        }

        string newPatientID = e.Snapshot.Value.ToString();
        Debug.Log($"Yeni PatientID: {newPatientID}");

        dataBase.patientID = newPatientID;
        dataBase.device.patientID = newPatientID;


        StartCoroutine(LoadPatientWait(newPatientID));

    }
    
    IEnumerator LoadPatientWait(string newPatientID)
    {
        yield return new WaitForSeconds(1f);
        databaseService.LoadPatientAsync(newPatientID).ContinueWith(task =>
        {
            if (task.IsFaulted || task.Result == null)
            {
                Debug.LogWarning("Hasta bulunamadý.");
                return;
            }

            dataBase.patient = task.Result;
            Debug.Log("Hasta yüklendi: " + dataBase.patient.name);

            //ListenToGameType();
            ListenToGameConfig();
        });
    }
    private void ListenToGameType()
    {
        if (patientSceneRef != null)
            patientSceneRef.ValueChanged -= OnPatientSceneChanged;

        string sessionPath = $"{dataBase.patientID}_session_{dataBase.patient.sessionCount}";
        patientSceneRef = dbRef.Child("sessions").Child(sessionPath).Child("gameType");
        patientSceneRef.ValueChanged += OnPatientSceneChanged;

        Debug.Log("gameType dinleniyor: " + patientSceneRef.ToString());
    }
    private void OnPatientSceneChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("Firebase error (gameType): " + e.DatabaseError.Message);
            return;
        }

        if (!e.Snapshot.Exists || e.Snapshot.Value == null)
        {
            Debug.LogWarning("gameType boþ veya silinmiþ.");
            return;
        }

        string sceneName = e.Snapshot.Value.ToString();
        Debug.Log("Yeni gameType deðeri: " + sceneName);

        //ListenToMinMaxCalibre();

        switch (sceneName)
        {
            case "mainMenu":
                SceneManager.LoadScene("MainMenu");
                break;
            case "appleGame":
                SceneManager.LoadScene("DebugScene");
                break;
            case "fingerDance":
                SceneManager.LoadScene("Opera");
                break;
            default:
                Debug.LogWarning("Bilinmeyen sahne: " + sceneName);
                break;
        }
    }
    private void ListenToGameConfig()
    {
        // Eðer daha önce bir dinleyici varsa, onu kaldýr.
        if (gameConfigRef != null)
        {
            gameConfigRef.ValueChanged -= OnGameConfigChanged;
        }

        // Dinlenecek yolu oluþtur: gameConfigs/config_{UID}_session_{sessionCount}
        string path = $"gameConfigs/config_{dataBase.patientID}_session_{dataBase.patient.sessionCount}";
        gameConfigRef = dbRef.Child(path);

        // Deðiþiklikleri dinlemek için event'i ekle
        gameConfigRef.ValueChanged += OnGameConfigChanged;

        Debug.Log("GameConfig dinlenmeye baþlandý: " + gameConfigRef.ToString());
    }

    // <-- YENÝ METOT -->
    /// <summary>
    /// GameConfig verisi deðiþtiðinde tetiklenir.
    /// </summary>
    private void OnGameConfigChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("Firebase error (gameConfig): " + e.DatabaseError.Message);
            return;
        }

        if (!e.Snapshot.Exists)
        {
            Debug.LogWarning("gameConfig verisi bu seans için bulunamadý veya silindi.");
            dataBase.appleGameConfig = null; // Veri yoksa lokaldeki veriyi de temizle
            return;
        }

        // Gelen veriyi GameConfig nesnesine dönüþtür
        AppleGameConfig config = databaseService.CreateAppleGameConfigFromSnapshot(e.Snapshot);

        if (config.status == AppleGameStatus.playing)
        {
            if (dataBase.appleGameConfig.status == AppleGameStatus.playing)
            {
                Debug.LogWarning("The Game Is Already Playing. Please End The Game Before Start !!!");
            }
            else
            {
                Debug.Log("Game Starting...");
                appleGameController.StartGame(config.gameMode, config.level);
            }
        }
        if (config.status == AppleGameStatus.finish)
        {
            Debug.Log("Game Finishing...");
            appleGameController.Finish();
        }

        // Genel veritabaný nesnesini güncelle
        dataBase.appleGameConfig = config;



        Debug.Log($"[GameConfig Güncellendi] GameMode: {config.gameMode}, Level: {config.level})");
    }
}
