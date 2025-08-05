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

        Debug.Log("PatientID dinlenmeye ba�land�: " + patientIDRef.ToString());
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
            Debug.LogWarning("PatientID alan� bo�.");
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
                Debug.LogWarning("Hasta bulunamad�.");
                return;
            }

            dataBase.patient = task.Result;
            Debug.Log("Hasta y�klendi: " + dataBase.patient.name);

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
            Debug.LogWarning("gameType bo� veya silinmi�.");
            return;
        }

        string sceneName = e.Snapshot.Value.ToString();
        Debug.Log("Yeni gameType de�eri: " + sceneName);

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
        // E�er daha �nce bir dinleyici varsa, onu kald�r.
        if (gameConfigRef != null)
        {
            gameConfigRef.ValueChanged -= OnGameConfigChanged;
        }

        // Dinlenecek yolu olu�tur: gameConfigs/config_{UID}_session_{sessionCount}
        string path = $"gameConfigs/config_{dataBase.patientID}_session_{dataBase.patient.sessionCount}";
        gameConfigRef = dbRef.Child(path);

        // De�i�iklikleri dinlemek i�in event'i ekle
        gameConfigRef.ValueChanged += OnGameConfigChanged;

        Debug.Log("GameConfig dinlenmeye ba�land�: " + gameConfigRef.ToString());
    }

    // <-- YEN� METOT -->
    /// <summary>
    /// GameConfig verisi de�i�ti�inde tetiklenir.
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
            Debug.LogWarning("gameConfig verisi bu seans i�in bulunamad� veya silindi.");
            dataBase.appleGameConfig = null; // Veri yoksa lokaldeki veriyi de temizle
            return;
        }

        // Gelen veriyi GameConfig nesnesine d�n��t�r
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

        // Genel veritaban� nesnesini g�ncelle
        dataBase.appleGameConfig = config;



        Debug.Log($"[GameConfig G�ncellendi] GameMode: {config.gameMode}, Level: {config.level})");
    }
}
