using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the movement, rotation, and lifetime of a projectile.
/// Works alongside ProjectileDamage for handling collision and damage application.
/// </summary>
public class ProjectileController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Reference to the ProjectileDamage component")]
    [SerializeField] private ProjectileDamage projectileDamage;

    [Header("Settings")]
    [Tooltip("Time in seconds before the projectile is destroyed")]
    [SerializeField] private float lifetime = 5f;
    [Tooltip("Whether to automatically rotate the projectile in the direction of movement")]
    [SerializeField] private bool autoRotate = true;
    [SerializeField] private float rotationOffset = 0f;

    [Header("Visual Effects")]
    [SerializeField] private bool enableTrail = false;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem launchEffect;
    [SerializeField] private GameObject impactEffectPrefab;

    // Private fields
    private float damage;
    private bool initialized = false;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        // Ensure components are assigned
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        if (projectileDamage == null)
            projectileDamage = GetComponent<ProjectileDamage>();
            
        if (enableTrail && trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        // Disable trail until initialized
        if (trailRenderer != null)
            trailRenderer.enabled = false;
    }

    /// <summary>
    /// Initializes the projectile with the specified direction, speed, and damage.
    /// </summary>
    /// <param name="direction">The direction in which the projectile will travel</param>
    /// <param name="speed">The speed at which the projectile will travel</param>
    /// <param name="damage">The amount of damage the projectile will deal on impact</param>
    public void Initialize(Vector2 direction, float speed, float damage)
    {
        if (initialized)
            return;

        // Normalize direction and set velocity
        Vector2 normalizedDirection = direction.normalized;
        rb.linearVelocity = normalizedDirection * speed;

        // Set damage
        this.damage = damage;
        if (projectileDamage != null)
            projectileDamage.damage = damage;

        // Handle rotation
        if (autoRotate)
        {
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }

        // Enable visual effects
        if (enableTrail && trailRenderer != null)
            trailRenderer.enabled = true;

        // Play launch effect if assigned
        if (launchEffect != null)
            launchEffect.Play();

        // Start lifetime coroutine
        lifetimeCoroutine = StartCoroutine(DestroyAfterLifetime());

        initialized = true;
    }

    /// <summary>
    /// Coroutine that destroys the projectile after the specified lifetime.
    /// </summary>
    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    /// <summary>
    /// Spawns an impact effect at the current position.
    /// </summary>
    public void SpawnImpactEffect()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Gets the current damage value of the projectile.
    /// </summary>
    /// <returns>The damage value</returns>
    public float GetDamage()
    {
        return damage;
    }

    /// <summary>
    /// Cleans up any coroutines or references when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (lifetimeCoroutine != null)
            StopCoroutine(lifetimeCoroutine);
            
        // Clear references
        rb = null;
        projectileDamage = null;
        trailRenderer = null;
    }
}

