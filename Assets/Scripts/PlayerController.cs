using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller for the player character, handling movement, combat, stats and progression.
/// Integrates with CameraFollow.cs for camera tracking and TilemapManager.cs for world interaction.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region Movement Variables
    [Header("Movement Parameters")]
    [Tooltip("Maximum movement speed of the player")]
    [SerializeField] private float maxSpeed = 5f;
    
    [Tooltip("How quickly the player accelerates")]
    [SerializeField] private float acceleration = 50f;
    
    [Tooltip("How quickly the player decelerates when no input is given")]
    [SerializeField] private float deceleration = 25f;
    
    [Tooltip("Multiplier applied when player is sprinting")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    
    [Header("Physics Parameters")]
    [Tooltip("Gravity scale for the player's Rigidbody2D")]
    [SerializeField] private float gravityScale = 2.5f;
    
    [Tooltip("Maximum fall velocity")]
    [SerializeField] private float maxFallVelocity = 15f;
    
    [Tooltip("Linear drag for horizontal movement")]
    [SerializeField] private float linearDrag = 0.5f;
    
    [Tooltip("Mass of the player's Rigidbody2D")]
    [SerializeField] private float playerMass = 1.0f;
    
    // Current movement vector
    private Vector2 _movementInput;
    private Vector2 _currentVelocity;
    private bool _isSprinting = false;
    #endregion

    #region Character Stats
    [Header("Base Stats")]
    [Tooltip("Maximum health points")]
    [SerializeField] private float maxHealth = 100f;
    
    [Tooltip("Current health points")]
    [SerializeField] private float currentHealth;
    
    [Tooltip("Maximum mana points")]
    [SerializeField] private float maxMana = 50f;
    
    [Tooltip("Current mana points")]
    [SerializeField] private float currentMana;
    
    [Tooltip("Maximum stamina points")]
    [SerializeField] private float maxStamina = 100f;
    
    [Tooltip("Current stamina points")]
    [SerializeField] private float currentStamina;
    
    [Tooltip("Stamina regeneration rate per second")]
    [SerializeField] private float staminaRegenRate = 10f;
    
    [Tooltip("Stamina cost per second while sprinting")]
    [SerializeField] private float sprintStaminaCost = 20f;
    #endregion

    #region RPG Attributes
    [Header("Character Attributes")]
    [Tooltip("Strength - Affects physical damage and carrying capacity")]
    [SerializeField] private int strength = 10;
    
    [Tooltip("Dexterity - Affects accuracy, dodge chance and attack speed")]
    [SerializeField] private int dexterity = 10;
    
    [Tooltip("Intelligence - Affects spell damage and mana pool")]
    [SerializeField] private int intelligence = 10;
    
    [Tooltip("Constitution - Affects health points and resistance")]
    [SerializeField] private int constitution = 10;
    
    [Tooltip("Current level of the character")]
    [SerializeField] private int level = 1;
    
    [Tooltip("Current experience points")]
    [SerializeField] private int experience = 0;
    
    [Tooltip("Experience points required to level up")]
    [SerializeField] private int experienceToNextLevel = 100;
    #endregion

    #region Component References
    // References to required components
    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private CameraFollow _cameraFollow;
    
    // Input Actions reference from Assets/Scripts/Input/PlayerInputActions.cs
    private PlayerInputActions _playerInputActions;
    #endregion

    #region Combat Variables
    [Header("Combat Parameters")]
    [Tooltip("Basic melee attack damage")]
    [SerializeField] private float meleeDamage = 10f;
    
    [Tooltip("Basic ranged attack damage")]
    [SerializeField] private float rangedDamage = 5f;
    
    [Tooltip("Melee attack range")]
    [SerializeField] private float meleeRange = 1.5f;
    
    [Tooltip("Range attack projectile prefab")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("Projectile speed")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("Attack cooldown in seconds")]
    [SerializeField] private float attackCooldown = 0.5f;
    
    // Combat tracking variables
    private bool _canAttack = true;
    private Vector2 _lastAttackDirection;
    #endregion

    #region Inventory System
    // Basic inventory capacity
    private const int InventorySize = 20;
    
    // Placeholder for inventory items
    private List<InventoryItem> _inventory = new List<InventoryItem>();
    
    // Equipped items
    private Dictionary<EquipmentSlot, InventoryItem> _equippedItems = new Dictionary<EquipmentSlot, InventoryItem>();
    #endregion

    #region Unity Lifecycle Methods
    /// <summary>
    /// Initialize the player controller
    /// </summary>
    private void Awake()
    {
        // Get and verify component references
        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
        {
            Debug.LogError("Rigidbody2D component not found on player!");
            _rigidbody = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure Rigidbody2D properties for 2D platformer physics
        _rigidbody.gravityScale = gravityScale;
        _rigidbody.linearDamping = linearDrag;
        _rigidbody.mass = playerMass;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        
        // Additional physics setup for better platformer feel
        Physics2D.queriesHitTriggers = true;
        Physics2D.queriesStartInColliders = false;
        
        Debug.Log($"Rigidbody2D configured with: Gravity Scale={gravityScale}, Drag={linearDrag}, Mass={playerMass}");
        
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogWarning("Animator component not found on player!");
        }
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer component not found on player!");
        }
        
        // Initialize stats
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;
        
        // Initialize input actions from Assets/Scripts/Input directory
        _playerInputActions = new PlayerInputActions();
        
        if (_playerInputActions != null)
        {
            Debug.Log("Input system initialized successfully");
            
            // Set up input action callbacks
            _playerInputActions.Player.Movement.performed += ctx => {
                _movementInput = ctx.ReadValue<Vector2>();
                Debug.Log($"Movement input: {_movementInput.x}, {_movementInput.y}");
            };
            _playerInputActions.Player.Movement.canceled += ctx => {
                _movementInput = Vector2.zero;
                Debug.Log("Movement input canceled");
            };
            
            _playerInputActions.Player.Sprint.performed += ctx => _isSprinting = ctx.ReadValueAsButton() && currentStamina > 0;
            _playerInputActions.Player.Sprint.canceled += ctx => _isSprinting = false;
            
            _playerInputActions.Player.MeleeAttack.performed += ctx => { if (_canAttack) PerformMeleeAttack(); };
            _playerInputActions.Player.RangedAttack.performed += ctx => { if (_canAttack) PerformRangedAttack(); };
            _playerInputActions.Player.Jump.performed += ctx => { 
                Debug.Log("Jump action performed");
                Jump();
            };
            
            // Explicitly enable the Player action map
            _playerInputActions.Player.Enable();
            Debug.Log("Player input action map enabled");
        }
        else
        {
            Debug.LogError("Failed to initialize input system!");
        }
        
        // Find and configure camera follow
        _cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(transform);
        }
        else
        {
            Debug.LogWarning("CameraFollow component not found on main camera!");
        }
    }

    /// <summary>
    /// Enable input actions when the component is enabled
    /// </summary>
    private void OnEnable()
    {
        if (_playerInputActions != null)
        {
            _playerInputActions.Enable();
            // Make sure the Player action map is enabled
            _playerInputActions.Player.Enable();
            Debug.Log("Input actions enabled");
        }
        else
        {
            Debug.LogError("Input actions not initialized in OnEnable!");
            // Try to initialize again as fallback
            _playerInputActions = new PlayerInputActions();
            _playerInputActions?.Enable();
        }
    }
    
    /// <summary>
    /// Disable input actions when the component is disabled
    /// </summary>
    private void OnDisable()
    {
        _playerInputActions.Disable();
    }

    /// <summary>
    /// Update method for framerate-independent operations (mainly visual feedback)
    /// </summary>
    private void Update()
    {
        // Update animations based on movement
        UpdateAnimations();
        
        // Regenerate stamina
        if (!_isSprinting && currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// Fixed update for physics calculations
    /// </summary>
    private void FixedUpdate()
    {
        // Handle movement with acceleration and deceleration
        HandleMovement();
        
        // Apply maximum fall velocity limit
        if (_rigidbody.linearVelocity.y < -maxFallVelocity)
        {
            Vector2 clampedVelocity = _rigidbody.linearVelocity;
            clampedVelocity.y = -maxFallVelocity;
            _rigidbody.linearVelocity = clampedVelocity;
        }
        
        // Handle sprint stamina cost
    }
    #endregion
    
    #region Jump Methods
    /// <summary>
    /// Makes the player character jump
    /// </summary>
    private void Jump()
    {
        // Example jump implementation
        if (_rigidbody != null)
        {
            // Apply upward force for jumping
            _rigidbody.AddForce(Vector2.up * 7f, ForceMode2D.Impulse);
            
            // Trigger jump animation if animator exists
            if (_animator != null)
            {
                _animator.SetTrigger("Jump");
            }
            
            // Visualize jump force in Scene view
            Debug.DrawRay(transform.position, Vector2.up * 7f, Color.yellow, 0.5f);
            Debug.Log("Player jumped!");
        }
        else
        {
            Debug.LogError("Cannot jump - Rigidbody2D is null!");
        }
    }
    #endregion

    #region Movement Methods
    /// <summary>
    /// Handles player movement with acceleration and deceleration
    /// </summary>
    private void HandleMovement()
    {
        // Sanity check: Ensure rigidbody exists
        if (_rigidbody == null)
        {
            Debug.LogError("HandleMovement: Rigidbody2D is null! Cannot apply movement.");
            return;
        }

        // Debug movement input with more details
        Debug.Log($"HandleMovement: Input Vector = ({_movementInput.x}, {_movementInput.y}), Magnitude = {_movementInput.magnitude}, IsSprinting = {_isSprinting}");
        
        // Get current rigidbody velocity to preserve vertical component
        Vector2 currentRigidbodyVelocity = _rigidbody.linearVelocity;
        Debug.Log($"Current Rigidbody Velocity: ({currentRigidbodyVelocity.x}, {currentRigidbodyVelocity.y})");
        
        // Calculate target horizontal velocity
        float currentMaxSpeed = _isSprinting ? maxSpeed * sprintMultiplier : maxSpeed;
        Vector2 targetVelocity = _movementInput * currentMaxSpeed;

        // Sanity check: Validate movement input isn't NaN or infinite
        if (float.IsNaN(_movementInput.x) || float.IsNaN(_movementInput.y) || 
            float.IsInfinity(_movementInput.x) || float.IsInfinity(_movementInput.y))
        {
            Debug.LogWarning("HandleMovement: Invalid movement input detected (NaN or Infinity). Resetting to zero.");
            _movementInput = Vector2.zero;
            targetVelocity = Vector2.zero;
        }
        
        // Apply acceleration or deceleration to both horizontal and vertical movement
        if (_movementInput.magnitude > 0.1f)
        {
            // Apply acceleration
            _currentVelocity = Vector2.MoveTowards(
                _currentVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime
            );
        }
        else
        {
            // Apply deceleration when no input
            _currentVelocity = Vector2.MoveTowards(
                _currentVelocity,
                Vector2.zero,
                deceleration * Time.fixedDeltaTime
            );
        }

        // Sanity check: Clamp velocity to prevent extreme values
        float maxAllowedSpeed = currentMaxSpeed * 1.5f; // Allow some buffer over max speed
        _currentVelocity.x = Mathf.Clamp(_currentVelocity.x, -maxAllowedSpeed, maxAllowedSpeed);
        _currentVelocity.y = Mathf.Clamp(_currentVelocity.y, -maxAllowedSpeed, maxAllowedSpeed);
        
        // Use both horizontal and vertical velocity from player input
        Vector2 newVelocity = new Vector2(_currentVelocity.x, _currentVelocity.y);
        
        // Draw debug visualization for movement in the Scene view
        Debug.DrawRay(transform.position, _movementInput * 2f, Color.blue, 0.1f); // Input direction
        Debug.DrawRay(transform.position, _currentVelocity, Color.green, 0.1f); // Current velocity
        Debug.DrawRay(transform.position, newVelocity, Color.red, 0.1f); // Final velocity

        // Apply velocity to rigidbody
        _rigidbody.linearVelocity = newVelocity;
        
        // If we're moving, remember direction for attacks
        if (_movementInput.magnitude > 0.1f)
        {
            _lastAttackDirection = _movementInput.normalized;
        }

        // Show additional debug visualization for movement state
        if (Application.isEditor)
        {
            string movementState = _movementInput.magnitude > 0.1f ? "Moving" : "Idle";
            string sprintState = _isSprinting ? "Sprinting" : "Normal";
            Debug.Log($"Movement State: {movementState}, Speed Mode: {sprintState}, Current Speed: {_currentVelocity.magnitude:F2}/{currentMaxSpeed:F2}");
        }
    }

    /// <summary>
    /// Handles sprint stamina consumption
    /// </summary>
    private void HandleSprinting()
    {
        if (_isSprinting && _movementInput.magnitude > 0.1f)
        {
            currentStamina -= sprintStaminaCost * Time.fixedDeltaTime;
            
            // Stop sprinting if stamina depleted
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                _isSprinting = false;
            }
        }
    }

    /// <summary>
    /// Updates animations based on movement
    /// </summary>
    private void UpdateAnimations()
    {
        if (_animator != null)
        {
            // Set animation parameters based on movement
            _animator.SetFloat("Speed", _currentVelocity.magnitude);
            _animator.SetBool("IsSprinting", _isSprinting);
            
            // Set direction parameters for animation blending
            if (_movementInput.magnitude > 0.1f)
            {
                _animator.SetFloat("Horizontal", _movementInput.x);
                _animator.SetFloat("Vertical", _movementInput.y);
            }
        }
        
        // Flip sprite based on movement direction (if using side-view sprites)
        if (_spriteRenderer != null && Mathf.Abs(_movementInput.x) > 0.1f)
        {
            _spriteRenderer.flipX = _movementInput.x < 0;
        }
    }
    #endregion

    #region Collision Methods
    /// <summary>
    /// Called when the player collides with another collider
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10f); // Example damage value
        }
        
        // Check if we hit an obstacle
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Handle obstacle interaction
            Debug.Log("Hit obstacle: " + collision.gameObject.name);
        }
    }

    /// <summary>
    /// Checks if the player can move to a given position without colliding with the environment
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <returns>True if the position is valid for movement</returns>
    private bool IsValidPosition(Vector2 position)
    {
        // Use TilemapManager to check if a position is valid for movement
        TilemapManager tilemapManager = FindFirstObjectByType<TilemapManager>();
        if (tilemapManager != null)
        {
            // A position is valid if it does NOT have a resource at that location
            return !tilemapManager.HasResourceAt(position);
        }
        
        return true; // Default to true if TilemapManager not found
    }
    #endregion

    #region Combat Methods
    /// <summary>
    /// Performs a melee attack in the current facing direction
    /// </summary>
    private void PerformMeleeAttack()
    {
        // Start attack cooldown
        StartCoroutine(AttackCooldown());
        
        // Trigger attack animation
        if (_animator != null)
        {
            _animator.SetTrigger("MeleeAttack");
        }
        
        // Calculate attack direction
        Vector2 attackDirection = _lastAttackDirection;
        
        // Get hit position
        Vector2 hitPosition = (Vector2)transform.position + (attackDirection * meleeRange);
        
        // Detect enemies in range
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(hitPosition, meleeRange / 2f);
        
        // Apply damage to enemies
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                // Apply damage to enemy
                // Apply damage to enemy
                // Get the IDamageable interface from the enemy
                IDamageable enemy = hitCollider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(meleeDamage);
                }
                Debug.Log("Hit enemy with melee attack: " + hitCollider.name);
            }
        }
    }

    /// <summary>
    /// Performs a ranged attack by spawning a projectile
    /// </summary>
    private void PerformRangedAttack()
    {
        // Check if we have enough mana
        if (currentMana < 10f) // Example cost
        {
            Debug.Log("Not enough mana for ranged attack!");
            return;
        }
        
        // Use mana
        currentMana -= 10f; // Example cost
        
        // Start attack cooldown
        StartCoroutine(AttackCooldown());
        
        // Trigger attack animation
        if (_animator != null)
        {
            _animator.SetTrigger("RangedAttack");
        }
        
        // Calculate attack direction
        Vector2 attackDirection = _lastAttackDirection;
        
        // Spawn projectile
            GameObject projectile = Instantiate(
                projectilePrefab,
                transform.position,
                Quaternion.identity
            );
            
            // Get projectile component and set properties
            ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
            if (projectileController != null)
            {
                projectileController.Initialize(attackDirection, projectileSpeed, rangedDamage);
            }
            else
            {
                // If no custom controller, just apply physics force
                Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                if (projectileRb != null)
                {
                    projectileRb.linearVelocity = attackDirection * projectileSpeed;
                }
                
                // Add damage script to the projectile if it doesn't have a controller
                ProjectileDamage damageBehavior = projectile.AddComponent<ProjectileDamage>();
                damageBehavior.damage = rangedDamage;
                
                // Destroy after 5 seconds if no hit occurs
                Destroy(projectile, 5f);
            }
            
            Debug.Log("Fired projectile in direction: " + attackDirection);
    }
    
    #endregion

    /// <summary>
    /// Coroutine to handle attack cooldown period
    /// </summary>
    private IEnumerator AttackCooldown()
    {
        _canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        _canAttack = true;
    }
    
    #region Damage and Health Methods
    /// <summary>
    /// Applies damage to the player
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    public void TakeDamage(float damageAmount)
    {
        // Apply damage reduction from armor/stats if needed
        float reducedDamage = CalculateDamageReduction(damageAmount);
        
        // Apply damage
        currentHealth -= reducedDamage;
        
        // Trigger damage animation/effects
        if (_animator != null)
        {
            _animator.SetTrigger("Hit");
        }
        
        // Apply knockback or other effects
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"Player took {reducedDamage} damage. Health: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Calculates damage reduction based on player stats and equipment
    /// </summary>
    /// <param name="incomingDamage">The raw incoming damage</param>
    /// <returns>The reduced damage amount</returns>
    private float CalculateDamageReduction(float incomingDamage)
    {
        // Example formula - can be adjusted based on game balance
        float armorValue = GetTotalArmorValue();
        float constitutionBonus = constitution * 0.5f;
        
        float damageReduction = Mathf.Clamp(armorValue + constitutionBonus, 0, 75) / 100f; // Cap at 75% reduction
        
        return incomingDamage * (1 - damageReduction);
    }
    
    /// <summary>
    /// Calculates total armor value from equipped items
    /// </summary>
    /// <returns>The total armor value</returns>
    private float GetTotalArmorValue()
    {
        float totalArmor = 0;
        
        // Add base armor from constitution
        totalArmor += constitution * 0.5f;
        
        // Add armor from equipped items
        foreach (var item in _equippedItems.Values)
        {
            // Example: armor calculation based on item value and type
            if (item.Type == ItemType.Armor)
            {
                totalArmor += item.Value;
            }
        }
        
        return totalArmor;
    }
    
    /// <summary>
    /// Heal the player for a specified amount
    /// </summary>
    /// <param name="healAmount">Amount to heal</param>
    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        Debug.Log($"Player healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        // Trigger death animation
        if (_animator != null)
        {
            _animator.SetTrigger("Death");
        }
        
        // Disable controls
        enabled = false;
        
        // Handle game over or respawn logic
        Debug.Log("Player has died!");
        
        // Example: Respawn after delay
        StartCoroutine(RespawnAfterDelay(3f));
    }
    
    /// <summary>
    /// Respawn the player after a delay
    /// </summary>
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Reset health and position
        currentHealth = maxHealth;
        transform.position = Vector3.zero; // Or use a spawn point
        
        // Re-enable controls
        enabled = true;
        
        Debug.Log("Player respawned");
    }
    #endregion
    
    #region Progression Methods
    /// <summary>
    /// Award experience points to the player
    /// </summary>
    /// <param name="amount">Amount of experience to award</param>
    public void GainExperience(int amount)
    {
        experience += amount;
        
        // Check for level up
        if (experience >= experienceToNextLevel)
        {
            LevelUp();
        }
        
        Debug.Log($"Gained {amount} XP. Total: {experience}/{experienceToNextLevel}");
    }
    
    /// <summary>
    /// Level up the character and increase stats
    /// </summary>
    private void LevelUp()
    {
        // Increase level
        level++;
        
        // Calculate leftover experience
        experience -= experienceToNextLevel;
        
        // Increase experience required for next level
        experienceToNextLevel = CalculateNextLevelExperience();
        
        // Increase base stats
        maxHealth += 10f;
        maxMana += 5f;
        maxStamina += 5f;
        
        // Fully restore health, mana, and stamina
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;
        
        // Trigger level up effects/animation
        if (_animator != null)
        {
            _animator.SetTrigger("LevelUp");
        }
        
        Debug.Log($"Level up! Now level {level}");
        
        // Check if we should level up again
        if (experience >= experienceToNextLevel)
        {
            LevelUp();
        }
    }
    
    /// <summary>
    /// Calculate experience needed for the next level
    /// </summary>
    /// <returns>Experience points needed</returns>
    private int CalculateNextLevelExperience()
    {
        // Example formula: each level requires 20% more XP than the previous
        return Mathf.RoundToInt(experienceToNextLevel * 1.2f);
    }
    
    /// <summary>
    /// Increase a specific attribute (strength, dexterity, etc.)
    /// </summary>
    /// <param name="attributeName">Name of the attribute to increase</param>
    /// <param name="amount">Amount to increase</param>
    public void IncreaseAttribute(string attributeName, int amount)
    {
        switch (attributeName.ToLower())
        {
            case "strength":
                strength += amount;
                Debug.Log($"Strength increased to {strength}");
                break;
            case "dexterity":
                dexterity += amount;
                Debug.Log($"Dexterity increased to {dexterity}");
                break;
            case "intelligence":
                intelligence += amount;
                maxMana += amount * 5f; // Intelligence increases mana
                currentMana = maxMana;
                Debug.Log($"Intelligence increased to {intelligence}");
                break;
            case "constitution":
                constitution += amount;
                maxHealth += amount * 5f; // Constitution increases health
                currentHealth = maxHealth;
                Debug.Log($"Constitution increased to {constitution}");
                break;
            default:
                Debug.LogWarning($"Unknown attribute: {attributeName}");
                break;
        }
    }
    #endregion
    
    #region Inventory Methods
    /// <summary>
    /// Add an item to the player's inventory
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns>True if item was added, false if inventory is full</returns>
    public bool AddItem(InventoryItem item)
    {
        if (_inventory.Count >= InventorySize)
        {
            Debug.Log("Inventory is full!");
            return false;
        }
        
        _inventory.Add(item);
        Debug.Log($"Added {item.Name} to inventory");
        return true;
    }
    /// <summary>
    /// Remove an item from the player's inventory
    /// </summary>
    /// <param name="itemName">Name of the item to remove</param>
    /// <returns>True if item was removed, false if not found</returns>
    public bool RemoveItem(string itemName)
    {
        InventoryItem itemToRemove = _inventory.Find(item => item.Name == itemName);
        
        if (itemToRemove != null)
        {
            _inventory.Remove(itemToRemove);
            Debug.Log($"Removed {itemName} from inventory");
            return true;
        }
        
        Debug.Log($"Item {itemName} not found in inventory");
        return false;
    }
    
    /// <summary>
    /// Equip an item to the appropriate slot
    /// </summary>
    /// <param name="itemName">Name of the item to equip</param>
    /// <returns>True if item was equipped, false otherwise</returns>
    public bool EquipItem(string itemName)
    {
        // Find the item in inventory
        InventoryItem itemToEquip = _inventory.Find(item => item.Name == itemName && item.IsEquippable);
        
        if (itemToEquip == null)
        {
            Debug.Log($"Item {itemName} not found or not equippable");
            return false;
        }
        
        return EquipItem(itemToEquip);
    }
    
    /// <summary>
    /// Equip an item to the appropriate slot
    /// </summary>
    /// <param name="itemToEquip">The item to equip</param>
    /// <returns>True if item was equipped, false otherwise</returns>
    public bool EquipItem(InventoryItem itemToEquip)
    {
        if (itemToEquip == null || !itemToEquip.IsEquippable)
        {
            Debug.Log("Item is null or not equippable");
            return false;
        }
        
        // Check if something is already equipped in that slot
        if (_equippedItems.ContainsKey(itemToEquip.Slot))
        {
            // Unequip current item
            InventoryItem currentItem = _equippedItems[itemToEquip.Slot];
            _equippedItems.Remove(itemToEquip.Slot);
            Debug.Log($"Unequipped {currentItem.Name}");
        }
        
        // Equip the new item
        _equippedItems[itemToEquip.Slot] = itemToEquip;
        Debug.Log($"Equipped {itemToEquip.Name} in {itemToEquip.Slot} slot");
        
        // Apply item stats or effects
        UpdateEquipmentStats();
        
        return true;
    }
    
    /// <summary>
    /// Unequip an item from its slot
    /// </summary>
    /// <param name="slot">The equipment slot to unequip</param>
    /// <returns>True if an item was unequipped, false otherwise</returns>
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (_equippedItems.ContainsKey(slot))
        {
            InventoryItem item = _equippedItems[slot];
            _equippedItems.Remove(slot);
            Debug.Log($"Unequipped {item.Name}");
            
            // Update stats
            UpdateEquipmentStats();
            
            return true;
        }
        
        Debug.Log($"No item equipped in {slot} slot");
        return false;
    }
    
    /// <summary>
    /// Update player stats based on equipped items
    /// </summary>
    private void UpdateEquipmentStats()
    {
        // Reset to base stats first
        // Then apply bonuses from equipped items
        // This is just a placeholder implementation
        Debug.Log("Updated equipment stats");
    }
    
    /// <summary>
    /// Use a consumable item from inventory
    /// </summary>
    /// <param name="itemName">Name of the item to use</param>
    /// <returns>True if item was used, false otherwise</returns>
    public bool UseItem(string itemName)
    {
        InventoryItem itemToUse = _inventory.Find(item => 
            item.Name == itemName && item.Type == ItemType.Consumable);
        
        if (itemToUse == null)
        {
            Debug.Log($"Item {itemName} not found or not usable");
            return false;
        }
        
        // Apply item effect (example)
        switch (itemToUse.Name.ToLower())
        {
            case "health potion":
                Heal(50f);
                break;
            case "mana potion":
                currentMana = Mathf.Min(currentMana + 30f, maxMana);
                Debug.Log($"Restored mana. Mana: {currentMana}/{maxMana}");
                break;
            case "stamina potion":
                currentStamina = maxStamina;
                Debug.Log("Restored stamina to maximum");
                break;
            default:
                Debug.Log($"Used {itemToUse.Name}");
                break;
        }
        
        // Remove the item after use
        _inventory.Remove(itemToUse);
        
        return true;
    }
    #endregion
    
    #region Camera Integration Methods
    /// <summary>
    /// Set the target for the camera to follow
    /// </summary>
    /// <param name="target">The transform that the camera should follow</param>
    public void SetTarget(Transform target)
    {
        // Find the camera follow component if not already assigned
        if (_cameraFollow == null)
        {
            _cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }
        
        // Set the target
        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(target);
            Debug.Log("Camera target set to: " + target.name);
        }
        else
        {
            Debug.LogWarning("CameraFollow component not found on main camera!");
        }
    }
    
    /// <summary>
    /// Update camera boundaries based on current area
    /// </summary>
    /// <param name="minBounds">Minimum boundaries (bottom-left)</param>
    /// <param name="maxBounds">Maximum boundaries (top-right)</param>
    public void UpdateCameraBounds(Vector2 minBounds, Vector2 maxBounds)
    {
        if (_cameraFollow == null)
        {
            _cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }
        
        if (_cameraFollow != null)
        {
            _cameraFollow.UpdateBoundaries(minBounds.x, maxBounds.x, minBounds.y, maxBounds.y);
            Debug.Log("Camera boundaries updated");
        }
    }
    #endregion
    
    #region Utility Methods
    /// <summary>
    /// Get the current health of the player
    /// </summary>
    /// <returns>Current health value</returns>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Get the maximum health of the player
    /// </summary>
    /// <returns>Maximum health value</returns>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Get the current health as a percentage (0-1)
    /// </summary>
    /// <returns>Health percentage between 0 and 1</returns>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Get the current mana of the player
    /// </summary>
    /// <returns>Current mana value</returns>
    public float GetCurrentMana()
    {
        return currentMana;
    }
    
    /// <summary>
    /// Get the maximum mana of the player
    /// </summary>
    /// <returns>Maximum mana value</returns>
    public float GetMaxMana()
    {
        return maxMana;
    }
    
    /// <summary>
    /// Get the current mana as a percentage (0-1)
    /// </summary>
    /// <returns>Mana percentage between 0 and 1</returns>
    public float GetManaPercentage()
    {
        return currentMana / maxMana;
    }
    
    /// <summary>
    /// Get the current stamina of the player
    /// </summary>
    /// <returns>Current stamina value</returns>
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
    
    /// <summary>
    /// Get the maximum stamina of the player
    /// </summary>
    /// <returns>Maximum stamina value</returns>
    public float GetMaxStamina()
    {
        return maxStamina;
    }
    
    /// <summary>
    /// Get the current stamina as a percentage (0-1)
    /// </summary>
    /// <returns>Stamina percentage between 0 and 1</returns>
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
    
    /// <summary>
    /// Get the player's current level
    /// </summary>
    /// <returns>Player level</returns>
    public int GetPlayerLevel()
    {
        return level;
    }
    
    /// <summary>
    /// Get the player's current experience points
    /// </summary>
    /// <returns>Current experience points</returns>
    public int GetExperience()
    {
        return experience;
    }
    
    /// <summary>
    /// Get the experience required for the next level
    /// </summary>
    /// <returns>Experience needed for next level</returns>
    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }
    
    /// <summary>
    /// Get the experience progress as a percentage (0-1)
    /// </summary>
    /// <returns>Experience progress percentage between 0 and 1</returns>
    public float GetExperiencePercentage()
    {
        return (float)experience / experienceToNextLevel;
    }
    
    /// <summary>
    /// Get a specific attribute value
    /// </summary>
    /// <param name="attributeName">Name of the attribute ("strength", "dexterity", "intelligence", "constitution")</param>
    /// <returns>Value of the requested attribute or -1 if attribute name is invalid</returns>
    public int GetAttributeValue(string attributeName)
    {
        switch (attributeName.ToLower())
        {
            case "strength":
                return strength;
            case "dexterity":
                return dexterity;
            case "intelligence":
                return intelligence;
            case "constitution":
                return constitution;
            default:
                Debug.LogWarning($"Unknown attribute: {attributeName}");
                return -1;
        }
    }
    
    /// <summary>
    /// Check if the player is currently moving
    /// </summary>
    /// <returns>True if player is moving, false otherwise</returns>
    public bool IsMoving()
    {
        return _currentVelocity.magnitude > 0.1f;
    }
    
    /// <summary>
    /// Check if the player is currently sprinting
    /// </summary>
    /// <returns>True if player is sprinting, false otherwise</returns>
    public bool IsSprinting()
    {
        return _isSprinting;
    }
    
    /// <summary>
    /// Check if the player is alive
    /// </summary>
    /// <returns>True if player is alive, false if dead</returns>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    /// <summary>
    /// Get the current movement direction of the player
    /// </summary>
    /// <returns>Normalized movement direction vector</returns>
    public Vector2 GetMovementDirection()
    {
        return _movementInput.normalized;
    }
    
    /// <summary>
    /// Get the current attack direction (last direction player was facing)
    /// </summary>
    /// <returns>Normalized attack direction vector</returns>
    public Vector2 GetAttackDirection()
    {
        return _lastAttackDirection;
    }
    
    /// <summary>
    /// Get the player's current velocity
    /// </summary>
    /// <returns>Current velocity vector</returns>
    public Vector2 GetCurrentVelocity()
    {
        return _currentVelocity;
    }
    
    /// <summary>
    /// Check if player can attack (not in cooldown)
    /// </summary>
    /// <returns>True if player can attack, false otherwise</returns>
    public bool CanAttack()
    {
        return _canAttack;
    }
    
    /// <summary>
    /// Check if the player has enough mana for a specific spell
    /// </summary>
    /// <param name="manaCost">The mana cost of the spell</param>
    /// <returns>True if player has enough mana, false otherwise</returns>
    public bool HasEnoughMana(float manaCost)
    {
        return currentMana >= manaCost;
    }
    
    /// <summary>
    /// Check if the player has enough stamina for a specific action
    /// </summary>
    /// <param name="staminaCost">The stamina cost of the action</param>
    /// <returns>True if player has enough stamina, false otherwise</returns>
    public bool HasEnoughStamina(float staminaCost)
    {
        return currentStamina >= staminaCost;
    }
    
    /// <summary>
    /// Check if player has a specific item in inventory
    /// </summary>
    /// <param name="itemName">Name of the item to check</param>
    /// <returns>True if item exists in inventory, false otherwise</returns>
    public bool HasItem(string itemName)
    {
        return _inventory.Exists(item => item.Name == itemName);
    }
    
    /// <summary>
    /// Get the count of a specific item in inventory
    /// </summary>
    /// <param name="itemName">Name of the item to count</param>
    /// <returns>Number of matching items in inventory</returns>
    public int GetItemCount(string itemName)
    {
        return _inventory.Count<InventoryItem>(item => item.Name == itemName);
    }
    
    /// <summary>
    /// Check if an equipment slot has an item equipped
    /// </summary>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>True if slot has item equipped, false otherwise</returns>
    public bool HasEquippedItem(EquipmentSlot slot)
    {
        return _equippedItems.ContainsKey(slot);
    }
    
    /// <summary>
    /// Get the name of the item equipped in a specific slot
    /// </summary>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>Name of equipped item or empty string if none</returns>
    public string GetEquippedItemName(EquipmentSlot slot)
    {
        if (_equippedItems.ContainsKey(slot))
        {
            return _equippedItems[slot].Name;
        }
        return string.Empty;
    }
    
    /// <summary>
    /// Gets the current position of the player
    /// </summary>
    /// <returns>Player's position as Vector2</returns>
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Gets the current inventory space usage
    /// </summary>
    /// <returns>Number of items in inventory</returns>
    public int GetInventoryCount()
    {
        return _inventory.Count;
    }
    
    /// <summary>
    /// Gets the maximum inventory capacity
    /// </summary>
    /// <returns>Maximum inventory size</returns>
    public int GetInventoryCapacity()
    {
        return InventorySize;
    }
    
    /// <summary>
    /// Gets the inventory usage as a percentage (0-1)
    /// </summary>
    /// <returns>Inventory usage percentage between 0 and 1</returns>
    public float GetInventoryUsagePercentage()
    {
        return (float)_inventory.Count / InventorySize;
    }
    #endregion
}
