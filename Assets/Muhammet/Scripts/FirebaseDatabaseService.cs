using UnityEngine;
using Firebase.Database;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
public class FirebaseDatabaseService : MonoBehaviour
{
    private DatabaseReference dbRef;

    private DataBase database;
   
    void Awake()
    {
        database = GetComponent<DataBase>();
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        database.deviceID = deviceId;
        Debug.Log("Device Unique ID: " + deviceId);
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(EnableDevice());
    }


    // ---------- DEVICE ----------

    IEnumerator EnableDevice()
    {
        Device device = new Device("online", "MetaQuest_3_deneme", true, null);

        string deviceId = SystemInfo.deviceUniqueIdentifier;

        var task = AddDevice(deviceId, device);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Device eklenemedi!");
        }
        else
        {
            Debug.Log("Device ba�ar�yla eklendi!");
        }
    }
    public async Task AddDevice(string deviceId, Device device)
    {
        string json = JsonUtility.ToJson(device);
        await dbRef.Child("devices").Child(deviceId).SetRawJsonValueAsync(json);
        Debug.Log($"[Firebase] Device added: {deviceId}");
    }

    // ---------- PATIENT ----------

    public async Task AddPatient(string patientId, Patient patient)
    {
        string json = JsonUtility.ToJson(patient);
        await dbRef.Child("patients").Child(patientId).SetRawJsonValueAsync(json);
        Debug.Log($"[Firebase] Patient added: {patientId}");
    }

    public async Task<Patient> LoadPatientAsync(string patientID)
    {
        if (patientID == null)
        {
            Debug.LogError("Patient ID is NULL");
            return null;
        }
        else
        {
            Debug.Log("patient ID = " + patientID);
        }
        try
        {
            var snapshot = await dbRef.Child("patients").Child(patientID).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning("Hasta bulunamad�: " + patientID);
                return null;
            }

            string json = snapshot.GetRawJsonValue();
            Patient patient = JsonUtility.FromJson<Patient>(json);

            Debug.Log("Hasta y�klendi: " + patient.name);

            return patient;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Hasta y�klenirken hata olu�tu: " + e.Message);
            return null;
        }
    }

    // ---------- GameConfig ----------
    public AppleGameConfig CreateAppleGameConfigFromSnapshot(DataSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
        {
            Debug.LogWarning("AppleGameConfig i�in veri bulunamad�.");
            return null;
        }

        AppleGameConfig config = new AppleGameConfig();

        // --- LEVEL AYARI ---
        if (snapshot.Child("level").Exists)
        {
            int.TryParse(snapshot.Child("level").Value.ToString(), out config.level);
        }
        else
        {
            config.level = 1; // Varsay�lan de�er
            Debug.LogWarning("Firebase'de 'level' alan� bulunamad�. Varsay�lan olarak 1 ayarland�.");
        }

        // --- GAME MODE AYARI ---
        if (snapshot.Child("gameMode").Exists)
        {
            string gameModeString = snapshot.Child("gameMode").Value.ToString();
            if (Enum.TryParse<AppleGameMode>(gameModeString, true, out AppleGameMode mode))
            {
                config.gameMode = mode;
            }
            else
            {
                config.gameMode = AppleGameMode.Reach; // Varsay�lan de�er
                Debug.LogError($"Firebase'den gelen '{gameModeString}' de�eri ge�erli bir AppleGameMode de�il! Varsay�lan olarak 'Reach' ayarland�.");
            }
        }
        else
        {
            config.gameMode = AppleGameMode.Reach; // Varsay�lan de�er
            Debug.LogWarning("Firebase'de 'gameMode' alan� bulunamad�. Varsay�lan olarak 'Reach' ayarland�.");
        }

        // --- STATUS AYARI ---
        if (snapshot.Child("status").Exists)
        {
            string statusString = snapshot.Child("status").Value.ToString();
            if (Enum.TryParse<AppleGameStatus>(statusString, true, out AppleGameStatus status))
            {
                config.status = status;
            }
            else
            {
                config.status = AppleGameStatus.idle; // Varsay�lan de�er
                Debug.LogError($"Firebase'den gelen '{statusString}' de�eri ge�erli bir AppleGameStatus de�il! Varsay�lan olarak 'idle' ayarland�.");
            }
        }
        else
        {
            config.status = AppleGameStatus.idle; // Varsay�lan de�er
            Debug.LogWarning("Firebase'de 'status' alan� bulunamad�. Varsay�lan olarak 'idle' ayarland�.");
        }

        // --- APPLE DIRECTION AYARI (YEN� EKLEND�) ---
        var appleDirectionSnapshot = snapshot.Child("appleDirection");
        if (appleDirectionSnapshot.Exists && appleDirectionSnapshot.HasChildren)
        {
            // Listeyi her seferinde temizleyip yeniden doldurmak daha g�venlidir.
            config.appleDirections.Clear();

            foreach (var directionNode in appleDirectionSnapshot.Children)
            {
                try
                {
                    // x, y, z de�erlerini float olarak oku
                    float.TryParse(directionNode.Child("x").Value?.ToString(), out float x);
                    float.TryParse(directionNode.Child("y").Value?.ToString(), out float y);
                    float.TryParse(directionNode.Child("z").Value?.ToString(), out float z);

                    // Yeni Vector3 olu�tur ve listeye ekle
                    config.appleDirections.Add(new Vector3(x, y, z));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"appleDirection okunurken bir hata olu�tu: {ex.Message}");
                }
            }
            Debug.Log($"{config.appleDirections.Count} adet appleDirection y�klendi.");
        }
        else
        {
            Debug.LogWarning("Firebase'de 'appleDirection' alan� bulunamad� veya i�i bo�.");
        }

        Debug.Log($"AppleGameConfig olu�turuldu -> Seviye: {config.level}, Oyun Modu: {config.gameMode}, Durum: {config.status}, Y�n Say�s�: {config.appleDirections.Count}");

        return config;
    }

    // ---------- Game Results ----------

    /// <summary>
    /// Bu, i�lemin sonucunu bekleyen ana async metottur. Hata y�netimi burada yap�l�r.
    /// </summary>
    public async Task SaveProgressHistory(List<ProgressLog.LevelEntry> history)
    {
        if (string.IsNullOrEmpty(database.patientID) || database.patient == null)
        {
            // Hata f�rlatarak �a��ran metoda sorunu bildirebiliriz.
            throw new System.InvalidOperationException("Hasta bilgileri eksik, ba�ar� kayd� yap�lam�yor!");
        }

        if (history == null || history.Count == 0)
        {
            Debug.LogWarning("Kaydedilecek bir ba�ar� ge�mi�i bulunamad�.");
            return;
        }

        string path = $"gameResults/{database.patientID}_results_{database.patient.sessionCount}";
        string json = JsonUtility.ToJson(new ProgressHistoryWrapper { history = history });

        // As�l veritaban� yazma i�lemi. Hata olursa exception f�rlat�r.
        await dbRef.Child(path).SetRawJsonValueAsync(json);
    }

    // --- YEN� FONKS�YON ---
    /// <summary>
    /// "Ate�le ve Unut" (Fire-and-Forget) senaryolar� i�in g�venli bir sarmalay�c�.
    /// Bu metot, �a��ran yerin bekleme yapmas�n� gerektirmez ve CS4014 uyar�s�n� engeller.
    /// </summary>
    public async void SaveProgressHistory_FireAndForget(List<ProgressLog.LevelEntry> history)
    {
        try
        {
            // As�l async Task metodunu �a��r ve bitmesini bekle.
            await SaveProgressHistory(history);
            Debug.Log("<color=green>Ba�ar� kay�tlar� arka planda ba�ar�yla kaydedildi.</color>");
        }
        catch (System.Exception e)
        {
            // async void metotlardaki hatalar uygulaman�z� ��kertebilir, bu y�zden
            // burada yakalamak �OK �NEML�D�R.
            Debug.LogError($"Veritaban�na kay�t s�ras�nda bir hata olu�tu: {e.Message}");
        }
    }

    // JSON yard�mc� s�n�f�
    [System.Serializable]
    private class ProgressHistoryWrapper
    {
        public List<ProgressLog.LevelEntry> history;
    }
}
