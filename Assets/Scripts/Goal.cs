using UnityEngine;
using TMPro;
using System.Collections;

public class Goal : MonoBehaviour
{
    public TeamController scoringTeam;
    public TMP_Text goalText;
    public float goalTextDuration = 3f;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    private Coroutine _goalTextCoroutine;
    private Coroutine _respawnCoroutine;

    public bool IsShowingGoalText => goalText != null && goalText.gameObject.activeSelf;

    void Start()
    {
        if (goalText != null) goalText.gameObject.SetActive(false);
    }

    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ball"))
        {
            showGoal();
            scoringTeam?.AddGoal();
            LogGoal(other.GetComponentInParent<Ball>());

            if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = StartCoroutine(RespawnBallAfterDelay(1.5f));
        }
    }

    private void LogGoal(Ball ball)
    {
        Player scorer = ball != null ? ball.LastTouchedBy : null;
        string score = Game.Instance != null ? Game.Instance.ScoreLine : "";
        if (scorer == null)
            Debug.Log($"GOAL for {scoringTeam?.side}! (no player touched it) {score}");
        else if (scoringTeam != null && scorer.team != null && scorer.team != scoringTeam)
            Debug.Log($"OWN GOAL by {scorer.DebugName}! Point to {scoringTeam.side}. {score}");
        else
            Debug.Log($"GOAL by {scorer.DebugName}! {score}");
    }

    private IEnumerator RespawnBallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Game.Instance?.ResetAfterGoal();
    }

    private void showGoal()
    {
        if (_goalTextCoroutine != null) StopCoroutine(_goalTextCoroutine);
        _goalTextCoroutine = StartCoroutine(ShowGoalCoroutine());
    }

    private IEnumerator ShowGoalCoroutine()
    {
        goalText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < goalTextDuration)
        {
            float t = elapsed / goalTextDuration;

            // Grow throughout lifetime
            float scale = Mathf.Lerp(minScale, maxScale, t);
            goalText.transform.localScale = Vector3.one * scale;

            // Fade out in the second half
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
            goalText.color = new Color(goalText.color.r, goalText.color.g, goalText.color.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        goalText.gameObject.SetActive(false);

        // Reset for next time
        goalText.transform.localScale = Vector3.one * minScale;
        goalText.color = new Color(goalText.color.r, goalText.color.g, goalText.color.b, 1f);
    }
}