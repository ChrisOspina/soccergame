using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private StarterAssetsInputs _input;
    private Animator animator;
    private Ball ballAttachedToPlayer;
    private float timeShot = -1f;
    private const int ANIMATION_lAYER_SHOOT = 1;
    private int myScore, otherScore;

    public Ball BallAttachedToPlayer { get => ballAttachedToPlayer; set=>ballAttachedToPlayer = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_input.shoot)
        {
            _input.shoot = false;
            timeShot = Time.time;
            animator.Play("Shoot", ANIMATION_lAYER_SHOOT, 0f);
            animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, 1f);
            //Debug.Log("Shoot is fired! The input works!");
        }
        if (timeShot > 0f) {
            //Shoot the ball
            if (ballAttachedToPlayer != null && Time.time - timeShot >0.2)
            {
                ballAttachedToPlayer.StickToPlayer = false;

                Rigidbody rigidbody = ballAttachedToPlayer.transform.gameObject.GetComponent<Rigidbody>();
                Vector3 shootdirection = transform.forward;
                shootdirection.y += 0.2f;
                rigidbody.AddForce(shootdirection * 20f, ForceMode.Impulse);
                ballAttachedToPlayer = null;
            }
            //Finish kicking animation
            if(Time.time - timeShot > 0.5)
            {
                timeShot = -1f;
            }
        }
        else
        {
            animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, Mathf.Lerp(animator.GetLayerWeight(ANIMATION_lAYER_SHOOT), 0f, Time.deltaTime * 10f));
        }
    }

    public void IncreaseMyScore()
    {
        myScore++;
    }

    public void IncreaseOtherScore()
    {
        otherScore++;
    }
}
