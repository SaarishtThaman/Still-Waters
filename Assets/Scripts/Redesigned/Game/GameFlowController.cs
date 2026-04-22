using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene transitions only. Reads story state from SaveManager; decides which
// scene to load. No magic build indices — all names come from SceneFlowConfigSO.
public class GameFlowController : MonoBehaviour
{
    [SerializeField] SceneFlowConfigSO sceneFlow;
    [SerializeField] SaveManager       saveManager;
    [SerializeField] DialogController  dialogController;

    void Start()
    {
        string current = SceneManager.GetActiveScene().name;
        bool needsDialog = current == sceneFlow.introScene
                        || current == sceneFlow.firstNightScene
                        || current == sceneFlow.horrorEventScene
                        || current == sceneFlow.postHorrorScene;

        if (needsDialog)
            StartCoroutine(DelayedDialogStart(0.5f));
    }

    IEnumerator DelayedDialogStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        dialogController.StartDefaultDialog();
    }

    // Called by DialogController.EndDialog and DayNightCycle.EndDay.
    public void PlayNextEvent(bool isScared)
    {
        var    save    = saveManager.data;
        string current = SceneManager.GetActiveScene().name;

        if (isScared)
        {
            save.severedHandEventPassed = true;
            SceneManager.LoadScene(sceneFlow.horrorEventScene);
            return;
        }

        if (save.severedHandEventPassed)
        {
            if (current == sceneFlow.horrorEventScene)
            {
                SceneManager.LoadScene(sceneFlow.mainGameScene);
            }
            else if (current == sceneFlow.mainGameScene)
            {
                bool allFound = save.armFound && save.legFound
                             && save.organNecklaceFound && save.maleHeadFound;
                SceneManager.LoadScene(allFound
                    ? sceneFlow.postHorrorScene
                    : sceneFlow.mainGameScene);
            }
            else if (current == sceneFlow.postHorrorScene)
            {
                save.dayNumber += 10;
                SceneManager.LoadScene(sceneFlow.penultimateScene);
            }
            else if (current == sceneFlow.penultimateScene)
            {
                SceneManager.LoadScene(sceneFlow.endScene);
            }
        }
        else
        {
            if (current == sceneFlow.introScene)
            {
                SceneManager.LoadScene(sceneFlow.tutorialScene);
            }
            else if (current == sceneFlow.tutorialScene)
            {
                SceneManager.LoadScene(sceneFlow.mainGameScene);
            }
            else if (current == sceneFlow.mainGameScene)
            {
                SceneManager.LoadScene(save.dayNumber == 1
                    ? sceneFlow.firstNightScene
                    : sceneFlow.mainGameScene);
            }
            else if (current == sceneFlow.firstNightScene)
            {
                SceneManager.LoadScene(sceneFlow.mainGameScene);
            }
        }
    }
}
