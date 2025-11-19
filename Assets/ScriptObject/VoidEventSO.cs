using UnityEngine;

[CreateAssetMenu(fileName = "VoidEvent", menuName = "Game Events/Void Event")]
public class VoidEventSO : ScriptableObject
{
    public System.Action OnEventRaised;
    
    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}