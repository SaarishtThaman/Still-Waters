using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public static PlayerManager instance;
    public GameObject exclamation;

    private Rigidbody2D rb;

    public TextMeshProUGUI inventoryText, thoughts, partsFoundPopupText;

    public Transform hookSpawn;
    public SpringJoint2D springJoint2D;
    public GameObject hookPrefab;

    private bool stateComplete = false;

    // TIMERS
    private float switchMaxTime = 3f;
    private float switchTimer = 0f;
    private float fishTimer = 0f;

    private GameObject fishingLine;
    private LineRenderer lr;
    private GameObject hook;

    public enum PLAYER_STATES
    {
        UI_DISPLAY,
        IDLE,
        MOVING,
        FISHING_STARTED,
        FISH_BIT,
        FISH_CAUGHT,
        REELING_IN,
        REEL_IN_FISH
    }

    public PLAYER_STATES state;

    // AUDIO
    public GameObject soundPlayer;
    private AudioSource audioSource;
    public AudioClip reelIn;
    public AudioClip throwString;
    public AudioClip fishBitExclamation, fishCaughtSound, horrorCaughtSound;

    private bool isThinking = false;
    public bool isScared;

    private StatsStorage statsStorage;

    private String[] lateDayDialogs = { "Its getting late. I should call it a day.",
        "I should go to sleep in my tent.",
        "I can't fish at night.",
        "Can't see a thing. I should just go to sleep." };

    private String[] inventoryFullDialogs = { "No more space on my boat...",
        "I should sell all my stuff first...",
        "My inventory is full. Need to sell off what I have." };

    private String[] lostFishDialogs = { "Shit, it got away.",
        "Fuck, I almost had it.",
        "Damn it..." };

    public void UpdatePartsFound()
    {
        // Some coroutine to show popup text that disappears. An audio clip would be nice as well.
        partsFoundPopupText.text = StatsStorage.instance.partsFound + "/" + StatsStorage.instance.totalParts;
    }

    void Awake()
    {
        exclamation.SetActive(false);
        instance = this;
    }

    void Start()
    {
        statsStorage = StatsStorage.instance;
        audioSource = soundPlayer.GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        state = PLAYER_STATES.IDLE;
        stateComplete = true;
        UpdateInventoryText();
        UpdatePartsFound();
    }

    public void LostFishThought()
    {
        int idx = UnityEngine.Random.Range(0, lostFishDialogs.Length);
        StartCoroutine(updateThoughts(lostFishDialogs[idx], 0.5f));
    }

    public void CaughtFishThought(FishData caughtFish)
    {
        if (caughtFish.type == FishData.TYPE.BODYPART) return;
        String toThink = "";
        if (caughtFish.type == FishData.TYPE.FISH)
            toThink = "Good catch...";
        else if (caughtFish.sellingPrice > 5 && caughtFish.type == FishData.TYPE.JUNK)
            toThink = "At least this is not total junk.";
        else toThink = "Well, at least I caught something...";
        StartCoroutine(updateThoughts(toThink, 0f));
    }

    IEnumerator updateThoughts(String s, float waitTime)
    {
        if (isThinking) yield break;
        isThinking = true;
        yield return new WaitForSeconds(waitTime);
        thoughts.text = "";
        foreach (char letter in s)
        {
            thoughts.text += letter;
            yield return new WaitForSeconds(0.045f);
        }
        yield return new WaitForSeconds(1.5f);
        while (!string.IsNullOrEmpty(thoughts.text))
        {
            thoughts.text = thoughts.text.Substring(0, thoughts.text.Length - 1);
            yield return new WaitForSeconds(0.03f);
        }
        isThinking = false;
    }

    void Update()
    {
        if (stateComplete)
        {
            stateComplete = false;
            SwitchState();
        }

        UpdateState();
    }

    void SwitchState()
    {
        switch (state)
        {
            case PLAYER_STATES.FISHING_STARTED:
                audioSource.clip = throwString;
                audioSource.loop = false;
                audioSource.Play();
                switchTimer = switchMaxTime;
                CastReel();
                break;
            case PLAYER_STATES.REELING_IN:
                audioSource.clip = reelIn;
                audioSource.loop = true;
                audioSource.Play();
                StartCoroutine(ReelInCoroutine(2.5f, false));
                break;
            case PLAYER_STATES.FISH_BIT:
                exclamation.SetActive(true);
                audioSource.clip = fishBitExclamation;
                audioSource.loop = false;
                audioSource.Play();
                StartCoroutine(FishBitCoroutine());
                break;
            case PLAYER_STATES.REEL_IN_FISH:
                exclamation.SetActive(false);
                FishingMinigame.instance.StartMiniGame();
                break;
            case PLAYER_STATES.IDLE:
                audioSource.Stop();
                break;
            case PLAYER_STATES.MOVING:
                break;
            case PLAYER_STATES.FISH_CAUGHT:
                audioSource.clip = reelIn;
                audioSource.loop = true;
                audioSource.Play();
                StartCoroutine(ReelInCoroutine(1.5f, true));
                break;
        }
    }

    public void SetState(PLAYER_STATES stateToSet)
    {
        state = stateToSet;
        stateComplete = true;
    }

    public void UpdateInventoryText()
    {
        inventoryText.text = statsStorage.caughtCount + "/" + statsStorage.inventorySpace;
    }

    IEnumerator FishBitCoroutine()
    {
        float forceMagnitue = statsStorage.stringLength / 1.25f;
        while (true)
        {
            Rigidbody2D hookRb = hook.GetComponent<Rigidbody2D>();
            hookRb.AddForce(Vector2.right * forceMagnitue, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.25f);
            hookRb.AddForce(Vector2.left * forceMagnitue, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void ResetInventory()
    {
        statsStorage.caughtCount = 0;
        statsStorage.inventory = new FishData[statsStorage.inventorySpace];
        UpdateInventoryText();
    }

    void CastReel()
    {
        hook = Instantiate(hookPrefab, hookSpawn);
        fishingLine = new GameObject("Fishing Line");
        fishingLine.transform.SetParent(null);
        lr = fishingLine.AddComponent<LineRenderer>();
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.025f;
        lr.endWidth = 0.025f;
        lr.positionCount = 2;

        lr.SetPosition(0, hookSpawn.position);
        lr.SetPosition(1, hook.transform.position);

        lr.enabled = true;
        Rigidbody2D hookRb = hook.GetComponent<Rigidbody2D>();
        hookRb.AddForce(new Vector2(1f, 1f), ForceMode2D.Impulse);
        springJoint2D.distance = statsStorage.stringLength;
        springJoint2D.connectedBody = hookRb;

        fishTimer = UnityEngine.Random.Range(statsStorage.meanWaitTime - 3, statsStorage.meanWaitTime + 3);
    }

    void UpdateState()
    {
        switch (state)
        {
            case PLAYER_STATES.IDLE:
                if (Input.GetAxisRaw("Horizontal") != 0)
                {
                    stateComplete = true;
                    state = PLAYER_STATES.MOVING;
                }
                if (Input.GetAxisRaw("Jump") != 0)
                {
                    if (isScared)
                    {
                        StartCoroutine(updateThoughts("I need to leave.", 0f));
                    }
                    else if (DayNightCycle.instance.isNightTime())
                    {
                        int idx = UnityEngine.Random.Range(0, lateDayDialogs.Length);
                        StartCoroutine(updateThoughts(lateDayDialogs[idx], 0f));
                    }
                    else if (statsStorage.caughtCount < statsStorage.inventorySpace)
                    {
                        stateComplete = true;
                        state = PLAYER_STATES.FISHING_STARTED;
                    }
                    else
                    {
                        int idx = UnityEngine.Random.Range(0, inventoryFullDialogs.Length);
                        StartCoroutine(updateThoughts(inventoryFullDialogs[idx], 0f));
                    }
                }
                break;
            case PLAYER_STATES.MOVING:
                if (Input.GetAxisRaw("Horizontal") == 0)
                {
                    stateComplete = true;
                    state = PLAYER_STATES.IDLE;
                }
                break;
            case PLAYER_STATES.FISHING_STARTED:
                if (switchTimer > 0)
                {
                    switchTimer -= Time.deltaTime;
                }
                if (fishTimer > 0)
                {
                    fishTimer -= Time.deltaTime;
                }
                if (Input.GetAxisRaw("Jump") != 0 && switchTimer <= 0 && fishTimer >= 0)
                {
                    stateComplete = true;
                    state = PLAYER_STATES.REELING_IN;
                }
                if (fishTimer <= 0)
                {
                    stateComplete = true;
                    state = PLAYER_STATES.FISH_BIT;
                }
                UpdateFishingLine();
                break;
            case PLAYER_STATES.FISH_CAUGHT:
                break;
            case PLAYER_STATES.REELING_IN:
                break;
            case PLAYER_STATES.FISH_BIT:
                if (Input.GetAxisRaw("Jump") != 0)
                {
                    stateComplete = true;
                    state = PLAYER_STATES.REEL_IN_FISH;
                }
                UpdateFishingLine();
                break;
            case PLAYER_STATES.REEL_IN_FISH:
                UpdateFishingLine();
                break;
        }
    }


    IEnumerator ReelInCoroutine(float reelSpeed, bool isFishCaught)
    {
        Rigidbody2D rb = hook.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        while (hook != null && Vector2.Distance(hook.transform.position, hookSpawn.position) > 0.1f)
        {
            hook.transform.position = Vector2.MoveTowards(
                hook.transform.position,
                hookSpawn.position,
                reelSpeed * Time.deltaTime
            );

            UpdateFishingLine();
            yield return null;
        }

        Destroy(hook);
        hook = null;
        Destroy(fishingLine);
        stateComplete = true;
        audioSource.Stop();
        state = PLAYER_STATES.IDLE;
        if (isFishCaught)
        {
            FishData caughtFish = FishingManager.instance.CaughtFish();
            audioSource.clip = caughtFish.type == FishData.TYPE.BODYPART ? horrorCaughtSound : fishCaughtSound;
            if (caughtFish.type == FishData.TYPE.BODYPART)
            {
                BloodScreen.instance.PlayBloodScreen();
            }
            audioSource.loop = false;
            audioSource.Play();
            statsStorage.inventory[statsStorage.caughtCount] = caughtFish;
            statsStorage.caughtCount++;
            UpdateInventoryText();
        }
    }


    public int GetCaughtCount()
    {
        return statsStorage.caughtCount;
    }

    public FishData[] GetInventory()
    {
        return statsStorage.inventory;
    }

    void UpdateFishingLine()
    {
        lr.SetPosition(0, hookSpawn.position);
        lr.SetPosition(1, hook.transform.position);
    }

    void FixedUpdate()
    {
        if (state == PLAYER_STATES.MOVING)
        {
            rb.linearVelocity = new Vector2(Input.GetAxisRaw("Horizontal") * statsStorage.moveSpeed, rb.linearVelocityY);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
        }

    }
}
