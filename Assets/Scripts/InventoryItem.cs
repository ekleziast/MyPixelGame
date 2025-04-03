using UnityEngine;

/// <summary>
/// Enum that defines the different types of items
/// </summary>
public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Quest
}

/// <summary>
/// Class that defines an inventory item with various properties
/// </summary>
public class InventoryItem : MonoBehaviour
{
    [Header("Basic Properties")]
    [Tooltip("The name of the item")]
    public string Name;
    
    [Tooltip("The description of the item")]
    [TextArea(3, 5)]
    public string Description;
    
    [Header("Item Type")]
    [Tooltip("The type of this item")]
    public ItemType Type;
    
    [Tooltip("Whether this item can be equipped")]
    public bool IsEquippable;
    
    [Tooltip("The equipment slot this item goes into (if equippable)")]
    [SerializeField]
    private EquipmentSlot _slot;
    
    [Header("Item Properties")]
    [Tooltip("The value/strength of this item (damage for weapons, defense for armor, etc.)")]
    public float Value;
    
    [Tooltip("The icon displayed in the inventory")]
    public Sprite Icon;
    
    /// <summary>
    /// Gets the equipment slot this item belongs to
    /// </summary>
    public EquipmentSlot Slot 
    { 
        get { return _slot; } 
        set { _slot = value; }
    }
    
    /// <summary>
    /// Apply the item's effect to the player
    /// </summary>
    /// <param name="player">The player controller reference</param>
    /// <returns>True if the item was used successfully</returns>
    public virtual bool Use(PlayerController player)
    {
        if (player == null)
            return false;
            
        // Implementation depends on item type
        switch (Type)
        {
            case ItemType.Consumable:
                // Add implementation for consumable items (potions, food, etc.)
                return true;
                
            case ItemType.Weapon:
            case ItemType.Armor:
                // Equippable items are handled by the equipment system
                if (IsEquippable)
                {
                    return player.EquipItem(this);
                }
                return false;
                
            case ItemType.Quest:
                // Quest items typically aren't usable directly
                return false;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Gets a description of the item with its stats
    /// </summary>
    /// <returns>A formatted string with item details</returns>
    public string GetItemStats()
    {
        string stats = $"{Name}\n{Description}\n";
        
        switch (Type)
        {
            case ItemType.Weapon:
                stats += $"Damage: {Value}";
                break;
                
            case ItemType.Armor:
                stats += $"Defense: {Value}";
                break;
                
            case ItemType.Consumable:
                stats += $"Effect Power: {Value}";
                break;
                
            case ItemType.Quest:
                stats += "Quest Item";
                break;
        }
        
        return stats;
    }
}

