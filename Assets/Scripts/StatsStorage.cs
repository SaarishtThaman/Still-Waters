using Unity.VisualScripting;
using UnityEngine;

public class StatsStorage : MonoBehaviour
{

    // UPGRADE DATA
    [HideInInspector]
    [System.NonSerialized]
    public float[] moveSpeeds = { 1f, 2f, 3f };
    [HideInInspector]
    [System.NonSerialized]
    public float[] stringLengths = { 1.75f, 2.25f, 3.25f };
    [HideInInspector]
    [System.NonSerialized]
    public int[] meanWaitTimes = { 17, 13, 8 };
    [HideInInspector]
    [System.NonSerialized]
    public int[] inventorySpaces = { 3, 5, 8 };

    [HideInInspector]
    [System.NonSerialized]
    public int engineLevel = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int rodLevel = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int baitLevel = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int inventoryLevel = 0;

    [HideInInspector]
    [System.NonSerialized]
    public int[] engineCosts = { 60, 70 };
    [HideInInspector]
    [System.NonSerialized]
    public int[] rodCosts = { 75, 80 };
    [HideInInspector]
    [System.NonSerialized]
    public int[] baitCosts = { 50, 75 };
    [HideInInspector]
    [System.NonSerialized]
    public int[] inventoryCosts = { 20, 70 };

    // PLAYER DATA
    [HideInInspector]
    [System.NonSerialized]
    public float moveSpeed;
    [HideInInspector]
    [System.NonSerialized]
    public float stringLength;
    [HideInInspector]
    [System.NonSerialized]
    public int meanWaitTime;
    [HideInInspector]
    [System.NonSerialized]
    public int inventorySpace;
    [HideInInspector]
    [System.NonSerialized]
    public int caughtCount = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int cash = 5;
    [HideInInspector]
    [System.NonSerialized]
    public FishData[] inventory;


    // WORLD DATA
    [HideInInspector]
    [System.NonSerialized]
    public int dayNumber = 0;
    [HideInInspector]
    [System.NonSerialized]
    public bool severedHandEventPassed = false;

    [HideInInspector]
    [System.NonSerialized]
    public bool maleHeadFound = false, legFound = false, armFound = false, organNecklaceFound = false;

    [HideInInspector]
    [System.NonSerialized]
    public int partsFound = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int totalParts = 6;

    public static StatsStorage instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        inventorySpace = inventorySpaces[inventoryLevel];
        inventory = new FishData[inventorySpace];
        moveSpeed = moveSpeeds[engineLevel];
        stringLength = stringLengths[rodLevel];
        meanWaitTime = meanWaitTimes[baitLevel];
    }

    public int GetEngineCost()
    {
        if (engineLevel >= engineCosts.Length) return -1;
        return engineCosts[engineLevel];
    }

    public int GetRodCost()
    {
        if (rodLevel >= rodCosts.Length) return -1;
        return rodCosts[rodLevel];
    }

    public int GetBaitCost()
    {
        if (baitLevel >= baitCosts.Length) return -1;
        return baitCosts[baitLevel];
    }

    public int GetInventoryCost()
    {
        if (inventoryLevel >= inventoryCosts.Length) return -1;
        return inventoryCosts[inventoryLevel];
    }

    public bool IsFullyUpGraded()
    {
        return GetEngineCost() == -1 && GetInventoryCost() == -1
        && GetBaitCost() == -1 && GetRodCost() == -1;
    }

    void Start()
    {
    }
}
