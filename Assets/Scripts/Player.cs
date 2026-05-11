using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class Player : MonoBehaviour
{
    private StarterAssetsInputs _input;
    private Animator animator;
    private Ball ballAttachedToPlayer;
    private float timeShot = -1f;
    private const int ANIMATION_lAYER_SHOOT = 1;
    private int myScore, otherScore;
    private CharacterController controller;
    private float distanceSinceLastDribble;
    public float shootingPower;

    public TMP_Text scoreText;
    public TMP_Text comScoreText;

    public AudioMixer mixer;
    AudioSource src;

    private float volume_master = 0;
    float volume_music = 0;
    float volume_sfx = 0;
    float volume_ambient = 0;

    public AudioClip dribble;
    public AudioClip kick;
    public AudioClip goal;

    public Ball BallAttachedToPlayer { get => ballAttachedToPlayer; set=>ballAttachedToPlayer = value; }

    void SaveSettings()
    {
        mixer.GetFloat("volume_master", out volume_master);
        mixer.GetFloat("volume_music", out volume_music);
        mixer.GetFloat("volume_sfx", out volume_sfx);
        mixer.GetFloat("volume_ambient", out volume_ambient);
    }

    void RestoreSettings()
    {
        mixer.SetFloat("volume_master", volume_master);
        mixer.SetFloat("volume_music", volume_music);
        mixer.SetFloat("volume_sfx", volume_sfx);
        mixer.SetFloat("volume_ambient", volume_ambient);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SaveSettings();

        _input = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        src = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        if (_input.shoot)
            Shoot();

        if (timeShot > 0f)
        {
            if (ballAttachedToPlayer != null && Time.time - timeShot > 0.2)
            {
                src.PlayOneShot(kick);

                ballAttachedToPlayer.StickToPlayer = false;

                Rigidbody rigidbody = ballAttachedToPlayer.transform.gameObject.GetComponent<Rigidbody>();
                Vector3 shootdirection = transform.forward;
                shootdirection.y += 0.2f;
                rigidbody.AddForce(shootdirection * (10 + shootingPower * 20f), ForceMode.Impulse);
                ballAttachedToPlayer = null;
            }

            if (Time.time - timeShot > 0.5)
                timeShot = -1f;
        }
        else
        {
            animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, Mathf.Lerp(animator.GetLayerWeight(ANIMATION_lAYER_SHOOT), 0f, Time.deltaTime * 10f));
        }

        if(ballAttachedToPlayer != null)
        {
            distanceSinceLastDribble += speed * Time.deltaTime;
            if(distanceSinceLastDribble > 1)
            {
                src.PlayOneShot(dribble);
                distanceSinceLastDribble = 0;
            }
        }
    }

    public void LoseBall()
    {
        ballAttachedToPlayer = null;
    }

    public void Shoot()
    {
        _input.shoot = false;
        timeShot = Time.time;
        animator.Play("Shoot", ANIMATION_lAYER_SHOOT, 0f);
        animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, 1f);
    }

    public void Pass()
    {

    }

    public void IncreaseMyScore()
    {
        src.PlayOneShot(goal);
        myScore++;
        scoreText.text = "Player: " + myScore.ToString();
    }

    public void IncreaseOtherScore()
    {
        src.PlayOneShot(goal);
        otherScore++;
        comScoreText.text = "COM: " + otherScore.ToString();
    }
}
