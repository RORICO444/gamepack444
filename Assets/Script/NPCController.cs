using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour , Interactable
{
    [SerializeField] private Dialog dialog;
    public void Interact()
    {
        DialogManager.Instance.showDialog(dialog);
    }
}
