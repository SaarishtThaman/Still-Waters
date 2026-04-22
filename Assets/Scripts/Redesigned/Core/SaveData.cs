// Plain C# class — no MonoBehaviour, no Unity dependency.
// Serializable so it can be written to disk in the future.
[System.Serializable]
public class SaveData
{
    // Economy
    public int cash = 5;

    // World state
    public int dayNumber = 0;

    // Upgrade levels (index into UpgradeConfigSO arrays)
    public int engineLevel    = 0;
    public int rodLevel       = 0;
    public int baitLevel      = 0;
    public int inventoryLevel = 0;

    // Story flags
    public bool severedHandEventPassed = false;
    public bool armFound           = false;
    public bool legFound           = false;
    public bool organNecklaceFound = false;
    public bool maleHeadFound      = false;
    public int  partsFound         = 0;
    public int  totalParts         = 6;

    // Inventory — stored as parallel arrays so it stays serializable
    // without needing FishDataSO references at runtime.
    public int      caughtCount     = 0;
    public string[] inventoryTitles = new string[0];
    public int[]    inventoryPrices = new int[0];
}
