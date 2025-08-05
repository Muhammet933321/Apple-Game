using Unity.VisualScripting;
using UnityEngine;

public class AppleGameController : MonoBehaviour
{

    private DataBase dataBase;
    public ProgressLog progressLog;
    ActivityManager Curr => GameModeController.Instance.Current;

    private FirebaseDatabaseService firebaseDataBaseService;

    private void Start()
    {
        dataBase = GetComponent<DataBase>();
        // Sahnede Firebase servisini bul
        firebaseDataBaseService = GetComponent<FirebaseDatabaseService>();

        if (firebaseDataBaseService == null)
        {
            Debug.LogError("Sahnede FirebaseDatabaseService bulunamad�!");
        }
    }

    /// <summary>
    /// Oyunu belirtilen mod ve seviye ile ba�lat�r.
    /// Bu fonksiyon FirebaseListenService taraf�ndan �a�r�lacak.
    /// </summary>
    /// <param name="mode">Ba�lat�lacak oyun modu.</param>
    /// <param name="level">Ba�lat�lacak seviye.</param>
    public void StartGame(AppleGameMode mode, int level)
    {
        dataBase.appleGameResult.handLogs.Add(new BothHandLog());
        // 1. Gelen moda g�re do�ru terapi modunu se�mek i�in switch-case yap�s�
        switch (mode)
        {
            case AppleGameMode.Reach:
                GameModeController.Instance.SwitchMode(TherapyMode.Reach);
                break;

            case AppleGameMode.Grip:
                GameModeController.Instance.SwitchMode(TherapyMode.Grip);
                break;

            case AppleGameMode.Carry:
                GameModeController.Instance.SwitchMode(TherapyMode.Carry);
                break;

            case AppleGameMode.Sort:
                GameModeController.Instance.SwitchMode(TherapyMode.Sort);
                break;

            default:
                Debug.LogError($"Bilinmeyen oyun modu: {mode}. Oyun ba�lat�lamad�.");
                return; // Bilinmeyen bir mod ise i�lemi sonland�r.
        }

        Curr.StartLevelAt(level);
        Debug.Log($"<color=green>Oyun Ba�lat�ld�!</color> Mod: <b>{mode}</b>, Seviye: <b>{level}</b>");

    }

    /// <summary>
    /// Oyunu manuel olarak bitirir ve ba�ar� kay�tlar�n� Firebase'e kaydeder.
    /// </summary>
    /// <summary>
    /// Oyunu manuel olarak bitirir ve ba�ar� kay�tlar�n� Firebase'e kaydeder.
    /// Bu metot art�k async olmak zorunda de�il.
    /// </summary>
    public void Finish()
    {
        if (dataBase.appleGameConfig.status != AppleGameStatus.playing)
        {
            Debug.LogWarning("Bitirilecek aktif bir oyun yok.");
            return;
        }

        dataBase.appleGameConfig.status = AppleGameStatus.finish;
        Curr?.Finish();
        Debug.Log("Oyun Bitti! Skor hesaplan�yor ve veriler kaydediliyor...");

        if (firebaseDataBaseService != null && progressLog != null)
        {
            // Kopyay� al, g�ncelle, geri ata
            int lastIndex = progressLog.history.Count - 1;
            var entry = progressLog.history[lastIndex];
            var handLogs = dataBase.appleGameResult.handLogs;
            entry.handLogs = handLogs[handLogs.Count - 1];
            progressLog.history[lastIndex] = entry;

            firebaseDataBaseService.SaveProgressHistory_FireAndForget(progressLog.history);

        }
        else
        {
            Debug.LogError("Firebase servisi veya ProgressLog atanmam��! Kay�t yap�lamad�.");
        }
    }


}