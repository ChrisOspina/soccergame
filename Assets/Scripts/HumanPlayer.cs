using UnityEngine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;

public class HumanPlayer : MonoBehaviour
{
    Player playerScript;
    private StarterAssetsInputs _input;

    void Awake()
    {
        playerScript = GetComponent<Player>();
        _input = GetComponent<StarterAssetsInputs>();
    }


    // Update is called once per frame
    void Update()
    {
        if (Game.Instance != null && Game.Instance.IsMatchOver) return;

        if (_input.pass)
        {
            _input.pass = false;
            playerScript.Pass();
        }

        if (_input.shoot)
        {
            if (playerScript.BallAttachedToPlayer != null)
            {
                playerScript.shootingPower = Mathf.Clamp01(playerScript.shootingPower + 0.5f * Time.deltaTime);
                Game.Instance.SetPowerBar(playerScript.shootingPower);
            }
        }
        else if (playerScript.shootingPower > 0f)
        {
            playerScript.Shoot();
            playerScript.shootingPower = 0f;
            Game.Instance.SetPowerBar(0f);
        }
    }
}
