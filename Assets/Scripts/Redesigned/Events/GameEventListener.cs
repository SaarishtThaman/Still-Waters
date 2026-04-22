using UnityEngine;
using UnityEngine.Events;

// Drop this component on any GameObject to respond to a GameEvent asset.
// Wire the response UnityEvent in the Inspector — no code needed.
public class GameEventListener : MonoBehaviour
{
    [SerializeField] GameEvent gameEvent;
    [SerializeField] UnityEvent response;

    void OnEnable()  => gameEvent.RegisterListener(this);
    void OnDisable() => gameEvent.UnregisterListener(this);

    public void OnEventRaised() => response.Invoke();
}
