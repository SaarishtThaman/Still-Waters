using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Single source of truth for all story progression.
// Previously scattered across GameManager, FishingManager.CloseUI(),
// and StoryEventManager — now centralised here.
public class StoryManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] SaveManager      saveManager;
    [SerializeField] SceneFlowConfigSO sceneFlow;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI dayText;

    [Header("Audio")]
    [SerializeField] AudioClip seagullClip;
    private AudioSource audioSource;

    [Header("Discovery Dialogs")]
    [SerializeField] DialogSequenceSO severedHandDialog;
    [SerializeField] DialogSequenceSO armDialog;
    [SerializeField] DialogSequenceSO legDialog;
    [SerializeField] DialogSequenceSO organNecklaceDialog;
    [SerializeField] DialogSequenceSO maleHeadDialog;
    [SerializeField] DialogSequenceSO wifeTorsoDialog;

    // FishingLogic reads this flag to know whether to force-return the severed hand.
    public bool SeveredHandEventPlay { get; set; }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        var save = saveManager.data;
        if (dayText != null)
            dayText.text = $"DAY {1 + save.dayNumber}";

        EvaluateStoryConditions(save);
    }

    void EvaluateStoryConditions(SaveData save)
    {
        bool hasUpgrade = save.engineLevel > 0 || save.rodLevel > 0 || save.baitLevel > 0;

        if (save.dayNumber >= 2 && hasUpgrade && !save.severedHandEventPassed)
        {
            SeveredHandEventPlay = true;
            PlaySeagull();
        }

        // Penultimate scene always plays seagull ambience.
        if (SceneManager.GetActiveScene().name == sceneFlow.penultimateScene)
            PlaySeagull();
    }

    void PlaySeagull()
    {
        audioSource.clip = seagullClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Called by CatchUIController.CloseUI after the player dismisses the catch card.
    public void HandleCatch(FishDataSO fish, PlayerController player)
    {
        var save = saveManager.data;

        switch (fish.bodyPartId)
        {
            case FishDataSO.BodyPartId.SeveredHand:
                player.IsScared             = true;
                save.severedHandEventPassed = true;
                // switchScene=true → DialogController will call GameFlowController.PlayNextEvent
                DialogController.instance.StartDialog(severedHandDialog, switchScene: true, player);
                break;

            case FishDataSO.BodyPartId.Arm:
                save.armFound = true;
                save.partsFound++;
                player.UpdatePartsFound();
                DialogController.instance.StartDialog(armDialog, switchScene: false, player);
                break;

            case FishDataSO.BodyPartId.Leg:
                save.legFound = true;
                save.partsFound++;
                player.UpdatePartsFound();
                DialogController.instance.StartDialog(legDialog, switchScene: false, player);
                break;

            case FishDataSO.BodyPartId.OrganNecklace:
                save.organNecklaceFound = true;
                save.partsFound++;
                player.UpdatePartsFound();
                DialogController.instance.StartDialog(organNecklaceDialog, switchScene: false, player);
                break;

            case FishDataSO.BodyPartId.MaleHead:
                save.maleHeadFound = true;
                save.partsFound++;
                player.UpdatePartsFound();
                DialogController.instance.StartDialog(maleHeadDialog, switchScene: false, player);
                break;

            case FishDataSO.BodyPartId.WifeTorso:
                DialogController.instance.StartDialog(wifeTorsoDialog, switchScene: true, player);
                break;

            default:
                // Regular fish or junk — return control to player immediately.
                player.SetUIDisplay(false);
                player.ShowCaughtFishThought(fish);
                break;
        }
    }
}
