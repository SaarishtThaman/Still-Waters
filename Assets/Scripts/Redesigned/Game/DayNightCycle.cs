using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Visual day/night transition. Raises state via property — no singleton needed.
// Other systems (PlayerController, ShopController) receive a reference in the Inspector.
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] SaveManager       saveManager;
    [SerializeField] GameFlowController gameFlow;
    [SerializeField] SceneFlowConfigSO sceneFlow;
    [SerializeField] SpriteRenderer    nightOverlay;
    [SerializeField] AudioClip         nightAudio;

    public bool IsNightTime { get; private set; }

    private AudioSource audioSource;
    private float       alpha;
    private bool        nightAudioStarted;

    static readonly Color NightTint = new Color(9f / 255f, 8f / 255f, 34f / 255f);

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        string scene = SceneManager.GetActiveScene().name;
        if (scene != sceneFlow.penultimateScene)
            StartCoroutine(FadeToNight());
    }

    IEnumerator FadeToNight()
    {
        while (alpha < 0.72f)
        {
            yield return new WaitForSeconds(1f);
            alpha += 0.006f;
            nightOverlay.color = new Color(NightTint.r, NightTint.g, NightTint.b, alpha);
        }
        IsNightTime = true;
    }

    void Update()
    {
        if (!nightAudioStarted && alpha >= 0.6f)
        {
            nightAudioStarted = true;
            audioSource.clip  = nightAudio;
            audioSource.loop  = true;
            audioSource.Play();
        }
    }

    // Called by the End Day button in the Inspector.
    public void EndDay()
    {
        saveManager.data.dayNumber++;
        gameFlow.PlayNextEvent(isScared: false);
    }
}
