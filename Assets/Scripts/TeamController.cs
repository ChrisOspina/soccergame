using System.Collections.Generic;
using Cinemachine;
using StarterAssets;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum TeamSide { Player, COM }

public class TeamController : MonoBehaviour
{
    public TeamSide side;

    [Header("Goals")]
    public Transform opponentGoal; // where this team shoots
    public Transform ownGoal;      // the net this team defends

    [Header("UI / Audio")]
    public TMP_Text scoreText;
    public string scoreLabel = "Team";
    public AudioClip goalClip;

    [Header("Passing")]
    [Tooltip("Minimum facing dot product (1 = dead ahead, 0 = 90 degrees) for a teammate to be considered a valid pass target")]
    [Range(0f, 1f)] public float passFacingThreshold = 0.3f;
    public float maxPassDistance = 25f;

    [Header("Player-controlled side only")]
    public CinemachineVirtualCamera vcam;

    private readonly List<Player> members = new List<Player>();
    private int score;
    private int controlledIndex;
    private float controlledPassTime = -999f;
    private const float passSwitchWindow = 3f;

    public IReadOnlyList<Player> Members => members;
    public int Score => score;
    public Player Controlled => (side == TeamSide.Player && members.Count > 0) ? members[controlledIndex] : null;

    public void RegisterMember(Player player)
    {
        if (!members.Contains(player))
            members.Add(player);
    }

    public void UnregisterMember(Player player)
    {
        members.Remove(player);
    }

    public void AddGoal()
    {
        score++;
        if (scoreText != null)
            scoreText.text = scoreLabel + ": " + score.ToString();
        if (goalClip != null)
        {
            // Use an AudioSource on this object; fall back to a member's so the cheer still routes through the SFX mixer group.
            AudioSource source = GetComponent<AudioSource>();
            if (source == null && members.Count > 0) source = members[0].GetComponent<AudioSource>();
            if (source != null) source.PlayOneShot(goalClip);
        }
        else
        {
            Debug.LogWarning($"{name}: goalClip is not assigned, so no goal sound will play.");
        }
        Game.Instance?.OnGoalScored();
    }

    public void ResetPositions()
    {
        foreach (Player member in members)
            member.ResetPosition();
    }

    public Player FindBestPassTarget(Player passer)
    {
        Player best = null;
        float bestDot = passFacingThreshold;

        foreach (Player teammate in members)
        {
            if (teammate == passer) continue;

            Vector3 toTeammate = teammate.transform.position - passer.transform.position;
            toTeammate.y = 0f;
            float distance = toTeammate.magnitude;
            if (distance < 0.01f || distance > maxPassDistance) continue;

            float dot = Vector3.Dot(passer.transform.forward, toTeammate.normalized);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = teammate;
            }
        }

        return best;
    }

    // Asks the AI teammate carrying the ball to pass it to requester.
    public void RequestPass(Player requester)
    {
        foreach (Player member in members)
        {
            if (member == requester || member.BallAttachedToPlayer == null) continue;
            AITeammate ai = member.GetComponent<AITeammate>();
            if (ai != null && ai.enabled)
                ai.RequestPass(requester);
            return;
        }
    }

    // Called by Player when it passes, so the receiver of *your* pass can take over control.
    public void OnMemberPassed(Player passer)
    {
        if (passer == Controlled)
            controlledPassTime = Time.time;
    }

    // Called by Ball whenever a member gains possession. On the human side, control follows
    // the ball only when it was your pass; switching on every AI pickup/steal made the camera jump constantly.
    public void OnBallReceived(Player receiver)
    {
        if (side != TeamSide.Player) return;
        if (Time.time - controlledPassTime > passSwitchWindow) return;
        controlledPassTime = -999f;
        int index = members.IndexOf(receiver);
        if (index >= 0 && index != controlledIndex)
            SetControlled(index);
    }

    public void SwitchControlled()
    {
        if (side != TeamSide.Player || members.Count < 2) return;

        int nextIndex = (controlledIndex + 1) % members.Count;
        SetControlled(nextIndex);
    }

    private void SetControlled(int index)
    {
        Player previous = members[controlledIndex];
        Player next = members[index];
        Debug.Log($"[Stuck] CONTROL SWITCH {previous.name}#{previous.GetInstanceID()} -> {next.name}#{next.GetInstanceID()} t={Time.time:F2}"); // TEMP diagnostic

        SetHumanControlled(previous, false);
        SetHumanControlled(next, true);

        controlledIndex = index;

        if (vcam != null)
        {
            ThirdPersonController tpc = next.GetComponent<ThirdPersonController>();
            if (tpc != null && tpc.CinemachineCameraTarget != null)
            {
                vcam.Follow = tpc.CinemachineCameraTarget.transform;
                vcam.LookAt = tpc.CinemachineCameraTarget.transform;
            }
        }
    }

    private void SetHumanControlled(Player player, bool humanControlled)
    {
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (tpc != null) tpc.enabled = humanControlled;

#if ENABLE_INPUT_SYSTEM
        PlayerInput playerInput = player.GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = humanControlled;
#endif

        // Disabling PlayerInput mid-keypress never sends the release, so without this a
        // character handed back to the AI (or back to you later) keeps "holding" old input.
        StarterAssetsInputs input = player.GetComponent<StarterAssetsInputs>();
        if (input != null)
        {
            input.move = Vector2.zero;
            input.look = Vector2.zero;
            input.jump = false;
            input.sprint = false;
            input.shoot = false;
            input.pass = false;
            input.switchPlayer = false;
        }

        HumanPlayer human = player.GetComponent<HumanPlayer>();
        if (human != null) human.enabled = humanControlled;

        AITeammate ai = player.GetComponent<AITeammate>();
        if (ai != null) ai.enabled = !humanControlled;
    }
}
