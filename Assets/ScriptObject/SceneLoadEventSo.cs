using System;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(menuName = "Events/SceneLoadEventSo")]
public class SceneLoadEventSO : ScriptableObject
{
   
    
    public UnityAction<GameSceneSO, Vector3, bool> LoadRequestEvent;
    //唤起加载请求
    public void RaiseLoadRequestEvent(GameSceneSO locationToLoad, Vector3 positionToLoad, bool fadeScreen)
    {
        LoadRequestEvent?.Invoke(locationToLoad, positionToLoad, fadeScreen);
    }

    
}