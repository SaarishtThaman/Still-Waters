using TMPro;
using UnityEngine;

// Shop UI and upgrades. Reads from SaveManager; writes upgrade levels back.
// No calls into PlayerController internals — just checks its state property.
public class ShopController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] SaveManager     saveManager;
    [SerializeField] PlayerController playerController;
    [SerializeField] DayNightCycle   dayNightCycle;
    [SerializeField] UpgradeConfigSO upgradeConfig;

    [Header("Tent")]
    [SerializeField] SpriteRenderer tentRenderer;
    [SerializeField] Sprite         tentNormal;
    [SerializeField] Sprite         tentHighlighted;
    [SerializeField] GameObject     tentMenuButton;

    [Header("Shop UI")]
    [SerializeField] GameObject     shopUI;
    [SerializeField] TextMeshProUGUI cashText;

    [SerializeField] GameObject     upgradeEngineButton;
    [SerializeField] TextMeshProUGUI engineLevelText;
    [SerializeField] TextMeshProUGUI engineCostText;

    [SerializeField] GameObject     upgradeRodButton;
    [SerializeField] TextMeshProUGUI rodLevelText;
    [SerializeField] TextMeshProUGUI rodCostText;

    [SerializeField] GameObject     upgradeBaitButton;
    [SerializeField] TextMeshProUGUI baitLevelText;
    [SerializeField] TextMeshProUGUI baitCostText;

    [SerializeField] GameObject     upgradeInventoryButton;
    [SerializeField] TextMeshProUGUI inventoryLevelText;
    [SerializeField] TextMeshProUGUI inventoryCostText;

    [SerializeField] GameObject sellAllButton;
    [SerializeField] GameObject endDayButton;

    [Header("Audio")]
    [SerializeField] AudioClip uiOpenClip;
    [SerializeField] AudioClip uiCloseClip;
    [SerializeField] AudioClip sellClip;
    [SerializeField] AudioClip purchaseClip;
    private AudioSource audioSource;

    void Start() => audioSource = GetComponent<AudioSource>();

    void OnTriggerStay2D(Collider2D other)
    {
        bool canInteract = playerController.CurrentState == PlayerController.State.Idle
                        || playerController.CurrentState == PlayerController.State.Moving;
        tentMenuButton.SetActive(canInteract);
        tentRenderer.sprite = canInteract ? tentHighlighted : tentNormal;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        tentMenuButton.SetActive(false);
        tentRenderer.sprite = tentNormal;
    }

    public void OpenShop()
    {
        playerController.SetUIDisplay(true);
        audioSource.PlayOneShot(uiOpenClip);
        shopUI.SetActive(true);
        RefreshUI();
    }

    public void CloseShop()
    {
        playerController.SetUIDisplay(false);
        audioSource.PlayOneShot(uiCloseClip);
        shopUI.SetActive(false);
    }

    public void SellAll()
    {
        int earned = saveManager.SellInventory();
        saveManager.data.cash += earned;
        playerController.UpdateInventoryText();
        audioSource.PlayOneShot(sellClip);
        RefreshUI();
    }

    public void UpgradeEngine()    => TryPurchase(upgradeConfig.engineCosts,    ref saveManager.data.engineLevel);
    public void UpgradeRod()       => TryPurchase(upgradeConfig.rodCosts,       ref saveManager.data.rodLevel);
    public void UpgradeBait()      => TryPurchase(upgradeConfig.baitCosts,      ref saveManager.data.baitLevel);
    public void UpgradeInventory() => TryPurchase(upgradeConfig.inventoryCosts, ref saveManager.data.inventoryLevel);

    void TryPurchase(int[] costs, ref int level)
    {
        int cost = upgradeConfig.GetCost(costs, level);
        if (cost < 0 || saveManager.data.cash < cost) return;
        saveManager.data.cash -= cost;
        level++;
        audioSource.PlayOneShot(purchaseClip);
        playerController.UpdateInventoryText();
        RefreshUI();
    }

    void RefreshUI()
    {
        var save = saveManager.data;
        cashText.text = $"${save.cash}";

        RefreshUpgrade(upgradeEngineButton,    engineLevelText,    engineCostText,
                       upgradeConfig.engineCosts,    save.engineLevel,    "Engine");
        RefreshUpgrade(upgradeRodButton,       rodLevelText,       rodCostText,
                       upgradeConfig.rodCosts,       save.rodLevel,       "Fishing Rod");
        RefreshUpgrade(upgradeBaitButton,      baitLevelText,      baitCostText,
                       upgradeConfig.baitCosts,      save.baitLevel,      "Bait");
        RefreshUpgrade(upgradeInventoryButton, inventoryLevelText, inventoryCostText,
                       upgradeConfig.inventoryCosts, save.inventoryLevel, "Inventory");

        sellAllButton.SetActive(save.caughtCount > 0);
        endDayButton.SetActive(dayNightCycle.IsNightTime || playerController.IsScared);
    }

    void RefreshUpgrade(GameObject btn, TextMeshProUGUI levelTxt, TextMeshProUGUI costTxt,
                        int[] costs, int level, string label)
    {
        bool maxed = upgradeConfig.IsMaxLevel(costs, level);
        btn.SetActive(!maxed);
        levelTxt.text = maxed ? $"{label} (MAX)" : $"{label} (Lv {level + 1})";
        costTxt.text  = maxed ? "" : $"${costs[level]}";
    }
}
