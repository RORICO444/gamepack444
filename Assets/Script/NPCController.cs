using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCController : MonoBehaviour , Interactable
{
    [SerializeField] private Dialog dialog;

    public BoxCollider2D coll;
    public void Interact()
    {
        StartCoroutine(DialogManager.Instance.showDialog(dialog));
        LargeDialogManager.Instance.interacteCount++;
        coll.enabled = false;
        if(LargeDialogManager.Instance.interacteCount >= 2)
        {
            LargeDialogManager.Instance.interacteCount = 0;
            LargeDialogManager.Instance.ShowNextLine();
        }
    }
}
