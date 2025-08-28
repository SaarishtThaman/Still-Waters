using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.ComponentModel;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    public static DialogManager instance;
    AudioSource audioSource;
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public float typingSpeed = 0.05f;
    private bool isTyping = false;
    public GameObject continueButton;
    public AudioClip textAudio;
    public String[] dialogList;
    private Queue<String> dialogs;
    bool switchScene = true;

    void Awake()
    {
        instance = this;
        dialogs = new Queue<String>();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = textAudio;
        if (dialogBox != null) dialogBox.SetActive(false);
    }

    void Update()
    {
    }

    public void StartDialog()
    {
        dialogs.Clear();
        if (dialogBox != null) dialogBox.SetActive(true);
        foreach (String dialog in dialogList)
        {
            dialogs.Enqueue(dialog);
        }

        DisplayNextSentence();
    }

    public void StartDialog(String[] dialogList, bool isFinalScene)
    {
        switchScene = isFinalScene;
        PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.UI_DISPLAY);
        dialogs.Clear();
        if (dialogBox != null) dialogBox.SetActive(true);
        this.dialogList = dialogList;
        foreach (String dialog in dialogList)
        {
            dialogs.Enqueue(dialog);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isTyping) return;
        //continueButton.SetActive(false);

        if (dialogs.Count == 0)
        {
            EndDialog();
            return;
        }

        String dialog = dialogs.Dequeue();
        audioSource.Play();
        StartCoroutine(TypeSentence(dialog));
    }

    IEnumerator TypeSentence(String dialogObject)
    {
        isTyping = true;
        dialogText.text = "";
        foreach (char letter in dialogObject.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        audioSource.Stop();
        isTyping = false;
        //continueButton.SetActive(true);
    }

    public void EndDialog()
    {
        audioSource.Stop();
        if (dialogBox != null) dialogBox.SetActive(false);
        if (switchScene)
        {
            GameManager.instance.PlayNextEvent();
        }
        else
        {
            PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.IDLE);
        }
    }
}