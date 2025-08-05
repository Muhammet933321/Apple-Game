using Firebase.Database;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPositionLoger : MonoBehaviour
{
    public ProgressLog progressLog;
    DataBase dataBase;
    public GameObject rightHandObj;
    public GameObject leftHandObj;
    float startTime;
    bool isPlaying = false;
    Coroutine loggingCoroutine;

    private void Start()
    {
        dataBase = GetComponent<DataBase>();
    }

    void Update()
    {
        if (dataBase.appleGameConfig.status == AppleGameStatus.playing)
        {
            if (!isPlaying)
            {
                isPlaying = true;
                startTime = Time.time;

                if (loggingCoroutine == null)
                {
                    loggingCoroutine = StartCoroutine(LogHandsRepeatedly());
                }
            }
        }
        else
        {
            isPlaying = false;

            if (loggingCoroutine != null)
            {
                StopCoroutine(loggingCoroutine);

                loggingCoroutine = null;
            }
        }
    }

    private IEnumerator LogHandsRepeatedly()
    {
        // 3 saniyelik gecikme
        yield return new WaitForSeconds(3f);

        while (isPlaying)
        {
            LogHand();
            yield return new WaitForSeconds(0.1f); // Her 0.1 saniyede bir
        }
    }

    private void LogHand()
    {
        if (rightHandObj == null || leftHandObj == null || dataBase.appleGameResult.handLogs == null || dataBase.appleGameResult.handLogs.Count == 0)
        {
            Debug.LogWarning("LogHand: Eksik veri veya bos handLogs listesi.");
            return;
        }

        int lastIndex = dataBase.appleGameResult.handLogs.Count - 1;

        // Sað el log
        HandLog rightLog = new HandLog
        {
            position = rightHandObj.transform.position,
            rotation = rightHandObj.transform.rotation,
            isGrab = CheckGrab(rightHandObj)
        };

        dataBase.appleGameResult.handLogs[lastIndex].handleLogsRight.Add(rightLog);

        // Sol el log
        HandLog leftLog = new HandLog
        {
            position = leftHandObj.transform.position,
            rotation = leftHandObj.transform.rotation,
            isGrab = CheckGrab(leftHandObj)
        };

        dataBase.appleGameResult.handLogs[lastIndex].handleLogsLeft.Add(leftLog);
    }


    private bool CheckGrab(GameObject hand)
    {
        return false; // geçici olarak
    }
}
