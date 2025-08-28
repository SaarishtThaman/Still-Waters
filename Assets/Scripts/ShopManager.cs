using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public Sprite tent, highlightedTent;
    public GameObject tentMenuButtonObj, tentUI;
    private AudioSource audioSource;
    public GameObject sellAllButton, endDayButton, upgradeEngineButton, upgradeRodButton, upgradeBaitButton, upgradeInventoryButton;
    public AudioClip sellAllSound, uiOpen, uiClose, purchaseSound;
    public TextMeshProUGUI cash, engineLevel, engineCost, rodLevel, rodCost, baitLevel, baitCost, inventoryLevel, inventoryCost;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerManager>() != null &&
        (other.gameObject.GetComponent<PlayerManager>().state == PlayerManager.PLAYER_STATES.IDLE ||
        other.gameObject.GetComponent<PlayerManager>().state == PlayerManager.PLAYER_STATES.MOVING))
        {
            tentMenuButtonObj.SetActive(true);
            GetComponent<SpriteRenderer>().sprite = highlightedTent;
        }
        else
        {
            tentMenuButtonObj.SetActive(false);
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerManager>() != null)
        {
            tentMenuButtonObj.SetActive(false);
            GetComponent<SpriteRenderer>().sprite = tent;
        }
    }

    public void OpenTentMenu()
    {
        tentMenuButtonObj.SetActive(false);
        PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.UI_DISPLAY);
        audioSource.clip = uiOpen;
        audioSource.Play();
        tentUI.SetActive(true);
        UpdateUI();
    }

    public void CloseTentMenu()
    {
        PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.IDLE);
        audioSource.clip = uiClose;
        audioSource.Play();
        tentUI.SetActive(false);
    }

    void UpdateUI()
    {
        PlayerManager playerManager = PlayerManager.instance;
        if (playerManager.GetCaughtCount() > 0)
        {
            sellAllButton.SetActive(true);
        }
        else
        {
            sellAllButton.SetActive(false);
        }

        cash.text = "" + StatsStorage.instance.cash;
        if (DayNightCycle.instance.isNightTime() || playerManager.isScared)
        {
            endDayButton.SetActive(true);
        }
        else
        {
            endDayButton.SetActive(false);
        }
        StatsStorage statsStorage = StatsStorage.instance;

        if (statsStorage.GetEngineCost() == -1)
        {
            engineLevel.text = "Engine (Lvl MAX)";
            upgradeEngineButton.SetActive(false);
        }
        else
        {
            engineLevel.text = "Engine (Lvl " + (statsStorage.engineLevel + 1) + ")";
            engineCost.text = "" + statsStorage.engineCosts[statsStorage.engineLevel];
        }
        if (statsStorage.GetBaitCost() == -1)
        {
            baitLevel.text = "Bait (Lvl MAX)";
            upgradeBaitButton.SetActive(false);
        }
        else
        {
            baitLevel.text = "Bait (Lvl " + (statsStorage.baitLevel + 1) + ")";
            baitCost.text = "" + statsStorage.baitCosts[statsStorage.baitLevel];
        }
        if (statsStorage.GetRodCost() == -1)
        {
            rodLevel.text = "Fishing Rod (Lvl MAX)";
            upgradeRodButton.SetActive(false);
        }
        else
        {
            rodLevel.text = "Fishing Rod (Lvl " + (statsStorage.rodLevel + 1) + ")";
            rodCost.text = "" + statsStorage.rodCosts[statsStorage.rodLevel];
        }
        if (statsStorage.GetInventoryCost() == -1)
        {
            inventoryLevel.text = "Inventory (Lvl MAX)";
            upgradeInventoryButton.SetActive(false);
        }
        else
        {
            inventoryLevel.text = "Inventory (Lvl " + (statsStorage.inventoryLevel + 1) + ")";
            inventoryCost.text = "" + statsStorage.inventoryCosts[statsStorage.inventoryLevel];
        }
    }

    public void SellAll()
    {
        PlayerManager playerManager = PlayerManager.instance;
        int caughtCount = playerManager.GetCaughtCount();
        if (caughtCount > 0)
        {
            FishData[] inventory = playerManager.GetInventory();
            int totalCost = 0;
            for (int i = 0; i < caughtCount; i++)
            {
                totalCost += inventory[i].sellingPrice;
            }
            StatsStorage.instance.cash += totalCost;
            playerManager.ResetInventory();
        }
        UpdateUI();
        audioSource.clip = sellAllSound;
        audioSource.Play();
    }

    public bool IsFullyUpGraded()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        return statsStorage.GetEngineCost() == -1 && statsStorage.GetInventoryCost() == -1
        && statsStorage.GetBaitCost() == -1 && statsStorage.GetRodCost() == -1;
    }

    public void UpgradeEngine()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        if (statsStorage.cash >= statsStorage.GetEngineCost())
        {
            statsStorage.cash -= statsStorage.GetEngineCost();
            statsStorage.engineLevel++;
            statsStorage.moveSpeed = statsStorage.moveSpeeds[statsStorage.engineLevel];
            UpdateUI();
            audioSource.clip = purchaseSound;
            audioSource.Play();
        }
    }

    public void UpgradeRod()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        if (statsStorage.cash >= statsStorage.GetRodCost())
        {
            statsStorage.cash -= statsStorage.GetRodCost();
            statsStorage.rodLevel++;
            statsStorage.stringLength = statsStorage.stringLengths[statsStorage.rodLevel];
            audioSource.clip = purchaseSound;
            audioSource.Play();
            UpdateUI();
        }
    }

    public void UpgradeBait()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        if (statsStorage.cash >= statsStorage.GetBaitCost())
        {
            statsStorage.cash -= statsStorage.GetBaitCost();
            statsStorage.baitLevel++;
            statsStorage.meanWaitTime = statsStorage.meanWaitTimes[statsStorage.baitLevel];
            audioSource.clip = purchaseSound;
            audioSource.Play();
            UpdateUI();
        }
    }

    public void UpgradeInventory()
    {
        StatsStorage statsStorage = StatsStorage.instance;
        if (statsStorage.cash >= statsStorage.GetInventoryCost())
        {
            statsStorage.cash -= statsStorage.GetInventoryCost();
            statsStorage.inventoryLevel++;
            FishData[] oldInventory = statsStorage.inventory;
            statsStorage.inventorySpace = statsStorage.inventorySpaces[statsStorage.inventoryLevel];
            FishData[] newInventory = new FishData[statsStorage.inventorySpace];
            for (int i = 0; i < oldInventory.Length; i++)
            {
                newInventory[i] = oldInventory[i];
            }
            statsStorage.inventory = newInventory;
            PlayerManager.instance.UpdateInventoryText();
            audioSource.clip = purchaseSound;
            audioSource.Play();
            UpdateUI();
        }
    }
}
