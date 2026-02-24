using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Transform transformPlayer;
    public Transform PlayerBallPos;
    private bool stickToPlayer = false;
    public bool StickToPlayer { get => stickToPlayer; set => stickToPlayer = value; }
    float speed;
    Vector2 previousLocation;
    Vector3 startPos;
    Player playerRef;

    void Start()
    {
        PlayerBallPos = transformPlayer.Find("Geometry").Find("BallLocation");
        playerRef = transformPlayer.GetComponent<Player>();
        startPos = transform.position;
    }

    void Update()
    {
        if (!stickToPlayer)
        {
            float distanceToPlayer = Vector3.Distance(transformPlayer.position, transform.position);
            if (distanceToPlayer < 2.0f)
            {
                stickToPlayer = true;
                playerRef.BallAttachedToPlayer = this;
            }
        }
        else
        {
            Vector2 currentLocation = new Vector2(transform.position.x, transform.position.y);
            speed = Vector2.Distance(currentLocation, previousLocation) / Time.deltaTime;
            transform.position = PlayerBallPos.position;
            transform.Rotate(new Vector3(transformPlayer.right.x, 0, transformPlayer.right.z), speed, Space.World);
            previousLocation = currentLocation;
        }
        if(transform.position.y < -2)
        {
            transform.position = startPos;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

        }
    }
}