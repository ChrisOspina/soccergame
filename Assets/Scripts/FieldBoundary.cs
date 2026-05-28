using UnityEngine;

public class FieldBoundary : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
            Game.Instance.ball.Respawn();
    }
}
