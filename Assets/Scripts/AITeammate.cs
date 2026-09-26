using StarterAssets;
using UnityEngine;

// Drives every non-human-controlled character on both teams: the COM roster always,
// and the human team's members while they aren't the one currently under player control.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Player))]
public class AITeammate : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    [Tooltip("Use the human player's sprint speed (from Game.playerTemplate) instead of Move Speed")]
    public bool matchHumanSpeed = true;
    [Tooltip("Fraction of the human's sprint speed used when Match Human Speed is on")]
    [Range(0.5f, 1.2f)] public float humanSpeedMultiplier = 0.85f;

    [Header("Shooting / Passing")]
    public float shootRange = 8f;
    public float actionCooldown = 1f;
    [Tooltip("An opponent this close counts as pressure and makes the carrier look to pass")]
    public float pressureRadius = 3f;
    [Tooltip("Carry the ball at least this long before a voluntary (unpressured) pass")]
    public float minHoldTime = 1.5f;

    [Header("Formation (relative to own goal, along the attack axis)")]
    public float formationDistance = 15f;
    public float formationLateral = 0f;
    public float supportDistanceFromBallCarrier = 6f;

    [Header("Grounded")]
    public float groundedOffset = -0.14f;
    public float groundedRadius = 0.28f;
    public LayerMask groundLayers;

    private const float gravity = -15f;
    private const float arriveThreshold = 0.3f;

    private CharacterController controller;
    private Player playerController;
    private Animator animator;
    private bool hasAnimator;
    private bool isGrounded;
    private float verticalVelocity;
    private float lastActionTime = -999f;
    private float possessionStartTime = -999f;
    private bool hadBall;
    private Player passRequester;
    private float passRequestTime = -999f;
    private const float passRequestWindow = 1f;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDMotionSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<Player>();
        hasAnimator = TryGetComponent(out animator);
        if (hasAnimator)
        {
            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }
    }

    void Start()
    {
        if (matchHumanSpeed && Game.Instance != null && Game.Instance.playerTemplate != null)
        {
            ThirdPersonController human = Game.Instance.playerTemplate.GetComponent<ThirdPersonController>();
            if (human != null) moveSpeed = human.SprintSpeed * humanSpeedMultiplier;
        }
    }

    public void RequestPass(Player requester)
    {
        passRequester = requester;
        passRequestTime = Time.time;
    }

    void OnEnable()
    {
        passRequester = null;
        verticalVelocity = 0f;
        lastActionTime = -999f;
        hadBall = false;
    }

    void Update()
    {
        if (Game.Instance == null || Game.Instance.ball == null || Game.Instance.IsMatchOver)
        {
            // Without this the animator keeps its last run speed and the player runs in place.
            if (hasAnimator) animator.SetFloat(animIDSpeed, 0f);
            return;
        }

        GroundedCheck();
        ApplyGravity();

        bool moving = Act(Game.Instance.ball);

        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, moving ? moveSpeed : 0f);
            animator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    private bool Act(Ball ball)
    {
        TeamController team = playerController.team;
        if (team == null) return MoveToward(transform.position);

        bool iHaveBall = playerController.BallAttachedToPlayer != null;
        if (iHaveBall && !hadBall)
            possessionStartTime = Time.time;
        hadBall = iHaveBall;

        if (iHaveBall)
            return HandlePossession(team);

        if (!TeammateHasBall(team) && IsClosestTeammateToBall(team, ball))
        {
            // Pushing into an opponent carrier's body just runs in place; close to tackle range and hold there.
            Player carrier = ball.Carrier;
            if (carrier != null)
                return MoveToward(carrier.transform.position, ball.tackleRadius * 0.75f);
            // Pickup is radius-based, so stop short instead of running into a ball that can't be grabbed yet (reattach delay).
            return MoveToward(ball.transform.position, ball.pickupRadius * 0.5f);
        }

        return MoveToward(FormationTarget(team, ball));
    }

    private bool HandlePossession(TeamController team)
    {
        // A called-for pass beats everything else, including the action cooldown.
        if (passRequester != null && Time.time - passRequestTime <= passRequestWindow)
        {
            Player target = passRequester;
            passRequester = null;
            FaceToward(target.transform.position);
            playerController.PassTo(target);
            lastActionTime = Time.time;
            return false;
        }

        Transform goal = team.opponentGoal;
        if (goal == null) return false;

        float distToGoal = Vector3.Distance(transform.position, goal.position);
        bool canAct = Time.time - lastActionTime >= actionCooldown;

        if (canAct && distToGoal <= shootRange)
        {
            FaceToward(goal.position);
            playerController.Shoot();
            lastActionTime = Time.time;
            return false;
        }

        // Only pass when pressured, or when a teammate is better placed up the field;
        // otherwise every receiver instantly passed back and the ball ping-ponged forever.
        bool pressured = UnderPressure(team);
        bool heldLongEnough = Time.time - possessionStartTime >= minHoldTime;
        if (canAct && (pressured || heldLongEnough))
        {
            Player target = team.FindBestPassTarget(playerController);
            bool targetIsForward = target != null &&
                Vector3.Distance(target.transform.position, goal.position) < distToGoal - 2f;
            if (target != null && (pressured || targetIsForward))
            {
                FaceToward(target.transform.position);
                playerController.Pass();
                lastActionTime = Time.time;
                return false;
            }
        }

        return MoveToward(goal.position);
    }

    private bool UnderPressure(TeamController team)
    {
        foreach (Player other in Player.All)
        {
            if (other.team == team) continue;
            if (Vector3.Distance(other.transform.position, transform.position) < pressureRadius)
                return true;
        }
        return false;
    }

    private bool TeammateHasBall(TeamController team)
    {
        foreach (Player member in team.Members)
        {
            if (member != playerController && member.BallAttachedToPlayer != null)
                return true;
        }
        return false;
    }

    private bool IsClosestTeammateToBall(TeamController team, Ball ball)
    {
        float myDistance = Vector3.Distance(transform.position, ball.transform.position);
        foreach (Player member in team.Members)
        {
            if (member == playerController) continue;
            if (Vector3.Distance(member.transform.position, ball.transform.position) < myDistance)
                return false;
        }
        return true;
    }

    private Vector3 FormationTarget(TeamController team, Ball ball)
    {
        if (team.ownGoal == null || team.opponentGoal == null)
            return transform.position;

        Vector3 attackDir = team.opponentGoal.position - team.ownGoal.position;
        attackDir.y = 0f;
        attackDir = attackDir.sqrMagnitude > 0.001f ? attackDir.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, attackDir);

        Vector3 spot = team.ownGoal.position + attackDir * formationDistance + right * formationLateral;

        // Drift toward the ball's lane a bit so support runs feel responsive, without abandoning the formation shape.
        Vector3 ballFlat = new Vector3(ball.transform.position.x, spot.y, ball.transform.position.z);
        return Vector3.Lerp(spot, ballFlat, 0.15f);
    }

    private bool MoveToward(Vector3 target, float stopDistance = arriveThreshold)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        Vector3 toTarget = flatTarget - transform.position;
        float distance = toTarget.magnitude;
        if (distance < stopDistance) return false;

        Vector3 dir = toTarget / distance;
        controller.Move(dir * moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(dir);
        return true;
    }

    private void FaceToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private void GroundedCheck()
    {
        Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
        if (hasAnimator)
            animator.SetBool(animIDGrounded, isGrounded);
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }

    // Swallows animation events shared with ThirdPersonController's animator controller.
    private void OnFootstep(AnimationEvent animationEvent) { }
    private void OnLand(AnimationEvent animationEvent) { }
}
