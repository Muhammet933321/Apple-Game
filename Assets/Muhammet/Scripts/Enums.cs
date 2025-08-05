using System;
using System.Collections.Generic;
using UnityEngine;

public class Enums : MonoBehaviour
{


}

[System.Serializable]
public class Patient
{
    public string name;
    public int age;
    public string diagnosis;
    public bool isFemale;
    public string note;
    public string customGames;
    public string romID;
    public int sessionCount;

}


[System.Serializable]
public class Device
{
    public string connectionStatus;
    public string deviceName;
    public bool enable;
    public string patientID;

    public Device(string connectionStatus, string deviceName, bool enable, string patientID)
    {
        this.connectionStatus = connectionStatus;
        this.deviceName = deviceName;
        this.enable = enable;
        this.patientID = patientID;
    }
}

[System.Serializable]
public class Session
{
    public string gameResultID;
    public string gameType;
    public bool maxRomClibre;
    public bool minRomClibre;
    public string romID;
}

[System.Serializable]
public class AppleGameResult
{
    public AppleGameResult()
    {
        handLogs = new List<BothHandLog>();
        notes = new List<NoteResult>();
    }
    public string gameType;
    public int score;
    public string sessionID;

    // appleGame özel alanlar

    public List<BothHandLog> handLogs;



    public float successRate;

    // fingerDance özel alanlar
    public int takes;
    public int mistakes;
    public List<NoteResult> notes;
}

[System.Serializable]
public class BothHandLog
{
    public BothHandLog()
    {
        handleLogsLeft = new List<HandLog> ();
        handleLogsRight = new List<HandLog> ();
    }
    public List<HandLog> handleLogsRight;
    public List<HandLog> handleLogsLeft;
}



[System.Serializable]
public class HandLog
{
    public HandLog()
    {
        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    public Vector3 position;
    public Quaternion rotation;
    public bool isGrab;
    public long time;
}


[System.Serializable]
public class AppleResult
{
    public int index;
    public string status;
    public float time;
}

[System.Serializable]
public class NoteResult
{
    public int finger;
    public bool hit;
    public string note;
    public float time;
}

[System.Serializable]
public class Rom
{
    public ArmData arm;
    public List<FingerRom> finger;
}

[System.Serializable]
public class ArmData
{
    public int leftSpace;
    public int rightSpace;
}

[System.Serializable]
public class FingerRom
{
    public float? max;
    public float? min;

    public FingerRom(float? max = null, float? min = null)
    {
        this.max = max;
        this.min = min;
    }
}



[System.Serializable]
public class AppleGameConfig
{
    public AppleGameMode gameMode;
    public AppleGameStatus status;
    public int level;
    public List<Vector3> appleDirections = new List<Vector3>();




}
public enum AppleGameMode
{
    Reach,
    Grip,
    Carry,
    Sort
};
public enum AppleGameStatus
{
    idle,
    playing,
    finish
};

[System.Serializable]
public class MinMax
{
    public int max;
    public int min;
}

