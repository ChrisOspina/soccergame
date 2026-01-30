using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Transform transfromPlayer;
    public Transform PlayerBallPos;
    private bool stickToPlayer = false;
    float speed;
    Vector3 previousLocation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!stickToPlayer)
        {
            float distanceToPlayer = Vector3.Distance(transfromPlayer.position, transform.position);
            if(distanceToPlayer < 2.0f)
            {
                stickToPlayer = true;
            }
        }
        else
        {
            Vector2 currentLocation = new Vector2(transform.position.x, transform.position.y);
 
            speed = Vector2.Distance(currentLocation, new Vector2(previousLocation.x, previousLocation.y)) / Time.deltaTime;
            transform.position = PlayerBallPos.position;
            transform.Rotate(new Vector3(transfromPlayer.right.x, 0, transfromPlayer.right.z), speed, Space.World);
            previousLocation = currentLocation;
        }
    }
}
