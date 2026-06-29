using UnityEngine;

/// <summary>
/// PlayerInventory - מערכת נשיאת סחורה של השחקן
/// השחקן יכול לאסוף סחורה מארגזים ולהפקיד אותה במדפים.
/// כולל מערכת קיבולת שניתן לשדרג.
/// 
/// Setup Instructions:
/// 1. Attach this script to the Player GameObject.
/// 2. Optionally assign a visual stack transform (a child object above the player's head)
///    to show boxes stacking as the player carries more items.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("Maximum items the player can carry at once")]
    [SerializeField] private int maxCapacity = 5;

    [Tooltip("Current items being carried")]
    [SerializeField] private int currentItems = 0;

    [Header("Visual Stack (Optional)")]
    [Tooltip("Parent transform for visual item boxes above player's head")]
    [SerializeField] private Transform stackParent;

    [Tooltip("Prefab for a visual box/crate that stacks on the player")]
    [SerializeField] private GameObject itemBoxPrefab;

    [Tooltip("Height offset between stacked boxes")]
    [SerializeField] private float stackOffset = 0.35f;

    // Event that other systems can subscribe to
    public System.Action<int, int> OnInventoryChanged; // (currentItems, maxCapacity)

    void Start()
    {
        UpdateVisualStack();
        NotifyChange();
    }

    /// <summary>
    /// Try to pick up items. Returns how many were actually picked up (limited by capacity).
    /// Called by SupplyCrate when the player touches it.
    /// </summary>
    public int PickupItems(int amount)
    {
        int spaceLeft = maxCapacity - currentItems;
        int pickedUp = Mathf.Min(amount, spaceLeft);

        if (pickedUp > 0)
        {
            currentItems += pickedUp;
            UpdateVisualStack();
            NotifyChange();
        }

        return pickedUp;
    }

    /// <summary>
    /// Try to deposit items onto a shelf. Returns how many were actually deposited.
    /// Called by Shelf when the player touches it.
    /// </summary>
    public int DepositItems(int maxToDeposit)
    {
        int toDeposit = Mathf.Min(currentItems, maxToDeposit);

        if (toDeposit > 0)
        {
            currentItems -= toDeposit;
            UpdateVisualStack();
            NotifyChange();
        }

        return toDeposit;
    }

    /// <summary>
    /// Upgrade the player's carrying capacity.
    /// Called by the upgrade system.
    /// </summary>
    public void UpgradeCapacity(int additionalSlots)
    {
        maxCapacity += additionalSlots;
        NotifyChange();
    }

    /// <summary>
    /// Update the visual boxes stacked above the player's head.
    /// </summary>
    private void UpdateVisualStack()
    {
        if (stackParent == null || itemBoxPrefab == null) return;

        // Clear existing visual boxes
        foreach (Transform child in stackParent)
        {
            Destroy(child.gameObject);
        }

        // Create new boxes based on current items
        for (int i = 0; i < currentItems; i++)
        {
            Vector3 localPos = new Vector3(0f, i * stackOffset, 0f);
            GameObject box = Instantiate(itemBoxPrefab, stackParent);
            box.transform.localPosition = localPos;
            box.transform.localRotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
        }
    }

    private void NotifyChange()
    {
        OnInventoryChanged?.Invoke(currentItems, maxCapacity);
    }

    // --- Public Getters ---
    public int CurrentItems => currentItems;
    public int MaxCapacity => maxCapacity;
    public bool IsFull => currentItems >= maxCapacity;
    public bool IsEmpty => currentItems <= 0;
}
