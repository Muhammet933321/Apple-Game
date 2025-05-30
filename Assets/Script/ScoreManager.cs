using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI  scoreText;
    private int score = 0;
    private void Awake()
    {
        Apple.PickedCorrectBasket += IncreaseScore;
        Apple.PickedWrongBasket += DecreaseScore;
    }

    private void IncreaseScore(Apple obj)
    {
        score++;
        UpdateText();
    }
    private void DecreaseScore(Apple obj)
    {
        score--;
        UpdateText();
    }

    private void UpdateText()
    {
        scoreText.text = "Puan: " + score.ToString();
    }
    
}
