using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RegisterQueue - קופה עם ניהול תור לקוחות
/// לקוחות מגיעים ומצטרפים לתור. כל לקוח ממתין לתורו,
/// משלם, ועוזב. מהירות התשלום תלויה ברמת שדרוג הקופה.
/// 
/// Setup Instructions:
/// 1. Create a register model (or Cube placeholder).
/// 2. Create empty GameObjects as children named "QueueSlot_0", "QueueSlot_1", etc.
///    Position them in a line behind the register (these are where customers stand in line).
/// 3. Add a BoxCollider set to "Is Trigger" (for customer detection).
/// 4. Attach this script and assign the queue slot transforms.
/// </summary>
public class RegisterQueue : MonoBehaviour
{
    [Header("Queue Settings")]
    [Tooltip("Positions where customers stand in line (first = at the register, last = back of line)")]
    [SerializeField] private Transform[] queueSlots;

    [Tooltip("Base time (seconds) to process one customer")]
    [SerializeField] private float baseProcessTime = 3f;

    [Header("Status")]
    [SerializeField] private bool isProcessing = false;

    // The queue of customers waiting
    private List<CustomerAI> customerQueue = new List<CustomerAI>();
    private float processTimer = 0f;

    void Update()
    {
        if (customerQueue.Count == 0) return;

        // Process the first customer in line
        if (!isProcessing)
        {
            isProcessing = true;
            processTimer = 0f;
        }

        if (isProcessing)
        {
            // Calculate actual process time (affected by register speed upgrade)
            float speedMultiplier = 1f;
            if (TycoonManager.Instance != null)
            {
                speedMultiplier = TycoonManager.Instance.GetRegisterSpeedMultiplier();
            }

            float actualProcessTime = baseProcessTime / speedMultiplier;

            processTimer += Time.deltaTime;

            if (processTimer >= actualProcessTime)
            {
                // Done processing - customer pays and leaves
                ProcessCustomer();
            }
        }
    }

    /// <summary>
    /// A customer joins the back of the line.
    /// Returns the queue position transform they should walk to.
    /// Returns null if the queue is full.
    /// </summary>
    public Transform JoinQueue(CustomerAI customer)
    {
        if (customerQueue.Count >= queueSlots.Length)
        {
            return null; // Queue is full
        }

        int position = customerQueue.Count;
        customerQueue.Add(customer);

        return queueSlots[position];
    }

    /// <summary>
    /// Remove a customer from the queue (e.g., if they give up).
    /// </summary>
    public void LeaveQueue(CustomerAI customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            RefreshQueuePositions();
        }
    }

    /// <summary>
    /// Process the first customer in line - they pay and leave.
    /// </summary>
    private void ProcessCustomer()
    {
        if (customerQueue.Count == 0) return;

        CustomerAI customer = customerQueue[0];

        // Customer pays - add money to the player
        if (TycoonManager.Instance != null && customer != null)
        {
            int payment = customer.GetTotalPayment();
            TycoonManager.Instance.AddMoney(payment);
            TycoonManager.Instance.RecordCustomerServed();

            Debug.Log($"[Register] Customer paid ${payment}!");

            // Spawn floating money text effect (optional)
            SpawnMoneyPopup(payment);
        }

        // Remove customer from queue and tell them to leave the store
        customerQueue.RemoveAt(0);
        
        if (customer != null)
        {
            customer.OnPaymentComplete();
        }

        // Move remaining customers forward
        RefreshQueuePositions();

        // Reset processing
        isProcessing = false;
        processTimer = 0f;
    }

    /// <summary>
    /// After someone leaves the queue, move everyone forward.
    /// </summary>
    private void RefreshQueuePositions()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            if (customerQueue[i] != null && i < queueSlots.Length)
            {
                customerQueue[i].MoveToQueuePosition(queueSlots[i]);
            }
        }
    }

    /// <summary>
    /// Spawn a floating "+$X" text above the register.
    /// </summary>
    private void SpawnMoneyPopup(int amount)
    {
        // This is a placeholder - in a real project you'd instantiate a
        // floating text prefab (TextMeshPro with animation).
        // For now, we just log it.
        Debug.Log($"+${amount}");
    }

    // --- Public Getters ---
    public int QueueLength => customerQueue.Count;
    public int MaxQueueLength => queueSlots != null ? queueSlots.Length : 0;
    public bool HasSpace => customerQueue.Count < (queueSlots != null ? queueSlots.Length : 0);
    public bool IsEmpty => customerQueue.Count == 0;
}
