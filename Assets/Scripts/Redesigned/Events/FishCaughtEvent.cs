using System.Collections.Generic;
using UnityEngine;

// Typed event channel that carries the caught FishDataSO as a payload.
// Used to notify the catch UI when a fish is successfully reeled in.
[CreateAssetMenu(menuName = "Still Waters/Events/Fish Caught Event")]
public class FishCaughtEvent : ScriptableObject
{
    readonly List<FishCaughtEventListener> listeners = new List<FishCaughtEventListener>();

    public void Raise(FishDataSO fish)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised(fish);
    }

    public void RegisterListener(FishCaughtEventListener l)   => listeners.Add(l);
    public void UnregisterListener(FishCaughtEventListener l) => listeners.Remove(l);
}
