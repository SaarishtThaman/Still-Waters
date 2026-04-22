using System.Collections.Generic;
using UnityEngine;

// Ryan Hipple's ScriptableObject event channel (void payload).
// Each event is a .asset file — drag it into any listener in the Inspector.
// Systems raise events without knowing who listens; listeners respond without
// knowing who raised them.
[CreateAssetMenu(menuName = "Still Waters/Events/Game Event")]
public class GameEvent : ScriptableObject
{
    readonly List<GameEventListener> listeners = new List<GameEventListener>();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener l)   => listeners.Add(l);
    public void UnregisterListener(GameEventListener l) => listeners.Remove(l);
}
