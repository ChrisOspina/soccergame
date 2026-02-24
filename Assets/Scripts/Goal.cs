using UnityEngine;
using TMPro;
using System.Collections;

public class Goal : MonoBehaviour
{
    public Player playerRef;
    public string name;
    public TMP_Text goalText;
    public float goalTextDuration = 3f;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    void Start()
    {
        goalText.gameObject.SetActive(false);
    }

    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ball"))
        {
            showGoal();
            if (name.Equals("Goal1"))
            {
                playerRef.IncreaseMyScore();
            }
            else if (name.Equals("Goal2"))
            {
                playerRef.IncreaseOtherScore();
            }
        }
    }

    private void showGoal()
    {
        StopAllCoroutines();
        StartCoroutine(ShowGoalCoroutine());
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