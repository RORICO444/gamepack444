using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

enum GameState
{
    free,dialog,menu
}


public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    GameState state;

    private void Start()
    {
        DialogManager.Instance.OnShowDialog += (() => state = GameState.dialog);
        DialogManager.Instance.OnHideDialog += (() => state = GameState.free);
    }

    private void Update()
    {
        if (state == GameState.free)
        {
            playerController.HandleUpdate();
        }
        else if (state == GameState.menu)
        {
            DialogManager.Instance.HandleUpdate();
        }
        else if (state == GameState.dialog)
        {
            
        }
    }
}
