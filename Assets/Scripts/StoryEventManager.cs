using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StoryEventManager : MonoBehaviour
{
    public static StoryEventManager instance;
    public TextMeshProUGUI dayText;
    public bool severedHandEventPlay = false;
    AudioSource audioSource;
    public AudioClip seagull;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StatsStorage statsStorage = StatsStorage.instance;
        int dayNumber = statsStorage.dayNumber;
        if (dayText != null) dayText.text = "DAY " + (1 + dayNumber);
        if (dayNumber >= 2 && (statsStorage.rodLevel > 0 || statsStorage.engineLevel > 0 || statsStorage.baitLevel > 0))
        {
            severedHandEventPlay = true && !StatsStorage.instance.severedHandEventPassed;
            if (severedHandEventPlay)
            {
                audioSource.clip = seagull;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        if (SceneManager.GetActiveScene().buildIndex == 7)
        {
            audioSource.clip = seagull;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {

    }
}
