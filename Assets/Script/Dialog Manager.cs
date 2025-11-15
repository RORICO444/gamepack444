using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] private Text dialogText;

    [SerializeField]private int lettersPerSecond;
    
    public event Action OnShowDialog;
    public event Action OnHideDialog;
    public static DialogManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void HandleUpdate()
    {
        
    }

    public void showDialog(Dialog dialog)
    {
        OnShowDialog?.Invoke();
        
        dialogBox.SetActive(true);
        StartCoroutine(TypeDialog(dialog.Lines[0]));
    }

    public IEnumerator TypeDialog(string line)
    {
        dialogText.text = "";
        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f/lettersPerSecond);
        }
    }
}
