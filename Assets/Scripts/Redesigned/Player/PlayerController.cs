using System.Collections;
using TMPro;
using UnityEngine;

// Handles movement, input gating, and player-facing UI (thoughts, inventory text).
// Knows nothing about fishing outcomes or story — it just moves and reacts.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum State { UIDisplay, Idle, Moving }

    [Header("Scene References")]
    [SerializeField] SaveManager saveManager;
    [SerializeField] FishingController fishingController;
    [SerializeField] DayNightCycle dayNightCycle;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI inventoryText;
    [SerializeField] TextMeshProUGUI thoughtsText;
    [SerializeField] TextMeshProUGUI partsFoundText;

    [Header("Audio")]
    [SerializeField] AudioClip fishCaughtClip;
    [SerializeField] AudioClip horrorCaughtClip;

    [Header("Thoughts")]
    [SerializeField] DialogSequenceSO lateDayThoughts;
    [SerializeField] DialogSequenceSO inventoryFullThoughts;
    [SerializeField] DialogSequenceSO lostFishThoughts;
    [SerializeField] DialogSequenceSO goodCatchThoughts;
    [SerializeField] DialogSequenceSO okCatchThoughts;

    public State CurrentState { get; private set; } = State.Idle;
    public bool IsScared { get; set; }

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool isFishing;
    private bool isThinking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        fishingController.onFishCaught += HandleFishCaught;
        fishingController.onFishLost   += HandleFishLost;
        UpdateInventoryText();
        UpdatePartsFound();
    }

    void Update()
    {
        if (CurrentState == State.UIDisplay || isFishing) return;

        float h = Input.GetAxisRaw("Horizontal");
        CurrentState = h != 0 ? State.Moving : State.Idle;

        if (Input.GetButtonDown("Jump") && CurrentState == State.Idle)
            TryStartFishing();
    }

    void FixedUpdate()
    {
        if (CurrentState == State.UIDisplay || isFishing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        float h = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(h * saveManager.MoveSpeed, rb.linearVelocity.y);
    }

    void TryStartFishing()
    {
        if (IsScared)
        {
            ShowThought("I need to leave.", 0f);
            return;
        }
        if (dayNightCycle.IsNightTime)
        {
            ShowThought(Random(lateDayThoughts), 0f);
            return;
        }
        if (saveManager.IsInventoryFull)
        {
            ShowThought(Random(inventoryFullThoughts), 0f);
            return;
        }
        isFishing = true;
        fishingController.StartFishing();
    }

    void HandleFishCaught(FishDataSO fish)
    {
        isFishing = false;
        bool isBodyPart = fish.type == FishDataSO.FishType.BodyPart;
        audioSource.PlayOneShot(isBodyPart ? horrorCaughtClip : fishCaughtClip);
        UpdateInventoryText();
        // Thought shown after catch UI closes (CatchUIController calls ShowCaughtFishThought).
    }

    void HandleFishLost()
    {
        isFishing = false;
        ShowThought(Random(lostFishThoughts), 0.5f);
    }

    public void ShowCaughtFishThought(FishDataSO fish)
    {
        if (fish.type == FishDataSO.FishType.BodyPart) return;
        string thought = fish.type == FishDataSO.FishType.Fish
            ? Random(goodCatchThoughts)
            : Random(okCatchThoughts);
        ShowThought(thought, 0f);
    }

    public void SetUIDisplay(bool active)
        => CurrentState = active ? State.UIDisplay : State.Idle;

    public void UpdateInventoryText()
        => inventoryText.text = $"{saveManager.data.caughtCount}/{saveManager.InventorySpace}";

    public void UpdatePartsFound()
        => partsFoundText.text = $"{saveManager.data.partsFound}/{saveManager.data.totalParts}";

    public void ShowThought(string text, float delay)
        => StartCoroutine(ThoughtCoroutine(text, delay));

    IEnumerator ThoughtCoroutine(string text, float delay)
    {
        if (isThinking) yield break;
        isThinking = true;
        yield return new WaitForSeconds(delay);
        thoughtsText.text = "";
        foreach (char c in text)
        {
            thoughtsText.text += c;
            yield return new WaitForSeconds(0.045f);
        }
        yield return new WaitForSeconds(1.5f);
        while (thoughtsText.text.Length > 0)
        {
            thoughtsText.text = thoughtsText.text.Substring(0, thoughtsText.text.Length - 1);
            yield return new WaitForSeconds(0.03f);
        }
        isThinking = false;
    }

    string Random(DialogSequenceSO seq)
        => seq.lines[UnityEngine.Random.Range(0, seq.lines.Length)];
}
