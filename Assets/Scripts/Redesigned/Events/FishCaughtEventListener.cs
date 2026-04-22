using UnityEngine;
using UnityEngine.Events;

// Drop this component on any GameObject to respond to a FishCaughtEvent asset.
public class FishCaughtEventListener : MonoBehaviour
{
    [SerializeField] FishCaughtEvent gameEvent;
    [SerializeField] UnityEvent<FishDataSO> response;

    void OnEnable()  => gameEvent.RegisterListener(this);
    void OnDisable() => gameEvent.UnregisterListener(this);

    public void OnEventRaised(FishDataSO fish) => response.Invoke(fish);
}
