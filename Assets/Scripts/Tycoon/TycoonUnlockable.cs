using UnityEngine;

/// <summary>
/// TycoonUnlockable - אובייקט שנפתח/מופיע בעת רכישה
/// זהו הקומפוננט שיושב על כל דבר שהשחקן יכול לקנות:
/// מדפים, קופות, קירות, הרחבות חנות וכו'.
/// מתחיל כמוסתר (Inactive) ומופיע עם אנימציה כשהשחקן קונה.
/// 
/// Setup Instructions:
/// 1. Create your shelf/register/wall GameObject.
/// 2. Attach this script to it.
/// 3. In the Inspector, uncheck the GameObject's active checkbox (make it inactive).
/// 4. Assign this object as the "unlockTarget" in the matching TycoonButton.
/// </summary>
public class TycoonUnlockable : MonoBehaviour
{
    [Header("Unlock Animation")]
    [Tooltip("How the object appears when unlocked")]
    [SerializeField] private UnlockAnimation animationType = UnlockAnimation.ScaleUp;

    [Tooltip("Duration of the appear animation")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip unlockSound;

    [Header("Status")]
    [SerializeField] private bool isUnlocked = false;

    public enum UnlockAnimation
    {
        ScaleUp,    // Grows from zero to full size
        DropDown,   // Falls from above
        FadeIn      // Fades in (requires transparent material)
    }

    /// <summary>
    /// Called by TycoonButton when the player purchases this item.
    /// </summary>
    public void Unlock()
    {
        if (isUnlocked) return;

        isUnlocked = true;
        gameObject.SetActive(true);

        // Play unlock sound
        if (unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);
        }

        // Start appear animation
        switch (animationType)
        {
            case UnlockAnimation.ScaleUp:
                StartCoroutine(AnimateScaleUp());
                break;
            case UnlockAnimation.DropDown:
                StartCoroutine(AnimateDropDown());
                break;
            case UnlockAnimation.FadeIn:
                StartCoroutine(AnimateScaleUp()); // Fallback to scale for simplicity
                break;
        }
    }

    /// <summary>
    /// Scale up from zero - looks like the object "builds itself"
    /// </summary>
    private System.Collections.IEnumerator AnimateScaleUp()
    {
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // Ease-out bounce curve for satisfying feel
            float t = EaseOutBack(progress);
            transform.localScale = targetScale * t;

            yield return null;
        }

        transform.localScale = targetScale;
    }

    /// <summary>
    /// Drop from above - object falls from the sky and lands in place
    /// </summary>
    private System.Collections.IEnumerator AnimateDropDown()
    {
        Vector3 targetPos = transform.position;
        Vector3 startPos = targetPos + Vector3.up * 10f;
        transform.position = startPos;

        Vector3 targetScale = transform.localScale;
        transform.localScale = targetScale; // Full size immediately

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // Ease-out bounce for the landing
            float t = EaseOutBounce(progress);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        transform.position = targetPos;
    }

    // --- Easing Functions ---

    /// <summary>Overshoot then settle (like a spring)</summary>
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>Bounce at the end (like dropping something)</summary>
    private float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    // --- Public Getters ---
    public bool IsUnlocked => isUnlocked;
}
