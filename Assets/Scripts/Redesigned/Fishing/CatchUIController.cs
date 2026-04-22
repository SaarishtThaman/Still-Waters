using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the catch card when a fish is reeled in and routes the close action
// to StoryManager. Subscribes to FishingController.onFishCaught.
public class CatchUIController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] FishingController fishingController;
    [SerializeField] PlayerController  playerController;
    [SerializeField] StoryManager      storyManager;
    [SerializeField] BloodScreen       bloodScreen;

    [Header("UI")]
    [SerializeField] GameObject      fishUI;
    [SerializeField] Image           fishSprite;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI priceText;

    [Header("Audio")]
    [SerializeField] AudioClip uiCloseClip;
    private AudioSource audioSource;

    private FishDataSO currentFish;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        fishUI.SetActive(false);
    }

    void Start()
    {
        fishingController.onFishCaught += ShowCatch;
    }

    void ShowCatch(FishDataSO fish)
    {
        currentFish          = fish;
        fishSprite.sprite    = fish.sprite;
        nameText.text        = fish.title;
        descriptionText.text = fish.description;
        priceText.text       = fish.type == FishDataSO.FishType.BodyPart
            ? "???"
            : $"${fish.sellingPrice}";

        fishUI.SetActive(true);
        playerController.SetUIDisplay(true);

        if (fish.type == FishDataSO.FishType.BodyPart)
            bloodScreen.PlayBloodScreen();
    }

    // Called by the close button in the Inspector.
    public void CloseUI()
    {
        fishUI.SetActive(false);
        audioSource.PlayOneShot(uiCloseClip);
        storyManager.HandleCatch(currentFish, playerController);
    }
}
