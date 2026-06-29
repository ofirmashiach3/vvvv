using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// CustomerAI - בינה מלאכותית של לקוח בחנות
/// מנהל את מחזור החיים המלא של לקוח:
/// 1. הולך למדף (GoingToShelf)
/// 2. אוסף מוצר (CollectingItem) 
/// 3. הולך לקופה (GoingToRegister)
/// 4. עומד בתור (WaitingInQueue)
/// 5. עוזב את החנות (Leaving)
/// 
/// Setup Instructions (for Customer Prefab):
/// 1. Create a Capsule (or character model).
/// 2. Add NavMeshAgent component.
/// 3. Attach this script.
/// 4. Save as Prefab and assign to CustomerSpawner.
/// 
/// IMPORTANT: The scene must have a NavMesh baked!
/// (Window > AI > Navigation > Bake)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CustomerAI : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walking speed of the customer")]
    [SerializeField] private float walkSpeed = 3.5f;

    [Tooltip("How close the customer needs to be to consider 'arrived'")]
    [SerializeField] private float arrivalDistance = 0.5f;

    [Header("Timing")]
    [Tooltip("Time spent 'browsing' at the shelf before taking an item")]
    [SerializeField] private float browseTime = 2f;

    [Header("Debug")]
    [SerializeField] private CustomerState currentState = CustomerState.Idle;

    // State Machine
    public enum CustomerState
    {
        Idle,
        GoingToShelf,
        CollectingItem,
        GoingToRegister,
        WaitingInQueue,
        Leaving,
        Destroyed
    }

    // References (set by CustomerSpawner via Initialize)
    private NavMeshAgent agent;
    private Shelf targetShelf;
    private RegisterQueue targetRegister;
    private Transform exitPoint;
    private CustomerSpawner spawner;

    // Item tracking
    private int itemValue = 0; // How much money this customer will pay

    // Timers
    private float stateTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 10f;
        agent.stoppingDistance = arrivalDistance;
    }

    /// <summary>
    /// Called by CustomerSpawner right after instantiation.
    /// Sets up the customer's targets and starts their shopping journey.
    /// </summary>
    public void Initialize(Shelf shelf, RegisterQueue register, Transform exit, CustomerSpawner parentSpawner)
    {
        targetShelf = shelf;
        targetRegister = register;
        exitPoint = exit;
        spawner = parentSpawner;

        // Add some randomness to walk speed (so customers don't all walk at the same pace)
        agent.speed = walkSpeed + Random.Range(-0.5f, 0.5f);

        // Randomize browse time
        browseTime += Random.Range(-0.5f, 1f);

        // Start walking to the shelf
        SetState(CustomerState.GoingToShelf);
    }

    void Update()
    {
        switch (currentState)
        {
            case CustomerState.GoingToShelf:
                UpdateGoingToShelf();
                break;

            case CustomerState.CollectingItem:
                UpdateCollectingItem();
                break;

            case CustomerState.GoingToRegister:
                UpdateGoingToRegister();
                break;

            case CustomerState.WaitingInQueue:
                // Just standing still, RegisterQueue handles processing
                break;

            case CustomerState.Leaving:
                UpdateLeaving();
                break;
        }
    }

    // ========== STATE UPDATES ==========

    private void UpdateGoingToShelf()
    {
        if (HasArrived())
        {
            SetState(CustomerState.CollectingItem);
        }
    }

    private void UpdateCollectingItem()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= browseTime)
        {
            // Take an item from the shelf
            if (targetShelf != null && targetShelf.HasStock)
            {
                itemValue = targetShelf.CustomerTakeItem();
            }
            else
            {
                // Shelf is empty, customer gets a base-price item anyway
                // (to keep the game flowing and not frustrating)
                itemValue = 5;
            }

            // Now go to the register
            SetState(CustomerState.GoingToRegister);
        }
    }

    private void UpdateGoingToRegister()
    {
        if (HasArrived())
        {
            // Join the queue
            if (targetRegister != null)
            {
                Transform queueSpot = targetRegister.JoinQueue(this);
                if (queueSpot != null)
                {
                    agent.SetDestination(queueSpot.position);
                    SetState(CustomerState.WaitingInQueue);
                }
                else
                {
                    // Queue is full - just leave
                    SetState(CustomerState.Leaving);
                }
            }
            else
            {
                SetState(CustomerState.Leaving);
            }
        }
    }

    private void UpdateLeaving()
    {
        if (HasArrived())
        {
            // Customer has left the store
            if (spawner != null)
            {
                spawner.OnCustomerLeft();
            }

            currentState = CustomerState.Destroyed;
            Destroy(gameObject);
        }
    }

    // ========== STATE TRANSITIONS ==========

    private void SetState(CustomerState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case CustomerState.GoingToShelf:
                if (targetShelf != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetShelf.transform.position);
                }
                break;

            case CustomerState.CollectingItem:
                agent.isStopped = true;
                // Customer stands at the shelf, "browsing"
                break;

            case CustomerState.GoingToRegister:
                agent.isStopped = false;
                if (targetRegister != null)
                {
                    agent.SetDestination(targetRegister.transform.position);
                }
                break;

            case CustomerState.WaitingInQueue:
                // Agent destination was already set when joining queue
                break;

            case CustomerState.Leaving:
                agent.isStopped = false;
                if (exitPoint != null)
                {
                    agent.SetDestination(exitPoint.position);
                }
                break;
        }
    }

    // ========== PUBLIC METHODS (called by RegisterQueue) ==========

    /// <summary>
    /// Called by RegisterQueue when this customer reaches the front and pays.
    /// </summary>
    public void OnPaymentComplete()
    {
        SetState(CustomerState.Leaving);
    }

    /// <summary>
    /// Called by RegisterQueue to move the customer forward in line.
    /// </summary>
    public void MoveToQueuePosition(Transform newPosition)
    {
        if (agent != null && newPosition != null)
        {
            agent.isStopped = false;
            agent.SetDestination(newPosition.position);
        }
    }

    /// <summary>
    /// Returns how much this customer should pay at the register.
    /// </summary>
    public int GetTotalPayment()
    {
        return Mathf.Max(itemValue, 1); // At least $1
    }

    // ========== HELPERS ==========

    /// <summary>
    /// Check if the NavMeshAgent has reached its destination.
    /// </summary>
    private bool HasArrived()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > arrivalDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f) return false;
        return true;
    }

    /// <summary>
    /// Safety - if the customer gets stuck, destroy after a timeout.
    /// </summary>
    private float lifetimeTimer = 0f;
    private const float MAX_LIFETIME = 120f; // 2 minutes max

    void LateUpdate()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= MAX_LIFETIME && currentState != CustomerState.Destroyed)
        {
            Debug.LogWarning("[CustomerAI] Customer timed out and was removed.");
            if (spawner != null) spawner.OnCustomerLeft();
            if (targetRegister != null) targetRegister.LeaveQueue(this);
            Destroy(gameObject);
        }
    }
}
