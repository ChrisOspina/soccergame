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
            _input.shoot = false;
            playerScript.Shoot();
        }
    }
}
