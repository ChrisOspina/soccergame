using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class Player : MonoBehaviour
{
    public static readonly List<Player> All = new List<Player>();

    public TeamController team;

    private StarterAssetsInputs _input;
    private Animator animator;
    private Ball ballAttachedToPlayer;
    private float timeShot = -1f;
    private const int ANIMATION_lAYER_SHOOT = 1;
    private CharacterController controller;
    private Vector3 startPos;
    private Vector3 lastPosition;
    public float passForce = 8f;
    public float shootForce = 20f;

    public AudioMixer mixer;
    AudioSource src;

    private float volume_master = 0;
    float volume_music = 0;
    float volume_sfx = 0;
    float volume_ambient = 0;

    public AudioClip dribble;
    public AudioClip kick;
    [Range(0f, 1f)] public float dribbleVolume = 0.5f;
    public float dribbleMinSpeed = 0.5f;

    // Dedicated looping source so the (long) dribble clip never stacks on top of itself.
    AudioSource dribbleSrc;

    public Transform BallLocation { get; private set; }

    public Ball BallAttachedToPlayer { get => ballAttachedToPlayer; set=>ballAttachedToPlayer = value; }

    void Awake()
    {
        BallLocation = transform.Find("Geometry/BallLocation");
        if (GetComponent<StuckDebugger>() == null) gameObject.AddComponent<StuckDebugger>(); // TEMP diagnostic
    }

    void OnEnable()
    {
        All.Add(this);
        team?.RegisterMember(this);
    }

    void OnDisable()
    {
        All.Remove(this);
        team?.UnregisterMember(this);
    }

    void SaveSettings()
    {
        if (mixer == null) return;
        mixer.GetFloat("volume_master", out volume_master);
        mixer.GetFloat("volume_music", out volume_music);
        mixer.GetFloat("volume_sfx", out volume_sfx);
        mixer.GetFloat("volume_ambient", out volume_ambient);
    }

    void RestoreSettings()
    {
        if (mixer == null) return;
        mixer.SetFloat("volume_master", volume_master);
        mixer.SetFloat("volume_music", volume_music);
        mixer.SetFloat("volume_sfx", volume_sfx);
        mixer.SetFloat("volume_ambient", volume_ambient);
    }

    void Start()
    {
        SaveSettings();

        _input = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        src = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();
        startPos = transform.position;
        lastPosition = transform.position;

        if (dribble != null)
        {
            // Teammates are cloned at runtime and may already carry the template's dribble source.
            foreach (AudioSource existing in GetComponents<AudioSource>())
                if (existing != src && existing.clip == dribble) dribbleSrc = existing;
            if (dribbleSrc == null) dribbleSrc = gameObject.AddComponent<AudioSource>();
            dribbleSrc.clip = dribble;
            dribbleSrc.loop = true;
            dribbleSrc.playOnAwake = false;
            dribbleSrc.volume = dribbleVolume;
            if (src != null)
            {
                dribbleSrc.outputAudioMixerGroup = src.outputAudioMixerGroup;
                dribbleSrc.spatialBlend = src.spatialBlend;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Game.Instance != null && Game.Instance.IsMatchOver)
        {
            if (dribbleSrc != null && dribbleSrc.isPlaying) dribbleSrc.Stop();
            return;
        }

        // Measured from position rather than controller.velocity, which only reflects the last
        // Move() call (the AI calls Move twice per frame: horizontal, then gravity).
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPosition = transform.position;

        if (timeShot > 0f)
        {
            if (ballAttachedToPlayer != null && Time.time - timeShot > 0.2)
            {
                src.PlayOneShot(kick);

                ballAttachedToPlayer.StickToPlayer = false;

                Rigidbody rigidbody = ballAttachedToPlayer.transform.gameObject.GetComponent<Rigidbody>();
                Vector3 shootdirection = transform.forward;
                shootdirection.y += 0.2f;
                rigidbody.AddForce(shootdirection * shootForce, ForceMode.Impulse);
                ballAttachedToPlayer = null;
            }

            if (Time.time - timeShot > 0.5)
                timeShot = -1f;
        }
        else
        {
            animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, Mathf.Lerp(animator.GetLayerWeight(ANIMATION_lAYER_SHOOT), 0f, Time.deltaTime * 10f));
        }

        UpdateDribbleSound(ballAttachedToPlayer != null && speed > dribbleMinSpeed);
    }

    private void UpdateDribbleSound(bool dribbling)
    {
        if (dribbleSrc == null) return;
        if (dribbling && !dribbleSrc.isPlaying) dribbleSrc.Play();
        else if (!dribbling && dribbleSrc.isPlaying) dribbleSrc.Pause();
    }

    public void LoseBall()
    {
        ballAttachedToPlayer = null;
    }

    public void ResetPosition()
    {
        controller.enabled = false;
        transform.position = startPos;
        lastPosition = startPos;
        controller.enabled = true;
    }

    public void Shoot()
    {
        if (ballAttachedToPlayer == null) return;
        timeShot = Time.time;
        animator.Play("Shoot", ANIMATION_lAYER_SHOOT, 0f);
        animator.SetLayerWeight(ANIMATION_lAYER_SHOOT, 1f);
    }

    public void Pass()
    {
        PassTo(team?.FindBestPassTarget(this));
    }

    // Passes straight at target, or along the facing direction when target is null.
    public void PassTo(Player target)
    {
        if (ballAttachedToPlayer == null) return;
        team?.OnMemberPassed(this);
        src.PlayOneShot(kick);
        ballAttachedToPlayer.StickToPlayer = false;

        Vector3 passDirection = transform.forward;
        if (target != null)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            passDirection = toTarget.normalized;
        }
        passDirection.y += 0.1f;

        Rigidbody rb = ballAttachedToPlayer.GetComponent<Rigidbody>();
        rb.AddForce(passDirection.normalized * passForce, ForceMode.Impulse);
        ballAttachedToPlayer = null;
    }
}
