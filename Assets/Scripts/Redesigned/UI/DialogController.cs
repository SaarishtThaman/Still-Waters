using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Typewriter dialog display. Decoupled from game flow — it raises no events
// and calls no singletons. The caller passes in what happens when dialog ends.
public class DialogController : MonoBehaviour
{
    // Lightweight scene-local singleton: only DialogController accesses this,
    // and it is always present in every scene that has dialog.
    public static DialogController instance;

    [Header("UI")]
    [SerializeField] GameObject      dialogBox;
    [SerializeField] TextMeshProUGUI dialogText;

    [Header("Audio")]
    [SerializeField] AudioClip typingClip;
    [SerializeField] float     typingSpeed = 0.05f;

    [Header("Scene Default Dialog")]
    [SerializeField] DialogSequenceSO defaultDialog;
    [SerializeField] GameFlowController gameFlow;

    private readonly Queue<string> queue = new Queue<string>();
    private bool           isTyping;
    private bool           switchSceneOnEnd;
    private PlayerController activePlayer;
    private AudioSource    audioSource;

    void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
        if (dialogBox != null) dialogBox.SetActive(false);
    }

    // Called by GameFlowController on scenes that open with dialog.
    public void StartDefaultDialog()
        => StartDialog(defaultDialog, switchScene: true, player: null);

    public void StartDialog(DialogSequenceSO sequence, bool switchScene, PlayerController player)
    {
        switchSceneOnEnd = switchScene;
        activePlayer     = player;

        queue.Clear();
        foreach (string line in sequence.lines) queue.Enqueue(line);

        dialogBox.SetActive(true);
        if (player != null) player.SetUIDisplay(true);

        DisplayNextLine();
    }

    // Wired to the continue button in the Inspector.
    public void DisplayNextLine()
    {
        if (isTyping) return;
        if (queue.Count == 0) { EndDialog(); return; }

        string line = queue.Dequeue();
        audioSource.clip = typingClip;
        audioSource.loop = true;
        audioSource.Play();
        StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping        = true;
        dialogText.text = "";
        foreach (char c in line)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        audioSource.Stop();
        isTyping = false;
    }

    void EndDialog()
    {
        audioSource.Stop();
        dialogBox.SetActive(false);

        if (switchSceneOnEnd)
            gameFlow.PlayNextEvent(activePlayer != null && activePlayer.IsScared);
        else if (activePlayer != null)
            activePlayer.SetUIDisplay(false);
    }
}
