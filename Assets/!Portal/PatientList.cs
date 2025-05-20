using UnityEngine;

public class PatientList : MonoBehaviour
{
    public PatientUI patientPrefab;
    public Transform contentPanel;

    private void Start()
    {
        // Example: Create 5 patients
        for (int i = 0; i < 5; i++)
        {
            CreatePatient("Patient " + (i + 1));
        }
    }

    private void CreatePatient(string patientName)
    {
        PatientUI newPatient = Instantiate(patientPrefab, contentPanel);
        newPatient.patientNameText.text = patientName;
    }
}
