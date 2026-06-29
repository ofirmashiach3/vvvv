using UnityEngine;

/// <summary>
/// Shelf - מדף בחנות שהשחקן ממלא והלקוחות קונים ממנו
/// השחקן ניגש למדף ומפקיד סחורה, הלקוחות ניגשים ולוקחים.
/// כשהמדף ריק, הלקוחות לא יבחרו אותו כיעד.
/// 
/// Setup Instructions:
/// 1. Create a shelf model (or Cube placeholder).
/// 2. Add a BoxCollider set to "Is Trigger" (slightly larger than the model).
/// 3. Attach this script.
/// 4. Optionally add visual "product" GameObjects as children that show/hide based on stock.
/// </summary>
public class Shelf : MonoBehaviour
{
    [Header("Stock Settings")]
    [Tooltip("Maximum items this shelf can hold")]
    [SerializeField] private int maxStock = 10;

    [Tooltip("Current items on the shelf")]
    [SerializeField] private int currentStock = 0;

    [Tooltip("Base price per item (before quality multiplier)")]
    [SerializeField] private int basePricePerItem = 10;

    [Header("Shelf Type")]
    [Tooltip("What kind of product this shelf holds")]
    [SerializeField] private ProductType productType = ProductType.Vegetables;

    [Header("Visual Products (Optional)")]
    [Tooltip("Array of product visual GameObjects on the shelf. They show/hide based on stock level.")]
    [SerializeField] private GameObject[] productVisuals;

    [Header("Fill Rate")]
    [Tooltip("How many items the player deposits per second while touching the shelf")]
    [SerializeField] private float fillRate = 3f;

    // Internal
    private float fillTimer = 0f;
    private bool playerIsNear = false;
    private PlayerInventory nearbyPlayerInventory;

    public enum ProductType
    {
        Vegetables,
        Fruits,
        Dairy,
        Bakery,
        Snacks,
        Drinks
    }

    void Update()
    {
        // Auto-fill shelf when player stands near it with items
        if (playerIsNear && nearbyPlayerInventory != null && !nearbyPlayerInventory.IsEmpty && !IsFull)
        {
            fillTimer += Time.deltaTime;
            if (fillTimer >= 1f / fillRate)
            {
                fillTimer = 0f;

                // Take one item from player inventory
                int deposited = nearbyPlayerInventory.DepositItems(1);
                if (deposited > 0)
                {
                    currentStock += deposited;
                    UpdateProductVisuals();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            nearbyPlayerInventory = other.GetComponent<PlayerInventory>();
            fillTimer = 0f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            nearbyPlayerInventory = null;
            fillTimer = 0f;
        }
    }

    /// <summary>
    /// Called by a customer AI to take an item from the shelf.
    /// Returns the price the customer should pay (0 if shelf is empty).
    /// </summary>
    public int CustomerTakeItem()
    {
        if (currentStock <= 0) return 0;

        currentStock--;
        UpdateProductVisuals();

        // Calculate item price with quality multiplier
        float priceMultiplier = 1f;
        if (TycoonManager.Instance != null)
        {
            priceMultiplier = TycoonManager.Instance.GetProducePriceMultiplier();
        }

        return Mathf.RoundToInt(basePricePerItem * priceMultiplier);
    }

    /// <summary>
    /// Update visual products on the shelf based on current stock.
    /// Shows/hides product meshes proportionally.
    /// </summary>
    private void UpdateProductVisuals()
    {
        if (productVisuals == null || productVisuals.Length == 0) return;

        // Calculate how many visuals should be active
        float stockRatio = (float)currentStock / maxStock;
        int visualsToShow = Mathf.CeilToInt(stockRatio * productVisuals.Length);

        for (int i = 0; i < productVisuals.Length; i++)
        {
            if (productVisuals[i] != null)
            {
                productVisuals[i].SetActive(i < visualsToShow);
            }
        }
    }

    /// <summary>
    /// Change the shelf color based on stock (for quick visual feedback).
    /// Green = full, Yellow = low, Red = empty.
    /// </summary>
    private Color GetStockColor()
    {
        float ratio = (float)currentStock / maxStock;
        if (ratio > 0.5f) return Color.green;
        if (ratio > 0.2f) return Color.yellow;
        return Color.red;
    }

    // --- Public Getters ---
    public bool HasStock => currentStock > 0;
    public bool IsFull => currentStock >= maxStock;
    public int CurrentStock => currentStock;
    public int MaxStock => maxStock;
    public ProductType Type => productType;
    public float StockPercentage => (float)currentStock / maxStock;
}
