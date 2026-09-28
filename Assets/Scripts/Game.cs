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
    [Tooltip("Seconds to wait after the goal text disappears before showing the result")]
    public float resultDelayAfterGoal = 2f;

    [Header("Pause")]
    public GameObject pausePanel;
    [Tooltip("Name of the AudioMixer group whose sources keep playing while paused")]
    public string ambientGroupName = "Ambiance";

    private float timeRemaining;
    private bool matchOver;
    private bool resultShown;
    private bool isPaused;
    private readonly List<PlayerInput> pausedInputs = new List<PlayerInput>();
    private int latestPlayerScore;
    private int latestComScore;

    public bool IsMatchOver => matchOver;
    public bool IsPaused => isPaused;
    public string ScoreLine => $"Score: Player {(playerTeam != null ? playerTeam.Score : 0)} - {(comTeam != null ? comTeam.Score : 0)} COM";

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
        if (pausePanel != null)
            pausePanel.SetActive(false);
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
        // Distinct names so debug logs can tell the teammates apart (they'd all be "<template>(Clone)").
        clone.name = $"{template.name} {(lateralOffset < 0f ? "Left" : "Right")}";

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
        // AudioListener.volume survives scene reloads, so mute stays on after a restart.
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;

        if (!matchOver && Keyboard.current != null &&
            (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            SetPaused(!isPaused);

        if (isPaused) return;

        if (matchOver)
        {
            // Restart only once the "Press R to restart" prompt is actually on screen.
            if (resultShown && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
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
        StartCoroutine(ShowResultWhenGoalTextClears());
    }

    // A match-winning goal is still animating its goal text, so hold the result until it's gone.
    IEnumerator ShowResultWhenGoalTextClears()
    {
        Goal[] goals = FindObjectsByType<Goal>(FindObjectsSortMode.None);
        bool waitedForGoalText = false;
        while (System.Array.Exists(goals, g => g.IsShowingGoalText))
        {
            waitedForGoalText = true;
            yield return null;
        }
        if (waitedForGoalText)
            yield return new WaitForSeconds(resultDelayAfterGoal);

        ShowResult();
    }

    void ShowResult()
    {
        resultShown = true;
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

    void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel != null)
            pausePanel.SetActive(paused);

        // Pauses every AudioSource except the ambient ones, which opt out via ignoreListenerPause.
        if (paused)
        {
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                var group = source.outputAudioMixerGroup;
                source.ignoreListenerPause = group != null && group.name == ambientGroupName;
            }
        }
        AudioListener.pause = paused;

        if (paused)
        {
            // Stop reading gameplay input so the camera/player can't move while frozen.
            pausedInputs.Clear();
            foreach (PlayerInput playerInput in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            {
                if (!playerInput.enabled || !playerInput.inputIsActive) continue;
                playerInput.DeactivateInput();
                pausedInputs.Add(playerInput);
                StarterAssetsInputs input = playerInput.GetComponent<StarterAssetsInputs>();
                if (input != null)
                {
                    input.move = Vector2.zero;
                    input.look = Vector2.zero;
                    input.jump = input.sprint = input.shoot = input.pass = input.switchPlayer = false;
                }
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            foreach (PlayerInput playerInput in pausedInputs)
                if (playerInput != null) playerInput.ActivateInput();
            pausedInputs.Clear();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnDestroy()
    {
        // Don't leak a frozen timescale / paused audio into a reloaded scene.
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void ResetAfterGoal()
    {
        ball.Respawn();
        playerTeam?.ResetPositions();
        comTeam?.ResetPositions();
    }

}
