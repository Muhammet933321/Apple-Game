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
            Debug.Log("Device baþarýyla eklendi!");
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
                Debug.LogWarning("Hasta bulunamadý: " + patientID);
                return null;
            }

            string json = snapshot.GetRawJsonValue();
            Patient patient = JsonUtility.FromJson<Patient>(json);

            Debug.Log("Hasta yüklendi: " + patient.name);

            return patient;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Hasta yüklenirken hata oluþtu: " + e.Message);
            return null;
        }
    }

    // ---------- GameConfig ----------
    public AppleGameConfig CreateAppleGameConfigFromSnapshot(DataSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
        {
            Debug.LogWarning("AppleGameConfig için veri bulunamadý.");
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
            config.level = 1; // Varsayýlan deðer
            Debug.LogWarning("Firebase'de 'level' alaný bulunamadý. Varsayýlan olarak 1 ayarlandý.");
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
                config.gameMode = AppleGameMode.Reach; // Varsayýlan deðer
                Debug.LogError($"Firebase'den gelen '{gameModeString}' deðeri geçerli bir AppleGameMode deðil! Varsayýlan olarak 'Reach' ayarlandý.");
            }
        }
        else
        {
            config.gameMode = AppleGameMode.Reach; // Varsayýlan deðer
            Debug.LogWarning("Firebase'de 'gameMode' alaný bulunamadý. Varsayýlan olarak 'Reach' ayarlandý.");
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
                config.status = AppleGameStatus.idle; // Varsayýlan deðer
                Debug.LogError($"Firebase'den gelen '{statusString}' deðeri geçerli bir AppleGameStatus deðil! Varsayýlan olarak 'idle' ayarlandý.");
            }
        }
        else
        {
            config.status = AppleGameStatus.idle; // Varsayýlan deðer
            Debug.LogWarning("Firebase'de 'status' alaný bulunamadý. Varsayýlan olarak 'idle' ayarlandý.");
        }

        // --- APPLE DIRECTION AYARI (YENÝ EKLENDÝ) ---
        var appleDirectionSnapshot = snapshot.Child("appleDirection");
        if (appleDirectionSnapshot.Exists && appleDirectionSnapshot.HasChildren)
        {
            // Listeyi her seferinde temizleyip yeniden doldurmak daha güvenlidir.
            config.appleDirections.Clear();

            foreach (var directionNode in appleDirectionSnapshot.Children)
            {
                try
                {
                    // x, y, z deðerlerini float olarak oku
                    float.TryParse(directionNode.Child("x").Value?.ToString(), out float x);
                    float.TryParse(directionNode.Child("y").Value?.ToString(), out float y);
                    float.TryParse(directionNode.Child("z").Value?.ToString(), out float z);

                    // Yeni Vector3 oluþtur ve listeye ekle
                    config.appleDirections.Add(new Vector3(x, y, z));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"appleDirection okunurken bir hata oluþtu: {ex.Message}");
                }
            }
            Debug.Log($"{config.appleDirections.Count} adet appleDirection yüklendi.");
        }
        else
        {
            Debug.LogWarning("Firebase'de 'appleDirection' alaný bulunamadý veya içi boþ.");
        }

        Debug.Log($"AppleGameConfig oluþturuldu -> Seviye: {config.level}, Oyun Modu: {config.gameMode}, Durum: {config.status}, Yön Sayýsý: {config.appleDirections.Count}");

        return config;
    }

    // ---------- Game Results ----------

    /// <summary>
    /// Bu, iþlemin sonucunu bekleyen ana async metottur. Hata yönetimi burada yapýlýr.
    /// </summary>
    public async Task SaveProgressHistory(List<ProgressLog.LevelEntry> history)
    {
        if (string.IsNullOrEmpty(database.patientID) || database.patient == null)
        {
            // Hata fýrlatarak çaðýran metoda sorunu bildirebiliriz.
            throw new System.InvalidOperationException("Hasta bilgileri eksik, baþarý kaydý yapýlamýyor!");
        }

        if (history == null || history.Count == 0)
        {
            Debug.LogWarning("Kaydedilecek bir baþarý geçmiþi bulunamadý.");
            return;
        }

        string path = $"gameResults/{database.patientID}_results_{database.patient.sessionCount}";
        string json = JsonUtility.ToJson(new ProgressHistoryWrapper { history = history });

        // Asýl veritabaný yazma iþlemi. Hata olursa exception fýrlatýr.
        await dbRef.Child(path).SetRawJsonValueAsync(json);
    }

    // --- YENÝ FONKSÝYON ---
    /// <summary>
    /// "Ateþle ve Unut" (Fire-and-Forget) senaryolarý için güvenli bir sarmalayýcý.
    /// Bu metot, çaðýran yerin bekleme yapmasýný gerektirmez ve CS4014 uyarýsýný engeller.
    /// </summary>
    public async void SaveProgressHistory_FireAndForget(List<ProgressLog.LevelEntry> history)
    {
        try
        {
            // Asýl async Task metodunu çaðýr ve bitmesini bekle.
            await SaveProgressHistory(history);
            Debug.Log("<color=green>Baþarý kayýtlarý arka planda baþarýyla kaydedildi.</color>");
        }
        catch (System.Exception e)
        {
            // async void metotlardaki hatalar uygulamanýzý çökertebilir, bu yüzden
            // burada yakalamak ÇOK ÖNEMLÝDÝR.
            Debug.LogError($"Veritabanýna kayýt sýrasýnda bir hata oluþtu: {e.Message}");
        }
    }

    // JSON yardýmcý sýnýfý
    [System.Serializable]
    private class ProgressHistoryWrapper
    {
        public List<ProgressLog.LevelEntry> history;
    }
}
