using UnityEngine;

/// <summary>
/// PlayerController - שליטה בתנועת השחקן
/// מאפשר תנועה עם WASD/חצים במבט עילי (Top-Down).
/// השחקן מסתובב לכיוון התנועה בצורה חלקה.
/// 
/// Setup Instructions:
/// 1. Attach this script to your Player GameObject (a Capsule with a Rigidbody).
/// 2. Make sure the Rigidbody has "Freeze Rotation X, Y, Z" checked.
/// 3. Add a Collider (CapsuleCollider) to the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Player movement speed (units per second)")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("How fast the player rotates to face movement direction")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Animation (Optional)")]
    [Tooltip("Animator component - leave empty if no animations yet")]
    [SerializeField] private Animator animator;

    // Internal references
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isMoving;

    // Animator parameter hash (for performance)
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Lock rotation so the player doesn't fall over
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // --- Read Input ---
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

        // Create movement vector (on the XZ plane, Y is up in Unity)
        moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = moveDirection.magnitude > 0.1f;

        // --- Rotate Player to Face Movement Direction ---
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Update Animator (if attached) ---
        if (animator != null)
        {
            animator.SetBool(AnimIsMoving, isMoving);
            animator.SetFloat(AnimMoveSpeed, isMoving ? 1f : 0f);
        }
    }

    void FixedUpdate()
    {
        // --- Apply Movement via Rigidbody (physics-based, prevents clipping through walls) ---
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y; // Preserve gravity
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Returns true if the player is currently moving.
    /// Used by other systems (e.g., inventory) to check state.
    /// </summary>
    public bool IsMoving => isMoving;
}
