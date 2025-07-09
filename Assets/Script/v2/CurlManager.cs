using UnityEngine;

public class CurlManager : MonoBehaviour
{
    public FingerCurlLimiter leftCurlLimiter;
    public FingerCurlLimiter rightCurlLimiter;

    public float curlValue = 0.5f;
    
    public void OnCurlValueChanged(float value)
    {
        curlValue = value;
        UpdateCurlLimits();
    }
    
    private void UpdateCurlLimits()
    {
        if (leftCurlLimiter != null)
        {
            leftCurlLimiter.SetCurlValue(curlValue);
        }

        if (rightCurlLimiter != null)
        {
            rightCurlLimiter.SetCurlValue(curlValue);
        }
    }
}
