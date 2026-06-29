using UnityEngine;
using System.Collections;

/// <summary>
/// CustomerSpawner - מזמן לקוחות חדשים לחנות
/// יוצר לקוחות בקצב קבוע, מוודא שלא עוברים את המקסימום,
/// ובוחר עבור כל לקוח מדף יעד שיש בו סחורה.
/// 
/// Setup Instructions:
/// 1. Create an empty GameObject called "CustomerSpawner" near the store entrance.
/// 2. Attach this script.
/// 3. Assign the customer prefab (a Capsule with NavMeshAgent + CustomerAI).
/// 4. Assign spawn point transforms (where customers appear).
/// 5. Assign available shelves and registers in the Inspector.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Customer prefab (must have NavMeshAgent + CustomerAI)")]
    [SerializeField] private GameObject customerPrefab;

    [Tooltip("Points where customers spawn (near the entrance)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Point where customers walk to exit the store")]
    [SerializeField] private Transform exitPoint;

    [Header("Spawn Rate")]
    [Tooltip("Base time between customer spawns (seconds)")]
    [SerializeField] private float baseSpawnInterval = 4f;

    [Tooltip("Randomness added to spawn interval")]
    [SerializeField] private float spawnIntervalVariance = 1.5f;

    [Header("Capacity")]
    [Tooltip("Maximum customers in the store at once")]
    [SerializeField] private int maxCustomers = 15;

    [Header("Store References")]
    [Tooltip("All shelves in the store that customers can visit")]
    [SerializeField] private Shelf[] availableShelves;

    [Tooltip("All registers where customers can pay")]
    [SerializeField] private RegisterQueue[] availableRegisters;

    // Internal
    private int currentCustomerCount = 0;
    private bool spawningActive = true;

    void Start()
    {
        // Validate setup
        if (customerPrefab == null)
        {
            Debug.LogError("[CustomerSpawner] Customer prefab is not assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[CustomerSpawner] No spawn points assigned!");
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Main spawn loop - runs forever, spawning customers at intervals.
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        // Wait a few seconds before first customer
        yield return new WaitForSeconds(2f);

        while (spawningActive)
        {
            // Check if we can spawn
            if (currentCustomerCount < maxCustomers && HasStockedShelves())
            {
                SpawnCustomer();
            }

            // Wait for next spawn (with randomness)
            float interval = baseSpawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            interval = Mathf.Max(interval, 1f); // Minimum 1 second between spawns
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// Spawn a single customer.
    /// </summary>
    private void SpawnCustomer()
    {
        // Pick a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Find a shelf that has stock
        Shelf targetShelf = FindStockedShelf();
        if (targetShelf == null) return; // No stocked shelves, don't spawn

        // Find a register with space in the queue
        RegisterQueue targetRegister = FindAvailableRegister();
        if (targetRegister == null) return; // All registers are full

        // Create the customer
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        CustomerAI customerAI = customerObj.GetComponent<CustomerAI>();

        if (customerAI != null)
        {
            // Tell the customer where to go
            customerAI.Initialize(targetShelf, targetRegister, exitPoint, this);
        }

        currentCustomerCount++;
    }

    /// <summary>
    /// Find a random shelf that has stock available.
    /// </summary>
    private Shelf FindStockedShelf()
    {
        // Collect all shelves with stock
        var stockedShelves = new System.Collections.Generic.List<Shelf>();
        foreach (Shelf shelf in availableShelves)
        {
            if (shelf != null && shelf.gameObject.activeInHierarchy && shelf.HasStock)
            {
                stockedShelves.Add(shelf);
            }
        }

        if (stockedShelves.Count == 0) return null;

        // Pick a random stocked shelf
        return stockedShelves[Random.Range(0, stockedShelves.Count)];
    }

    /// <summary>
    /// Find a register that has space in the queue.
    /// Prefers the shortest queue.
    /// </summary>
    private RegisterQueue FindAvailableRegister()
    {
        RegisterQueue bestRegister = null;
        int shortestQueue = int.MaxValue;

        foreach (RegisterQueue register in availableRegisters)
        {
            if (register != null && register.gameObject.activeInHierarchy && register.HasSpace)
            {
                if (register.QueueLength < shortestQueue)
                {
                    shortestQueue = register.QueueLength;
                    bestRegister = register;
                }
            }
        }

        return bestRegister;
    }

    /// <summary>
    /// Check if any shelf in the store has stock.
    /// If all shelves are empty, no point spawning customers.
    /// </summary>
    private bool HasStockedShelves()
    {
        foreach (Shelf shelf in availableShelves)
        {
            if (shelf != null && shelf.gameObject.activeInHierarchy && shelf.HasStock)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Called by CustomerAI when a customer leaves the store.
    /// </summary>
    public void OnCustomerLeft()
    {
        currentCustomerCount--;
        currentCustomerCount = Mathf.Max(0, currentCustomerCount);
    }

    /// <summary>
    /// Dynamically refresh the shelves list (call after new shelves are unlocked).
    /// </summary>
    public void RefreshShelves()
    {
        availableShelves = FindObjectsByType<Shelf>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// Dynamically refresh the registers list (call after new registers are unlocked).
    /// </summary>
    public void RefreshRegisters()
    {
        availableRegisters = FindObjectsByType<RegisterQueue>(FindObjectsSortMode.None);
    }

    // --- Public Getters ---
    public int CurrentCustomerCount => currentCustomerCount;
    public int MaxCustomers => maxCustomers;
}
