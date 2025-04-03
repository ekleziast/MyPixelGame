using UnityEngine;

/// <summary>
/// Handles damage application when a projectile collides with objects
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    /// <summary>
    /// Amount of damage this projectile inflicts
    /// </summary>
    [Tooltip("Amount of damage this projectile inflicts")]
    public float damage = 10f;

    /// <summary>
    /// Layer mask for objects that can be damaged by this projectile
    /// </summary>
    [Tooltip("Layer mask for objects that can be damaged")]
    public LayerMask damageLayers = -1; // Default to all layers

    /// <summary>
    /// Whether to destroy the projectile on collision with any object
    /// </summary>
    [Tooltip("Whether to destroy the projectile on collision")]
    public bool destroyOnCollision = true;

    /// <summary>
    /// VFX prefab to spawn on hit
    /// </summary>
    [Tooltip("VFX prefab to spawn when projectile hits")]
    public GameObject hitEffectPrefab;

    /// <summary>
    /// Called when projectile collides with another collider
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    /// <summary>
    /// Called when projectile triggers with another collider
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    /// <summary>
    /// Handles collision with other objects
    /// </summary>
    /// <param name="hitObject">The object that was hit</param>
    private void HandleCollision(GameObject hitObject)
    {
        // Check if this object is in our damage layers
        if (((1 << hitObject.layer) & damageLayers.value) == 0 && damageLayers.value != -1)
        {
            return;
        }

        // Try to apply damage if the object is damageable
        IDamageable damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"Projectile hit {hitObject.name} for {damage} damage");
        }

        // Spawn hit effect if specified
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the projectile if configured to do so
        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
    }
}

