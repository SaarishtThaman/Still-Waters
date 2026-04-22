using UnityEngine;

// All upgrade balance data lives here. Edit in the Inspector — no code changes needed.
[CreateAssetMenu(fileName = "UpgradeConfig", menuName = "Still Waters/Upgrade Config")]
public class UpgradeConfigSO : ScriptableObject
{
    public float[] moveSpeeds     = { 1f, 2f, 3f };
    public float[] stringLengths  = { 1.75f, 2.25f, 3.25f };
    public int[]   meanWaitTimes  = { 17, 13, 8 };
    public int[]   inventorySpaces = { 3, 5, 8 };

    public int[] engineCosts    = { 60, 70 };
    public int[] rodCosts       = { 75, 80 };
    public int[] baitCosts      = { 50, 75 };
    public int[] inventoryCosts = { 20, 70 };

    public int GetCost(int[] costs, int currentLevel)
        => currentLevel < costs.Length ? costs[currentLevel] : -1;

    public bool IsMaxLevel(int[] costs, int currentLevel)
        => currentLevel >= costs.Length;

    public bool IsFullyUpgraded(int engineLevel, int rodLevel, int baitLevel, int inventoryLevel)
        => IsMaxLevel(engineCosts, engineLevel)
        && IsMaxLevel(rodCosts, rodLevel)
        && IsMaxLevel(baitCosts, baitLevel)
        && IsMaxLevel(inventoryCosts, inventoryLevel);
}
