using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static LargeDialogManager;

enum GameState
{
    free,dialog,largeDialog,menu
}


public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    GameState state;

    private void Start()
    {
        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.dialog;
        };
        DialogManager.Instance.OnHideDialog += () => { 
            if(state == GameState.dialog)
                state = GameState.free;
        };
        LargeDialogManager.Instance.OnDialogStart += () =>
        {
            state = GameState.largeDialog;
        };
        LargeDialogManager.Instance.OnDialogEnd += () =>
        {
            if (state == GameState.largeDialog)
                state = GameState.free;
        };
    }

    private void Update()
    {
        if (state == GameState.free)
        {
            playerController.HandleUpdate();
        }
        else if (state == GameState.menu)
        {
            
        }
        else if (state == GameState.dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
        else if (state == GameState.largeDialog)
        {
            LargeDialogManager.Instance.HandleUpdate();
        }
    }
}
