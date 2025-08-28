using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class FishingManager : MonoBehaviour
{

    public static FishingManager instance;

    public FishData[] fish;
    public FishData[] junk;
    public FishData[] bodyParts;
    public FishData severedHand;

    private FishData[] allFishable;

    public GameObject fishUI;
    public Image fishSprite;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText, priceText;

    private float FAR_X = 9.61f;

    private FishData caughtFish = null;
    public AudioClip uiCloseAudio;
    AudioSource audioSource;

    void Awake()
    {
        instance = this;
    }

    public FishData PickFish()
    {
        // Check story conditions
        FishData bodyPart = CheckStoryConditions();
        if (bodyPart != null)
        {
            caughtFish = bodyPart;
            return bodyPart;
        }
        StatsStorage statsStorage = StatsStorage.instance;
        allFishable = fish.Concat(fish)
        .Concat(statsStorage.severedHandEventPassed ? Enumerable.Empty<FishData>() : fish)
        .Union(statsStorage.severedHandEventPassed ? Enumerable.Empty<FishData>() : junk)
        .Concat(statsStorage.severedHandEventPassed ? bodyParts : Enumerable.Empty<FishData>())
        .Concat(statsStorage.severedHandEventPassed ? bodyParts : Enumerable.Empty<FishData>())
        .Concat(statsStorage.severedHandEventPassed && statsStorage.IsFullyUpGraded() ? bodyParts : Enumerable.Empty<FishData>())
        .Concat(statsStorage.severedHandEventPassed && statsStorage.IsFullyUpGraded() ? bodyParts : Enumerable.Empty<FishData>())
        //.Concat(statsStorage.severedHandEventPassed ? bodyParts : Enumerable.Empty<FishData>())
        .Where(x =>
        {
            bool doPickFish = x.rodRequirement <= statsStorage.rodLevel && x.baitRequirement <= statsStorage.baitLevel;
            if (PlayerManager.instance.transform.position.x < FAR_X)
            {
                doPickFish = doPickFish && x.location == FishData.LOCATION.NEAR;
            }
            if (statsStorage.severedHandEventPassed)
            {
                if (x.name.Equals("Arm"))
                {
                    doPickFish &= !statsStorage.armFound;
                }
                if (x.name.Equals("Leg"))
                {
                    doPickFish &= !statsStorage.legFound;
                }
                if (x.name.Equals("Male Rotting Head"))
                {
                    doPickFish &= !statsStorage.maleHeadFound;
                }
                if (x.name.Equals("Organ Necklace"))
                {
                    doPickFish &= !statsStorage.organNecklaceFound;
                }
            }
            return doPickFish;
        })
        .ToArray();
        int randIdx = UnityEngine.Random.Range(0, allFishable.Length);
        caughtFish = allFishable[randIdx];
        return caughtFish;
    }

    FishData CheckStoryConditions()
    {
        StoryEventManager storyEventManager = StoryEventManager.instance;
        if (storyEventManager.severedHandEventPlay)
        {
            return severedHand;
        }
        return null;
    }

    public FishData CaughtFish()
    {
        //Update UI
        fishSprite.sprite = caughtFish.sprite;
        nameText.text = caughtFish.title;
        descriptionText.text = caughtFish.description;
        priceText.text = caughtFish.sellingPrice + "";
        fishUI.SetActive(true);

        PlayerManager.instance.state = PlayerManager.PLAYER_STATES.UI_DISPLAY;
        // Update inventory
        return caughtFish;
    }

    public void CloseUI()
    {
        fishUI.SetActive(false);
        if (caughtFish.name.Equals("Rotting Hand"))
        {
            String[] firstDiscovery = { "What the... hell is this?"
            , "Oh my god... why in God's name did Dan send me to this place?"
            , "I need to leave." };
            DialogManager.instance.StartDialog(firstDiscovery, false);
            PlayerManager.instance.isScared = true;
            return;
        }
        if (caughtFish.name.Equals("Arm"))
        {
            String[] armDiscovery = { "It was the arm of a slender woman and it looked much recent that the other parts."
            , "Why am I doing this to myself?" };
            StatsStorage.instance.armFound = true;
            StatsStorage.instance.partsFound++;
            PlayerManager.instance.UpdatePartsFound();
            DialogManager.instance.StartDialog(armDiscovery, false);
            return;
        }
        if (caughtFish.name.Equals("Leg"))
        {
            String[] legDiscovery = { "The stentch is unbearable.", "Why did I come back here?" };
            StatsStorage.instance.legFound = true;
            StatsStorage.instance.partsFound++;
            PlayerManager.instance.UpdatePartsFound();
            DialogManager.instance.StartDialog(legDiscovery, false);
            return;
        }
        if (caughtFish.name.Equals("Organ Necklace"))
        {
            String[] organDiscovery = { "It was a horrible necklace. But I saw the beauty behind the rot." };
            StatsStorage.instance.organNecklaceFound = true;
            StatsStorage.instance.partsFound++;
            PlayerManager.instance.UpdatePartsFound();
            DialogManager.instance.StartDialog(organDiscovery, false);
            return;
        }
        if (caughtFish.name.Equals("Male Rotting Head"))
        {
            String[] headDiscovery = { "The head had decomposed beyond recognition.",
                "I felt like throwing up but I also felt happy." };
            StatsStorage.instance.maleHeadFound = true;
            StatsStorage.instance.partsFound++;
            PlayerManager.instance.UpdatePartsFound();
            DialogManager.instance.StartDialog(headDiscovery, false);
            return;
        }
        if (caughtFish.name.Equals("Wife Torso"))
        {
            String[] torsoDiscovery = { "Looks familiar....", "Is that... is that Mary?" };
            DialogManager.instance.StartDialog(torsoDiscovery, true);
            return;
        }
        PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.IDLE);
        PlayerManager.instance.CaughtFishThought(caughtFish);
        audioSource.clip = uiCloseAudio;
        audioSource.Play();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {

    }
}
