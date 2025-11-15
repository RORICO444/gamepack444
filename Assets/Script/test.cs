using System.Collections;
using UnityEngine;
// 在可交互物体上添加这个脚本
public class SimpleInteractable : MonoBehaviour
{
    private void Start()
    {
        // 确保物体在可交互Layer上
        Debug.Log("可交互物体: " + gameObject.name + " - Layer: " + LayerMask.LayerToName(gameObject.layer));
    }
    
    public void Interact()
    {
        Debug.Log("交互成功!: " + gameObject.name);
    }
}
