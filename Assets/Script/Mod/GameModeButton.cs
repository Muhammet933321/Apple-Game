using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeButton : MonoBehaviour
{
    public GameMode gameMode; 
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Controller")
        {
            switch (gameMode)
            {
                case GameMode.Measurement:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
                case GameMode.WrongBasket:
                    GameModeManager.Instance.StartWrongBasket();
                    break;
                case GameMode.DropOnly:
                    GameModeManager.Instance.StartDropOnly();
                    break;
                case GameMode.Unreachable:
                    GameModeManager.Instance.StartUnreachable();
                    break;
                case GameMode.Static:
                    GameModeManager.Instance.StartStatic();
                    break;
            }
            GameModeManager.Instance.gameModeUI.SetActive(false);
        }
    }
}
