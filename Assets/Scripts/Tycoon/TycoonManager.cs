using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TycoonManager - מנהל הכלכלה המרכזי של המשחק (Singleton)
/// אחראי על ניהול כסף, שדרוגים גלובליים וסטטיסטיקות.
/// 
/// Setup Instructions:
/// 1. Create an empty GameObject called "TycoonManager" in the scene.
/// 2. Attach this script to it.
/// 3. All other scripts access it via TycoonManager.Instance.
/// </summary>
public class TycoonManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static TycoonManager Instance { get; private set; }

    [Header("Economy")]
    [Tooltip("Starting money for the player")]
    [SerializeField] private int startingMoney = 500;

    [Tooltip("Current player money")]
    [SerializeField] private int currentMoney;

    [Header("Global Stats")]
    [Tooltip("Total money earned throughout the entire game")]
    [SerializeField] private int totalMoneyEarned = 0;

    [Tooltip("Total customers served")]
    [SerializeField] private int totalCustomersServed = 0;

    [Header("Upgrade Levels")]
    [SerializeField] private int registerSpeedLevel = 1;
    [SerializeField] private int produceQualityLevel = 1;
    [SerializeField] private int playerCapacityLevel = 1;
    [SerializeField] private int storeExpansionLevel = 0;

    [Header("Upgrade Multipliers")]
    [Tooltip("How much faster registers process per level (e.g., 0.15 = 15% faster per level)")]
    [SerializeField] private float registerSpeedBonusPerLevel = 0.15f;

    [Tooltip("Price multiplier per produce quality level")]
    [SerializeField] private float produceQualityBonusPerLevel = 0.20f;

    // --- Events ---
    /// <summary>Fired whenever money changes. Parameters: (currentMoney, delta)</summary>
    public UnityEvent<int, int> OnMoneyChanged;

    /// <summary>Fired whenever any upgrade level changes.</summary>
    public UnityEvent OnUpgradesChanged;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentMoney = startingMoney;
    }

    // ========== MONEY MANAGEMENT ==========

    /// <summary>
    /// Add money to the player's wallet. Used when customers pay.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        currentMoney += amount;
        totalMoneyEarned += amount;
        OnMoneyChanged?.Invoke(currentMoney, amount);
    }

    /// <summary>
    /// Try to spend money. Returns true if successful, false if not enough money.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;
        if (currentMoney < amount) return false;

        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney, -amount);
        return true;
    }

    /// <summary>
    /// Check if the player can afford a purchase.
    /// </summary>
    public bool CanAfford(int amount)
    {
        return currentMoney >= amount;
    }

    // ========== UPGRADE SYSTEM ==========

    /// <summary>
    /// Upgrade register speed. Makes checkout faster.
    /// </summary>
    public void UpgradeRegisterSpeed(int cost)
    {
        if (SpendMoney(cost))
        {
            registerSpeedLevel++;
            OnUpgradesChanged?.Invoke();
            Debug.Log($"[TycoonManager] Register speed upgraded to level {registerSpeedLevel}!");
        }
    }

    /// <summary>
    /// Upgrade produce quality. Each item sells for more money.
    /// </summary>
    public void UpgradeProduceQuality(int cost)
    {
        if (SpendMoney(cost))
        {
            produceQualityLevel++;
            OnUpgradesChanged?.Invoke();
            Debug.Log($"[TycoonManager] Produce quality upgraded to level {produceQualityLevel}!");
        }
    }

    /// <summary>
    /// Upgrade player carrying capacity.
    /// </summary>
    public void UpgradePlayerCapacity(int cost)
    {
        if (SpendMoney(cost))
        {
            playerCapacityLevel++;
            
            // Find the player and upgrade their inventory
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                inventory.UpgradeCapacity(2); // +2 slots per level
            }
            
            OnUpgradesChanged?.Invoke();
            Debug.Log($"[TycoonManager] Player capacity upgraded to level {playerCapacityLevel}!");
        }
    }

    /// <summary>
    /// Record that a customer was served (for stats/achievements).
    /// </summary>
    public void RecordCustomerServed()
    {
        totalCustomersServed++;
    }

    // ========== GETTERS ==========

    /// <summary>Get the register speed multiplier based on current level.</summary>
    public float GetRegisterSpeedMultiplier()
    {
        return 1f + (registerSpeedLevel - 1) * registerSpeedBonusPerLevel;
    }

    /// <summary>Get the produce price multiplier based on quality level.</summary>
    public float GetProducePriceMultiplier()
    {
        return 1f + (produceQualityLevel - 1) * produceQualityBonusPerLevel;
    }

    public int CurrentMoney => currentMoney;
    public int TotalMoneyEarned => totalMoneyEarned;
    public int TotalCustomersServed => totalCustomersServed;
    public int RegisterSpeedLevel => registerSpeedLevel;
    public int ProduceQualityLevel => produceQualityLevel;
    public int PlayerCapacityLevel => playerCapacityLevel;
    public int StoreExpansionLevel => storeExpansionLevel;
}
