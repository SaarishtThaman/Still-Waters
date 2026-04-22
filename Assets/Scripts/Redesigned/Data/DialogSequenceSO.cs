using UnityEngine;

// A sequence of dialog lines as a project asset.
// Writers can edit lines without touching any C# file.
[CreateAssetMenu(fileName = "NewDialog", menuName = "Still Waters/Dialog Sequence")]
public class DialogSequenceSO : ScriptableObject
{
    [TextArea(2, 6)]
    public string[] lines;
}
