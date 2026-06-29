using UnityEngine;
using TMPro;

/// <summary>
/// TycoonButton - כפתור רצפה לרכישות ושדרוגים (בדיוק כמו ברובלוקס!)
/// כשהשחקן דורך על הכפתור, הרכישה מתבצעת אוטומטית אם יש מספיק כסף.
/// הכפתור מציג את המחיר ויכול להיעלם אחרי קנייה חד-פעמית (למשל קניית מדף)
/// או להישאר עבור שדרוגים חוזרים (למשל שדרוג מהירות קופה).
/// 
/// Setup Instructions:
/// 1. Create a Cube or Plane on the floor, scale it to be flat (like a pad).
/// 2. Add a BoxCollider, check "Is Trigger".
/// 3. Attach this script.
/// 4. Create a child 3D Text (TextMeshPro) to display the price.
/// 5. Assign the Unlockable object (the shelf/register that appears after purchase).
/// </summary>
public class TycoonButton : MonoBehaviour
{
    [Header("Purchase Settings")]
    [Tooltip("Cost of this purchase/upgrade")]
    [SerializeField] private int cost = 100;

    [Tooltip("Name displayed on the button (e.g., 'Buy Shelf', 'Upgrade Register')")]
    [SerializeField] private string displayName = "Buy Item";

    [Header("Behavior")]
    [Tooltip("If true, button disappears after one purchase. If false, it stays for repeated upgrades.")]
    [SerializeField] private bool oneTimePurchase = true;

    [Tooltip("Cost multiplier for each repeated purchase (only if oneTimePurchase is false)")]
    [SerializeField] private float costMultiplier = 1.5f;

    [Header("Unlock Target")]
    [Tooltip("The object to activate/show when purchased (shelf, register, etc.)")]
    [SerializeField] private TycoonUnlockable unlockTarget;

    [Header("Upgrade Action (Optional)")]
    [Tooltip("Which upgrade to trigger on purchase")]
    [SerializeField] private UpgradeType upgradeType = UpgradeType.None;

    [Header("Visuals")]
    [Tooltip("3D Text showing the price")]
    [SerializeField] private TextMeshPro priceText;

    [Tooltip("Color when player can afford")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.9f, 0.3f, 0.8f); // Green glow

    [Tooltip("Color when player can't afford")]
    [SerializeField] private Color expensiveColor = new Color(0.9f, 0.2f, 0.2f, 0.8f); // Red

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip failSound;

    // Internal
    private Renderer buttonRenderer;
    private Material buttonMaterial;
    private bool isPurchased = false;
    private int purchaseCount = 0;

    // Upgrade types that can be triggered by this button
    public enum UpgradeType
    {
        None,
        RegisterSpeed,
        ProduceQuality,
        PlayerCapacity,
        StoreExpansion
    }

    void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            // Create instance of material so each button can have its own color
            buttonMaterial = buttonRenderer.material;
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (isPurchased && oneTimePurchase) return;

        // Update button color based on affordability
        UpdateButtonColor();
    }

    /// <summary>
    /// Triggered when the player walks onto the button.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Only react to the player
        if (!other.CompareTag("Player")) return;
        if (isPurchased && oneTimePurchase) return;

        AttemptPurchase();
    }

    /// <summary>
    /// For buttons that allow repeated upgrades, also trigger on stay.
    /// This creates the classic Roblox tycoon "stand on button" feel.
    /// </summary>
    private float stayTimer = 0f;
    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimePurchase) return; // One-time buttons only trigger once on enter

        // For repeatable upgrades, trigger every 0.5 seconds while standing
        stayTimer += Time.deltaTime;
        if (stayTimer >= 0.5f)
        {
            stayTimer = 0f;
            AttemptPurchase();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            stayTimer = 0f;
        }
    }

    /// <summary>
    /// Try to make the purchase.
    /// </summary>
    private void AttemptPurchase()
    {
        TycoonManager manager = TycoonManager.Instance;
        if (manager == null) return;

        if (manager.CanAfford(cost))
        {
            // === SUCCESS! ===
            
            // Trigger the appropriate upgrade action
            switch (upgradeType)
            {
                case UpgradeType.RegisterSpeed:
                    manager.UpgradeRegisterSpeed(cost);
                    break;
                case UpgradeType.ProduceQuality:
                    manager.UpgradeProduceQuality(cost);
                    break;
                case UpgradeType.PlayerCapacity:
                    manager.UpgradePlayerCapacity(cost);
                    break;
                case UpgradeType.None:
                default:
                    manager.SpendMoney(cost);
                    break;
            }

            // Unlock the target object (if assigned)
            if (unlockTarget != null)
            {
                unlockTarget.Unlock();
            }

            // Play purchase sound
            if (purchaseSound != null)
            {
                AudioSource.PlayClipAtPoint(purchaseSound, transform.position);
            }

            // Visual feedback - quick scale bounce
            StartCoroutine(PurchaseBounceEffect());

            purchaseCount++;

            if (oneTimePurchase)
            {
                // Hide the button permanently
                isPurchased = true;
                StartCoroutine(FadeOutAndDisable());
            }
            else
            {
                // Increase cost for next purchase
                cost = Mathf.RoundToInt(cost * costMultiplier);
                UpdateDisplay();
            }
        }
        else
        {
            // === NOT ENOUGH MONEY ===
            if (failSound != null)
            {
                AudioSource.PlayClipAtPoint(failSound, transform.position);
            }

            // Visual feedback - shake the button
            StartCoroutine(FailShakeEffect());
        }
    }

    /// <summary>
    /// Update the price text display.
    /// </summary>
    private void UpdateDisplay()
    {
        if (priceText != null)
        {
            priceText.text = $"{displayName}\n${cost}";
        }
    }

    /// <summary>
    /// Update button color based on whether the player can afford it.
    /// </summary>
    private void UpdateButtonColor()
    {
        if (buttonMaterial == null) return;

        TycoonManager manager = TycoonManager.Instance;
        if (manager == null) return;

        Color targetColor = manager.CanAfford(cost) ? affordableColor : expensiveColor;
        buttonMaterial.color = Color.Lerp(buttonMaterial.color, targetColor, Time.deltaTime * 5f);

        // Add emission glow
        buttonMaterial.SetColor("_EmissionColor", targetColor * 0.5f);
    }

    // --- Visual Effects (Coroutines) ---

    private System.Collections.IEnumerator PurchaseBounceEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 bounceScale = originalScale * 1.2f;

        float t = 0;
        while (t < 0.15f)
        {
            transform.localScale = Vector3.Lerp(originalScale, bounceScale, t / 0.15f);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0;
        while (t < 0.15f)
        {
            transform.localScale = Vector3.Lerp(bounceScale, originalScale, t / 0.15f);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private System.Collections.IEnumerator FailShakeEffect()
    {
        Vector3 originalPos = transform.position;
        for (int i = 0; i < 6; i++)
        {
            transform.position = originalPos + new Vector3(Random.Range(-0.05f, 0.05f), 0, Random.Range(-0.05f, 0.05f));
            yield return new WaitForSeconds(0.03f);
        }
        transform.position = originalPos;
    }

    private System.Collections.IEnumerator FadeOutAndDisable()
    {
        // Shrink and fade the button after purchase
        Vector3 originalScale = transform.localScale;
        float t = 0;
        while (t < 0.5f)
        {
            float progress = t / 0.5f;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            t += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
