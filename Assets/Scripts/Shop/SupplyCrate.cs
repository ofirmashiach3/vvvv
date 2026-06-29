using UnityEngine;

/// <summary>
/// SupplyCrate - ארגז/משאית אספקה שהשחקן אוסף ממנו סחורה
/// כשהשחקן דורך על הארגז, הוא ממלא את המלאי שלו אוטומטית.
/// הארגז הוא אינסופי (תמיד יש סחורה) - בדיוק כמו ברובלוקס.
/// 
/// Setup Instructions:
/// 1. Create a crate model (or Cube placeholder, colored brown).
/// 2. Add a BoxCollider set to "Is Trigger".
/// 3. Attach this script.
/// 4. Place it at the store entrance or warehouse area.
/// </summary>
public class SupplyCrate : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("How many items the player picks up per second while standing on the crate")]
    [SerializeField] private float pickupRate = 4f;

    [Header("Visual Feedback")]
    [Tooltip("Particle system that plays when player is collecting (optional)")]
    [SerializeField] private ParticleSystem collectParticles;

    [Tooltip("Audio clip for pickup sound")]
    [SerializeField] private AudioClip pickupSound;

    // Internal
    private float pickupTimer = 0f;
    private bool playerIsNear = false;
    private PlayerInventory nearbyPlayerInventory;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Auto-fill player's inventory while they stand on the crate
        if (playerIsNear && nearbyPlayerInventory != null && !nearbyPlayerInventory.IsFull)
        {
            pickupTimer += Time.deltaTime;
            if (pickupTimer >= 1f / pickupRate)
            {
                pickupTimer = 0f;

                int pickedUp = nearbyPlayerInventory.PickupItems(1);
                if (pickedUp > 0)
                {
                    // Visual and audio feedback
                    if (collectParticles != null && !collectParticles.isPlaying)
                    {
                        collectParticles.Play();
                    }

                    if (pickupSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(pickupSound, 0.5f);
                    }
                }
            }
        }
        else
        {
            // Stop particles when not collecting
            if (collectParticles != null && collectParticles.isPlaying)
            {
                collectParticles.Stop();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            nearbyPlayerInventory = other.GetComponent<PlayerInventory>();
            pickupTimer = 0f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            nearbyPlayerInventory = null;
            pickupTimer = 0f;

            if (collectParticles != null)
            {
                collectParticles.Stop();
            }
        }
    }
}
