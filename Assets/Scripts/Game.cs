using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class Game : MonoBehaviour
{
    public static Game Instance;

    public Ball ball;
    public Player player;
    public COMPlayer comPlayer;

    [Header("Match Settings")]
    public float matchDuration = 180f;
    public int goalLimit = 3;

    [Header("Match UI")]
    public TMP_Text timerText;
    public TMP_Text resultText;

    private float timeRemaining;
    private bool matchOver;
    private int latestPlayerScore;
    private int latestComScore;

    public bool IsMatchOver => matchOver;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        timeRemaining = matchDuration;
        if (resultText != null)
            resultText.gameObject.SetActive(false);
        UpdateTimerUI();
    }

    void Update()
    {
        if (matchOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerUI();
            EndMatch();
        }
        else
        {
            UpdateTimerUI();
        }
    }

    public void ReportScore(int playerScore, int comScore)
    {
        if (matchOver) return;
        latestPlayerScore = playerScore;
        latestComScore = comScore;
        if (playerScore >= goalLimit || comScore >= goalLimit)
            EndMatch();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }

    void EndMatch()
    {
        matchOver = true;
        if (resultText == null) return;
        resultText.gameObject.SetActive(true);
        string outcome;
        if (latestPlayerScore > latestComScore)
            outcome = "You Win!";
        else if (latestComScore > latestPlayerScore)
            outcome = "COM Wins!";
        else
            outcome = "Draw!";
        resultText.text = outcome + "\n<size=60%>Press R to restart</size>";
    }

    public void ResetAfterGoal()
    {
        ball.Respawn();
        if (player != null) player.ResetPosition();
        if (comPlayer != null) comPlayer.ResetPosition();
    }

}
