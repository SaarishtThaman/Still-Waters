using UnityEngine;

// Scene names for the full story flow. No more magic build indices.
// Must match the exact scene names in File > Build Settings.
[CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "Still Waters/Scene Flow Config")]
public class SceneFlowConfigSO : ScriptableObject
{
    public string introScene;
    public string tutorialScene;
    public string mainGameScene;
    public string firstNightScene;
    public string horrorEventScene;
    public string postHorrorScene;
    public string penultimateScene;
    public string endScene;
}
