using UnityEngine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;

public class HumanPlayer : MonoBehaviour
{
    Player playerScript;
    private StarterAssetsInputs _input;
    private ThirdPersonController tpc;

    void Awake()
    {
        playerScript = GetComponent<Player>();
        _input = GetComponent<StarterAssetsInputs>();
        tpc = GetComponent<ThirdPersonController>();
    }

    void OnDisable()
    {
        if (tpc != null) tpc.MovementLocked = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (Game.Instance != null && Game.Instance.IsMatchOver) return;

        if (tpc != null) tpc.MovementLocked = playerScript.IsStunned;

        if (_input.pass)
        {
            _input.pass = false;
            // Without the ball, the pass button calls for it from whichever AI teammate has it.
            if (playerScript.BallAttachedToPlayer != null)
                playerScript.Pass();
            else
                playerScript.team?.RequestPass(playerScript);
        }

        if (_input.shoot)
        {
            _input.shoot = false;
            // On defence the shoot button becomes the tackle button.
            if (playerScript.BallAttachedToPlayer != null)
                playerScript.Shoot();
            else
                playerScript.Tackle();
        }

        if (_input.switchPlayer)
        {
            _input.switchPlayer = false;
            playerScript.team?.SwitchControlled();
        }
    }
}
