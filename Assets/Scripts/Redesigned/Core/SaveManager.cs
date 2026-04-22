using UnityEngine;

// The one legitimate singleton: it owns persistent state that must survive
// scene loads. Every other system receives a reference via the Inspector.
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [SerializeField] UpgradeConfigSO upgradeConfig;

    public SaveData data = new SaveData();

    // Computed current values — derived from upgrade level, never stored separately.
    public float MoveSpeed     => upgradeConfig.moveSpeeds[data.engineLevel];
    public float StringLength  => upgradeConfig.stringLengths[data.rodLevel];
    public int   MeanWaitTime  => upgradeConfig.meanWaitTimes[data.baitLevel];
    public int   InventorySpace => upgradeConfig.inventorySpaces[data.inventoryLevel];
    public bool  IsInventoryFull => data.caughtCount >= InventorySpace;

    public bool IsFullyUpgraded => upgradeConfig.IsFullyUpgraded(
        data.engineLevel, data.rodLevel, data.baitLevel, data.inventoryLevel);

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddToInventory(FishDataSO fish)
    {
        var titles = new string[data.inventoryTitles.Length + 1];
        var prices = new int[data.inventoryPrices.Length + 1];
        data.inventoryTitles.CopyTo(titles, 0);
        data.inventoryPrices.CopyTo(prices, 0);
        titles[titles.Length - 1] = fish.title;
        prices[prices.Length - 1] = fish.sellingPrice;
        data.inventoryTitles = titles;
        data.inventoryPrices = prices;
        data.caughtCount++;
    }

    public int SellInventory()
    {
        int total = 0;
        foreach (int price in data.inventoryPrices) total += price;
        data.caughtCount     = 0;
        data.inventoryTitles = new string[0];
        data.inventoryPrices = new int[0];
        return total;
    }

    public int GetUpgradeCost(int[] costs, int level)
        => upgradeConfig.GetCost(costs, level);
}
