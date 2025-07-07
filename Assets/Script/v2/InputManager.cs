using UnityEngine;

/// Tek sorumluluğu klavye girişlerini yakalayıp
/// ReachActivityManager üzerinde ilgili işlevleri tetiklemek.
public class InputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReachActivityManager.Instance?.OnRestart();     
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            ReachActivityManager.Instance?.OnContinue();    
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            ReachActivityManager.Instance?.OnFinish();        
        }
    }
}