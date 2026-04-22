using System;
using System.Collections;
using UnityEngine;

// Owns the full fishing state machine and all fishing mechanics.
// Communicates results outward only via C# events — never calls back into
// PlayerController, UI, or story systems directly.
public class FishingController : MonoBehaviour
{
    enum FishingState { Idle, Casting, FishBit, Minigame, ReelingIn, FishCaught }

    [Header("Scene References")]
    [SerializeField] SaveManager saveManager;
    [SerializeField] FishingLogic fishingLogic;
    [SerializeField] FishingMinigame minigame;

    [Header("Fishing Setup")]
    [SerializeField] Transform hookSpawn;
    [SerializeField] SpringJoint2D springJoint2D;
    [SerializeField] GameObject hookPrefab;
    [SerializeField] GameObject exclamation;

    [Header("Audio")]
    [SerializeField] AudioClip throwStringClip;
    [SerializeField] AudioClip reelInClip;
    [SerializeField] AudioClip fishBitClip;

    // PlayerController subscribes to these — no singleton calls needed.
    public event Action<FishDataSO> onFishCaught;
    public event Action             onFishLost;

    private FishingState state = FishingState.Idle;
    private GameObject   hook;
    private GameObject   lineObj;
    private LineRenderer  lr;
    private AudioSource  audioSource;
    private float        switchTimer;
    private float        fishTimer;
    private FishDataSO   selectedFish;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        exclamation.SetActive(false);
    }

    void Start()
    {
        minigame.onWin  += OnMinigameWon;
        minigame.onLoss += OnMinigameLost;
    }

    public void StartFishing()
    {
        int mean = saveManager.MeanWaitTime;
        fishTimer   = UnityEngine.Random.Range(mean - 3, mean + 3);
        switchTimer = 3f;
        state       = FishingState.Casting;
        audioSource.PlayOneShot(throwStringClip);
        CastReel();
    }

    void Update()
    {
        switch (state)
        {
            case FishingState.Casting:
                switchTimer -= Time.deltaTime;
                fishTimer   -= Time.deltaTime;
                UpdateLine();

                if (fishTimer <= 0)
                {
                    EnterFishBit();
                }
                else if (Input.GetButtonDown("Jump") && switchTimer <= 0)
                {
                    StartReelIn(caught: false);
                }
                break;

            case FishingState.FishBit:
                UpdateLine();
                if (Input.GetButtonDown("Jump"))
                {
                    exclamation.SetActive(false);
                    StopAllCoroutines();
                    selectedFish = fishingLogic.PickFish(transform.position.x);
                    state        = FishingState.Minigame;
                    minigame.StartMiniGame(selectedFish);
                }
                break;

            case FishingState.Minigame:
                UpdateLine();
                break;
        }
    }

    void EnterFishBit()
    {
        state = FishingState.FishBit;
        exclamation.SetActive(true);
        audioSource.PlayOneShot(fishBitClip);
        StartCoroutine(FishBitCoroutine());
    }

    void OnMinigameWon()
    {
        state = FishingState.FishCaught;
        StartReelIn(caught: true);
    }

    void OnMinigameLost()
    {
        selectedFish = null;
        StartReelIn(caught: false);
    }

    void StartReelIn(bool caught)
    {
        state = FishingState.ReelingIn;
        audioSource.clip = reelInClip;
        audioSource.loop = true;
        audioSource.Play();
        StartCoroutine(ReelInCoroutine(caught ? 1.5f : 2.5f, caught));
    }

    IEnumerator ReelInCoroutine(float speed, bool caught)
    {
        if (hook != null)
        {
            var hookRb = hook.GetComponent<Rigidbody2D>();
            hookRb.bodyType = RigidbodyType2D.Kinematic;
        }

        while (hook != null && Vector2.Distance(hook.transform.position, hookSpawn.position) > 0.1f)
        {
            hook.transform.position = Vector2.MoveTowards(
                hook.transform.position, hookSpawn.position, speed * Time.deltaTime);
            UpdateLine();
            yield return null;
        }

        Destroy(hook);
        Destroy(lineObj);
        hook    = null;
        lineObj = null;
        audioSource.Stop();
        audioSource.loop = false;
        state = FishingState.Idle;

        if (caught)
        {
            saveManager.AddToInventory(selectedFish);
            onFishCaught?.Invoke(selectedFish);
        }
        else
        {
            onFishLost?.Invoke();
        }
    }

    IEnumerator FishBitCoroutine()
    {
        float force = saveManager.StringLength / 1.25f;
        while (true)
        {
            var hookRb = hook.GetComponent<Rigidbody2D>();
            hookRb.AddForce(Vector2.right * force, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.25f);
            hookRb.AddForce(Vector2.left  * force, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.25f);
        }
    }

    void CastReel()
    {
        hook = Instantiate(hookPrefab, hookSpawn.position, Quaternion.identity);

        lineObj = new GameObject("FishingLine");
        lr = lineObj.AddComponent<LineRenderer>();
        lr.startColor  = Color.white;
        lr.endColor    = Color.white;
        lr.material    = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth  = 0.025f;
        lr.endWidth    = 0.025f;
        lr.positionCount = 2;

        var hookRb = hook.GetComponent<Rigidbody2D>();
        hookRb.AddForce(new Vector2(1f, 1f), ForceMode2D.Impulse);
        springJoint2D.distance      = saveManager.StringLength;
        springJoint2D.connectedBody = hookRb;
    }

    void UpdateLine()
    {
        if (lr == null || hook == null) return;
        lr.SetPosition(0, hookSpawn.position);
        lr.SetPosition(1, hook.transform.position);
    }
}
