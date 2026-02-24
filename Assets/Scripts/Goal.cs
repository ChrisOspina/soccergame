using UnityEngine;

public class Goal : MonoBehaviour
{
    public Player playerRef;
    public string name;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ball"))
        {
            if (name.Equals("Goal1"))
            {
                playerRef.IncreaseMyScore();
            }
            else if (name.Equals("Goal2"))
            {
                playerRef.IncreaseOtherScore();
            }
            //Debug.Log("Hit the goal!");
        }
    }
}
