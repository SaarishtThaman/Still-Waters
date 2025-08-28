using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle instance;
    public GameObject dayNightCycleObj;
    private SpriteRenderer sr;
    private float alpha = 0f;
    private bool NightTime = false;
    AudioSource audioSource;
    public AudioClip nightTimeAudio;

    void Start()
    {
        instance = this;
        audioSource = dayNightCycleObj.GetComponent<AudioSource>();
        sr = dayNightCycleObj.GetComponent<SpriteRenderer>();
        if (SceneManager.GetActiveScene().buildIndex != 7) StartCoroutine(UpdateAlpha());
    }

    public bool isNightTime()
    {
        return NightTime;
    }

    public void EndDay()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        statsStorage.dayNumber++;
        GameManager.instance.PlayNextEvent();
    }

    IEnumerator UpdateAlpha()
    {
        while (alpha < 0.72f)
        {
            yield return new WaitForSeconds(1f);
            alpha += 0.006f;
            sr.color = new Color(9f / 255, 8f / 255, 34f / 255, alpha);
        }
        NightTime = true;
    }

    void Update()
    {
        if (alpha >= 0.6f && !audioSource.isPlaying)
        {
            audioSource.clip = nightTimeAudio;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}
