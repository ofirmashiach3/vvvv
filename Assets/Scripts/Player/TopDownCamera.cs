using UnityEngine;

/// <summary>
/// TopDownCamera - מצלמה עילית שעוקבת אחרי השחקן
/// יוצרת מבט איזומטרי/Top-Down קלאסי כמו במשחקי טייקון ברובלוקס.
/// 
/// Setup Instructions:
/// 1. Attach this script to the Main Camera.
/// 2. Drag the Player transform into the "Target" field in the Inspector.
/// 3. Adjust Height, Distance, and Angle to get the view you want.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform to follow")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [Tooltip("Height above the player")]
    [SerializeField] private float height = 12f;

    [Tooltip("Distance behind the player (used with angle)")]
    [SerializeField] private float distance = 8f;

    [Tooltip("Camera look-down angle in degrees (45 = isometric, 90 = pure top-down)")]
    [Range(30f, 90f)]
    [SerializeField] private float angle = 55f;

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera follows (lower = smoother, higher = snappier)")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Zoom (Optional)")]
    [Tooltip("Allow zooming with scroll wheel")]
    [SerializeField] private bool allowZoom = true;

    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minHeight = 6f;
    [SerializeField] private float maxHeight = 25f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // --- Handle Zoom ---
        if (allowZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                height -= scroll * zoomSpeed;
                height = Mathf.Clamp(height, minHeight, maxHeight);

                // Adjust distance proportionally to keep the same feel
                distance = height * 0.65f;
            }
        }

        // --- Calculate Desired Camera Position ---
        // Convert angle to radians for calculation
        float angleRad = angle * Mathf.Deg2Rad;

        // Position the camera behind and above the player
        Vector3 offset = new Vector3(0f, height, -distance);

        Vector3 desiredPosition = target.position + offset;

        // --- Smooth Follow ---
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / followSpeed
        );

        // --- Look At Player ---
        // Look slightly ahead of the player's feet (not exactly at center)
        Vector3 lookTarget = target.position + Vector3.up * 1f;
        transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Set a new target for the camera to follow.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
