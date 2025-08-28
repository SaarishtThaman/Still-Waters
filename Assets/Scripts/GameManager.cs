using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
    }

    int getCurrentBuildIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }

    void Start()
    {
        // MAGIC NUMBER TIME
        if (getCurrentBuildIndex() == 1 || getCurrentBuildIndex() == 4
        || getCurrentBuildIndex() == 5 || getCurrentBuildIndex() == 6)
        {
            StartCoroutine(waitBeforeDialogStart(0.5f));
        }
    }

    public void PlayNextEvent()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        if (PlayerManager.instance != null && PlayerManager.instance.isScared)
        {
            statsStorage.severedHandEventPassed = true;
            statsStorage.partsFound++;
            SceneManager.LoadScene(5);
            return;
        }
        if (statsStorage.severedHandEventPassed)
        {
            if (getCurrentBuildIndex() == 5)
            {
                SceneManager.LoadScene(3);
            }
            if (getCurrentBuildIndex() == 3)
            {
                if (statsStorage.armFound && statsStorage.legFound
                && statsStorage.organNecklaceFound && statsStorage.maleHeadFound)
                {
                    SceneManager.LoadScene(6);
                }
                else
                {
                    SceneManager.LoadScene(3);
                }
            }
            if (getCurrentBuildIndex() == 6)
            {
                statsStorage.dayNumber += 10;
                SceneManager.LoadScene(7);
            }
            if (getCurrentBuildIndex() == 7)
            {
                SceneManager.LoadScene(8);
            }
        }
        else
        {
            // MAGIC NUMBERS HERE AS WELL
            if (getCurrentBuildIndex() == 1)
            {
                SceneManager.LoadScene(2);
            }
            if (getCurrentBuildIndex() == 3 && StatsStorage.instance.dayNumber == 1)
            {
                SceneManager.LoadScene(4);
            }
            if (getCurrentBuildIndex() == 3 && StatsStorage.instance.dayNumber > 1)
            {
                SceneManager.LoadScene(3);
            }
            if (getCurrentBuildIndex() == 4)
            {
                SceneManager.LoadScene(3);
            }
        }
    }

    IEnumerator waitBeforeDialogStart(float f)
    {
        yield return new WaitForSeconds(f);
        DialogManager.instance.StartDialog();
    }

    void Update()
    {

    }
}
