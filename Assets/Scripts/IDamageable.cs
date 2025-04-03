using UnityEngine;

/// <summary>
/// Interface for any object that can take damage
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    void TakeDamage(float damage);
}

