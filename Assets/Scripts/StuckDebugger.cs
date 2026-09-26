using StarterAssets;
using UnityEngine;

// TEMPORARY diagnostic: logs "[Stuck]" lines when a character's run animation is playing
// but it isn't actually moving. Remove once the freeze bug is found.
public class StuckDebugger : MonoBehaviour
{
    const float stuckAfter = 0.25f;

    Animator animator;
    Player player;
    Vector3 lastPos;
    float stuckTime;
    bool reported;
    string lastHit = "nothing";
    float lastHitTime = -999f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
        lastPos = transform.position;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.5f) return; // ignore the ground
        lastHit = hit.collider.name + " (layer " + LayerMask.LayerToName(hit.collider.gameObject.layer) + ")";
        lastHitTime = Time.time;
    }

    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        lastPos = transform.position;

        float animSpeed = animator != null ? animator.GetFloat("Speed") : 0f;
        float actualSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        bool tryingToMove = animSpeed > 0.5f;
        bool barelyMoving = actualSpeed < animSpeed * 0.2f;

        if (tryingToMove && barelyMoving)
        {
            stuckTime += Time.deltaTime;
            if (stuckTime >= stuckAfter && !reported)
            {
                reported = true;
                Debug.Log($"[Stuck] START {Describe()} animSpeed={animSpeed:F1} actualSpeed={actualSpeed:F2} " +
                          $"lastHit={lastHit} ({Time.time - lastHitTime:F2}s ago) {BallInfo()}");
            }
        }
        else
        {
            if (reported)
                Debug.Log($"[Stuck] END {Describe()} after {stuckTime:F2}s {BallInfo()}");
            stuckTime = 0f;
            reported = false;
        }
    }

    string Describe()
    {
        var tpc = GetComponent<ThirdPersonController>();
        var ai = GetComponent<AITeammate>();
        string mode = tpc != null && tpc.enabled ? "HUMAN" : ai != null && ai.enabled ? "AI" : "NONE";
        string side = player != null && player.team != null ? player.team.side.ToString() : "noTeam";
        return $"{name}#{GetInstanceID()} [{side}/{mode}] pos={transform.position:F1} t={Time.time:F2}";
    }

    string BallInfo()
    {
        Ball ball = Game.Instance != null ? Game.Instance.ball : null;
        if (ball == null) return "ball=?";
        Player carrier = ball.Carrier;
        string c = carrier == null ? "loose" : carrier == player ? "ME" : carrier.name + "#" + carrier.GetInstanceID();
        return $"carrier={c} ballPos={ball.transform.position:F1}";
    }
}
