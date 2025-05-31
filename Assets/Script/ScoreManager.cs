using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public Text  scoreText;
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
