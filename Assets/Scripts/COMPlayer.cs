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

    private CharacterController controller;
    private float verticalVelocity;
    private float lastShotTime = -999f;
    private const float gravity = -15f;
    private Vector3 startPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        startPos = transform.position;
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

        ApplyGravity();
        ChaseAndShoot();
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
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

        if (distToBall <= shootRange && Time.time - lastShotTime >= shootCooldown)
        {
            Shoot();
            return;
        }

        Vector3 dir = (ballPos - transform.position).normalized;
        controller.Move(dir * moveSpeed * Time.deltaTime);

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

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
