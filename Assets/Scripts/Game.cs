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
    public TeamController playerTeam;
    public TeamController comTeam;

    [Header("Teammate Spawning")]
    [Tooltip("The existing scene character each side's 2 extra teammates are cloned from")]
    public GameObject playerTemplate;
    public GameObject comTemplate;
    public float teammateLateralOffset = 6f;

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
        SpawnTeammates();

        timeRemaining = matchDuration;
        if (resultText != null)
            resultText.gameObject.SetActive(false);
        UpdateTimerUI();
    }

    void SpawnTeammates()
    {
        SpawnClone(playerTemplate, -teammateLateralOffset);
        SpawnClone(playerTemplate, teammateLateralOffset);
        SpawnClone(comTemplate, -teammateLateralOffset);
        SpawnClone(comTemplate, teammateLateralOffset);
    }

    void SpawnClone(GameObject template, float lateralOffset)
    {
        if (template == null) return;

        Vector3 spawnPos = template.transform.position + template.transform.right * lateralOffset;
        GameObject clone = Instantiate(template, spawnPos, template.transform.rotation);

        AITeammate ai = clone.GetComponent<AITeammate>();
        if (ai != null)
        {
            ai.formationLateral = lateralOffset;
            ai.enabled = true;
        }

        // A clone of the human template inherits whichever character is currently
        // human-controlled; force every freshly spawned teammate into AI mode so we
        // don't end up with multiple PlayerInput components fighting over the keyboard.
        ThirdPersonController tpc = clone.GetComponent<ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;

#if ENABLE_INPUT_SYSTEM
        PlayerInput playerInput = clone.GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
#endif

        HumanPlayer human = clone.GetComponent<HumanPlayer>();
        if (human != null) human.enabled = false;
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

    public void OnGoalScored()
    {
        if (matchOver) return;
        latestPlayerScore = playerTeam != null ? playerTeam.Score : 0;
        latestComScore = comTeam != null ? comTeam.Score : 0;
        if (latestPlayerScore >= goalLimit || latestComScore >= goalLimit)
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
        playerTeam?.ResetPositions();
        comTeam?.ResetPositions();
    }

}
