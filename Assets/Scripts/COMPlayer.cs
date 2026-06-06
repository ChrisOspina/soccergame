using UnityEngine;

public class COMPlayer : MonoBehaviour
{
    public Ball ball;
    public Transform goalTarget;   // Assign Goal1 (the player's goal) in inspector

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Shooting")]
    public float shootRange = 5f;
    public float shootForce = 15f;
    public float shootCooldown = 2f;

    [Header("Grounded")]
    public float groundedOffset = -0.14f;
    public float groundedRadius = 0.28f;
    public LayerMask groundLayers;

    private CharacterController controller;
    private Animator animator;
    private bool hasAnimator;
    private bool isGrounded;
    private float verticalVelocity;
    private float lastShotTime = -999f;
    private const float gravity = -15f;
    private Vector3 startPos;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDMotionSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        startPos = transform.position;
        hasAnimator = TryGetComponent(out animator);
        if (hasAnimator)
        {
            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }
    }

    public void ResetPosition()
    {
        controller.enabled = false;
        transform.position = startPos;
        controller.enabled = true;
        verticalVelocity = 0f;
    }

    void Update()
    {
        if (Game.Instance != null && Game.Instance.IsMatchOver) return;

        GroundedCheck();
        ApplyGravity();
        ChaseAndShoot();
    }

    void GroundedCheck()
    {
        Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
        if (hasAnimator)
            animator.SetBool(animIDGrounded, isGrounded);
    }

    void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }

    void ChaseAndShoot()
    {
        // Flatten ball position to COM's Y so distance check ignores vertical
        Vector3 ballPos = new Vector3(ball.transform.position.x, transform.position.y, ball.transform.position.z);
        float distToBall = Vector3.Distance(transform.position, ballPos);

        bool moving = false;
        if (distToBall <= shootRange && Time.time - lastShotTime >= shootCooldown)
        {
            Shoot();
        }
        else
        {
            Vector3 dir = (ballPos - transform.position).normalized;
            controller.Move(dir * moveSpeed * Time.deltaTime);
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                moving = true;
            }
        }

        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, moving ? moveSpeed : 0f);
            animator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    // Receives the footstep animation event fired by the Starter Assets animator controller
    private void OnFootstep(AnimationEvent animationEvent) { }

    void Shoot()
    {
        lastShotTime = Time.time;

        // Knock the ball free if the human player has it
        ball.ForceRelease();

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        Vector3 shootDir = (goalTarget.position - ball.transform.position).normalized;
        shootDir.y += 0.2f;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(shootDir * shootForce, ForceMode.Impulse);
    }
}
