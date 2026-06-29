using UnityEngine;
using TMPro;

/// <summary>
/// GameUI - ממשק המשתמש הראשי של המשחק
/// מציג את כמות הכסף, מלאי השחקן, ומספר הלקוחות בחנות.
/// מתעדכן אוטומטית דרך Events מהמערכות האחרות.
/// 
/// Setup Instructions:
/// 1. Create a Canvas (Screen Space - Overlay).
/// 2. Create TextMeshPro UI elements for money, inventory, and customers.
/// 3. Attach this script to the Canvas.
/// 4. Assign the text references in the Inspector.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text showing current money")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Tooltip("Text showing player inventory (items carried)")]
    [SerializeField] private TextMeshProUGUI inventoryText;

    [Tooltip("Text showing customers in store")]
    [SerializeField] private TextMeshProUGUI customersText;

    [Tooltip("Text showing store level/stats")]
    [SerializeField] private TextMeshProUGUI storeStatsText;

    [Header("Money Popup")]
    [Tooltip("Prefab for floating '+$X' text (optional)")]
    [SerializeField] private GameObject moneyPopupPrefab;

    [Header("Animation")]
    [Tooltip("Animate money text when it changes")]
    [SerializeField] private bool animateMoneyChanges = true;

    // Internal
    private PlayerInventory playerInventory;
    private CustomerSpawner customerSpawner;
    private int displayedMoney = 0;
    private float moneyAnimTimer = 0f;
    private int targetMoney = 0;

    void Start()
    {
        // Find references
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        customerSpawner = FindFirstObjectByType<CustomerSpawner>();

        // Subscribe to events
        if (TycoonManager.Instance != null)
        {
            TycoonManager.Instance.OnMoneyChanged.AddListener(OnMoneyChanged);
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
        }

        // Initial update
        UpdateAllUI();
    }

    void Update()
    {
        // Animate money counter (counting up/down effect)
        if (animateMoneyChanges && displayedMoney != targetMoney)
        {
            int diff = targetMoney - displayedMoney;
            int step = Mathf.Max(1, Mathf.Abs(diff) / 10);

            if (diff > 0)
                displayedMoney = Mathf.Min(displayedMoney + step, targetMoney);
            else
                displayedMoney = Mathf.Max(displayedMoney - step, targetMoney);

            if (moneyText != null)
            {
                moneyText.text = $"💰 ${displayedMoney:N0}";
            }
        }

        // Update customer count (not event-driven, so we poll)
        UpdateCustomerCount();
    }

    // ========== EVENT HANDLERS ==========

    private void OnMoneyChanged(int newMoney, int delta)
    {
        targetMoney = newMoney;

        if (!animateMoneyChanges)
        {
            displayedMoney = newMoney;
            if (moneyText != null)
            {
                moneyText.text = $"💰 ${newMoney:N0}";
            }
        }

        // Scale pop effect on money text
        if (moneyText != null)
        {
            StartCoroutine(TextPopEffect(moneyText.transform, delta > 0 ? 1.3f : 0.85f));
        }
    }

    private void OnInventoryChanged(int current, int max)
    {
        if (inventoryText != null)
        {
            inventoryText.text = $"📦 {current}/{max}";

            // Color based on capacity
            if (current >= max)
                inventoryText.color = new Color(1f, 0.3f, 0.3f); // Red = full
            else if (current > max * 0.7f)
                inventoryText.color = new Color(1f, 0.8f, 0.2f); // Yellow = almost full
            else
                inventoryText.color = Color.white;
        }
    }

    // ========== UPDATE METHODS ==========

    private void UpdateCustomerCount()
    {
        if (customersText != null && customerSpawner != null)
        {
            customersText.text = $"👥 {customerSpawner.CurrentCustomerCount}/{customerSpawner.MaxCustomers}";
        }
    }

    private void UpdateAllUI()
    {
        if (TycoonManager.Instance != null)
        {
            targetMoney = TycoonManager.Instance.CurrentMoney;
            displayedMoney = targetMoney;
            if (moneyText != null)
            {
                moneyText.text = $"💰 ${displayedMoney:N0}";
            }
        }

        if (playerInventory != null)
        {
            OnInventoryChanged(playerInventory.CurrentItems, playerInventory.MaxCapacity);
        }

        UpdateCustomerCount();
        UpdateStoreStats();
    }

    private void UpdateStoreStats()
    {
        if (storeStatsText == null || TycoonManager.Instance == null) return;

        TycoonManager tm = TycoonManager.Instance;
        storeStatsText.text = $"📊 Register Lv.{tm.RegisterSpeedLevel} | Quality Lv.{tm.ProduceQualityLevel} | Capacity Lv.{tm.PlayerCapacityLevel}";
    }

    // ========== VISUAL EFFECTS ==========

    private System.Collections.IEnumerator TextPopEffect(Transform textTransform, float scaleMultiplier)
    {
        Vector3 originalScale = Vector3.one;

        // Scale up
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, t / 0.1f);
            yield return null;
        }

        // Scale back
        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalScale * scaleMultiplier, originalScale, t / 0.15f);
            yield return null;
        }

        textTransform.localScale = originalScale;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (TycoonManager.Instance != null)
        {
            TycoonManager.Instance.OnMoneyChanged.RemoveListener(OnMoneyChanged);
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }
}
