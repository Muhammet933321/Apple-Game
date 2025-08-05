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
            Debug.LogError("Sahnede FirebaseDatabaseService bulunamadý!");
        }
    }

    /// <summary>
    /// Oyunu belirtilen mod ve seviye ile baþlatýr.
    /// Bu fonksiyon FirebaseListenService tarafýndan çaðrýlacak.
    /// </summary>
    /// <param name="mode">Baþlatýlacak oyun modu.</param>
    /// <param name="level">Baþlatýlacak seviye.</param>
    public void StartGame(AppleGameMode mode, int level)
    {
        dataBase.appleGameResult.handLogs.Add(new BothHandLog());
        // 1. Gelen moda göre doðru terapi modunu seçmek için switch-case yapýsý
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
                Debug.LogError($"Bilinmeyen oyun modu: {mode}. Oyun baþlatýlamadý.");
                return; // Bilinmeyen bir mod ise iþlemi sonlandýr.
        }

        Curr.StartLevelAt(level);
        Debug.Log($"<color=green>Oyun Baþlatýldý!</color> Mod: <b>{mode}</b>, Seviye: <b>{level}</b>");

    }

    /// <summary>
    /// Oyunu manuel olarak bitirir ve baþarý kayýtlarýný Firebase'e kaydeder.
    /// </summary>
    /// <summary>
    /// Oyunu manuel olarak bitirir ve baþarý kayýtlarýný Firebase'e kaydeder.
    /// Bu metot artýk async olmak zorunda deðil.
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
        Debug.Log("Oyun Bitti! Skor hesaplanýyor ve veriler kaydediliyor...");

        if (firebaseDataBaseService != null && progressLog != null)
        {
            // Kopyayý al, güncelle, geri ata
            int lastIndex = progressLog.history.Count - 1;
            var entry = progressLog.history[lastIndex];
            var handLogs = dataBase.appleGameResult.handLogs;
            entry.handLogs = handLogs[handLogs.Count - 1];
            progressLog.history[lastIndex] = entry;

            firebaseDataBaseService.SaveProgressHistory_FireAndForget(progressLog.history);

        }
        else
        {
            Debug.LogError("Firebase servisi veya ProgressLog atanmamýþ! Kayýt yapýlamadý.");
        }
    }


}