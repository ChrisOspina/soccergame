using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public float pickupRadius = 2.0f;

    [Header("Tackling")]
    [Tooltip("How close an opponent must get to the ball carrier to steal the ball")]
    public float tackleRadius = 1.2f;
    [Tooltip("Seconds after gaining the ball during which the carrier can't be tackled (stops instant back-and-forth steals)")]
    public float tackleProtection = 0.75f;
    [Tooltip("Seconds a player who just lost the ball must wait before tackling back (stops tackle ping-pong)")]
    public float retackleDelay = 1.5f;

    private bool stickToPlayer = false;
    private float releaseTime = -1f;
    private float attachTime = -999f;
    private const float reattachDelay = 0.5f;

    private Rigidbody rb;
    private Collider ballCollider;
    private Player lastDispossessed;
    private float dispossessedTime = -999f;

    public bool StickToPlayer
    {
        get => stickToPlayer;
        set
        {
            if (stickToPlayer && !value)
            {
                releaseTime = Time.time;
                SetCarrierCollisions(attachedPlayer, true);
                attachedPlayer = null;
                rb.isKinematic = false;
                SetPlayerCollisions(true);
            }
            stickToPlayer = value;
        }
    }

    public Player Carrier => stickToPlayer ? attachedPlayer : null;

    public void Respawn()
    {
        stickToPlayer = false;
        if (attachedPlayer != null) SetCarrierCollisions(attachedPlayer, true);
        attachedPlayer?.LoseBall();
        attachedPlayer = null;
        transform.position = startPos;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        SetPlayerCollisions(true);
    }

    float speed;
    Vector2 previousLocation;
    Vector3 startPos;
    Player attachedPlayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
    }

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (!stickToPlayer)
        {
            if (Time.time - releaseTime >= reattachDelay)
            {
                Player nearest = FindNearestPlayer();
                if (nearest != null)
                    AttachTo(nearest);
            }
        }
        else
        {
            if (Time.time - attachTime >= tackleProtection)
            {
                Player tackler = FindTackler();
                if (tackler != null)
                    AttachTo(tackler);
            }

            Vector2 currentLocation = new Vector2(transform.position.x, transform.position.y);
            speed = Vector2.Distance(currentLocation, previousLocation) / Time.deltaTime;
            transform.position = attachedPlayer.BallLocation.position;
            transform.Rotate(new Vector3(attachedPlayer.transform.right.x, 0, attachedPlayer.transform.right.z), speed, Space.World);
            previousLocation = currentLocation;
        }
        if (transform.position.y < -2)
            Respawn();
    }

    private void AttachTo(Player player)
    {
        // A tackle takes the ball off the current carrier.
        if (attachedPlayer != null && attachedPlayer != player)
        {
            SetCarrierCollisions(attachedPlayer, true);
            attachedPlayer.LoseBall();
            lastDispossessed = attachedPlayer;
            dispossessedTime = Time.time;
        }

        stickToPlayer = true;
        attachedPlayer = player;
        attachTime = Time.time;
        player.BallAttachedToPlayer = this;

        // Kinematic while carried so leftover velocity and character pushes can't fight the carry.
        // Skip when already kinematic (ball stolen from another carrier) — Unity warns otherwise.
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;

        // A carried ball is solid, so chasers ran in place against it; players pass through it until it's released.
        SetPlayerCollisions(false);
        // Defenders closing in stood in the carrier's path like a wall; let the carrier slip through them (tackles are distance-based).
        SetCarrierCollisions(player, false);

        player.team?.OnBallReceived(player);
    }

    private Player FindNearestPlayer()
    {
        Player nearest = null;
        float nearestDistance = pickupRadius;

        foreach (Player player in Player.All)
        {
            if (player.BallLocation == null) continue;

            float distance = Vector3.Distance(player.transform.position, transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    private Player FindTackler()
    {
        Player nearest = null;
        float nearestDistance = tackleRadius;

        foreach (Player player in Player.All)
        {
            if (player == attachedPlayer || player.BallLocation == null) continue;
            if (player.team != null && player.team == attachedPlayer.team) continue;
            if (player == lastDispossessed && Time.time - dispossessedTime < retackleDelay) continue;

            float distance = Vector3.Distance(player.transform.position, attachedPlayer.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    private void SetCarrierCollisions(Player carrier, bool collide)
    {
        foreach (Player player in Player.All)
        {
            if (player == carrier || (player.team != null && player.team == carrier.team)) continue;
            // Physics.IgnoreCollision has no effect between two CharacterControllers;
            // detectCollisions = false lets others (the carrier) move through this one.
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.detectCollisions = collide;
        }
    }

    private void SetPlayerCollisions(bool collide)
    {
        if (ballCollider == null) return;
        foreach (Player player in Player.All)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) Physics.IgnoreCollision(ballCollider, cc, !collide);
        }
    }
}
