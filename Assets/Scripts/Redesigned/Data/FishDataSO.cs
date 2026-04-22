using UnityEngine;

[CreateAssetMenu(fileName = "NewFish", menuName = "Still Waters/Fish Data")]
public class FishDataSO : ScriptableObject
{
    public string title;
    [TextArea(2, 4)] public string description;
    public Sprite sprite;
    public int difficulty = 1;
    public FishLocation location;
    public int sellingPrice;
    public int rodRequirement = 0;
    public int baitRequirement = 0;
    public FishType type;
    public BodyPartId bodyPartId = BodyPartId.None;

    public enum FishLocation { Near, Far }
    public enum FishType { Fish, Junk, BodyPart }

    // Used to track which body parts have been found without string comparisons.
    public enum BodyPartId
    {
        None,
        SeveredHand,
        Arm,
        Leg,
        OrganNecklace,
        MaleHead,
        WifeTorso
    }
}
