using UnityEngine;
using System;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "SceneLoadEvent", menuName = "Game Events/Scene Load Event")]
public class SceneLoadEventSO : ScriptableObject
{
    
    
    // 确保事件有初始值
    public event Action<GameSceneSO, Vector3, bool> LoadRequestEvent = delegate { };

    public void RaiseLoadRequest(GameSceneSO sceneToLoad, Vector3 posToGo, bool fadeScreen)
    {
        LoadRequestEvent?.Invoke(sceneToLoad, posToGo, fadeScreen);
    }

    // 添加调试信息
    public int GetListenerCount()
    {
        return LoadRequestEvent != null ? LoadRequestEvent.GetInvocationList().Length : 0;
    }
}

