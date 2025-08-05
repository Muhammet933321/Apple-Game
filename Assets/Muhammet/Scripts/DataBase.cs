using UnityEngine;

public class DataBase : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Device Instance")]
    public string deviceID;
    public Device device;

    [Header("Patient Instance")]
    public string patientID;
    public Patient patient;

    [Header("Session Instance")]
    public Session session;

    [Header("GameResultForFinger Instance")]
    public AppleGameResult appleGameResult;

    [Header("ROM Instance")]
    public Rom rom;

    [Header("GameConfig")]
    public AppleGameConfig appleGameConfig;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
