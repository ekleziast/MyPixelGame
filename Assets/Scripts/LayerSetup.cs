using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LayerSetup : MonoBehaviour
{
    [Header("Layer Settings")]
    [SerializeField] private string groundLayerName = "Ground";
    [SerializeField] private int groundLayerIndex = 6;
    [SerializeField] private bool logChanges = true;

    [Header("Tilemap Settings")]
    [SerializeField] private bool findAllTilemaps = true;
    [SerializeField] private List<Tilemap> tilemapsToSet;

    // Start is called once when the game begins
    private void Start()
    {
        if (Application.isPlaying)
        {
            ApplyLayerSettings();
        }
    }

    /// <summary>
    /// Finds all tilemaps in the scene and sets their layer to Ground
    /// </summary>
    public void ApplyLayerSettings()
    {
        if (findAllTilemaps)
        {
            // Find all tilemaps in the scene
            Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            
            if (allTilemaps.Length == 0)
            {
                Debug.LogWarning("No tilemaps found in the scene.");
                return;
            }
            
            int changedCount = 0;
            
            foreach (Tilemap tilemap in allTilemaps)
            {
                if (tilemap.gameObject.layer != groundLayerIndex)
                {
                    // Log the change if enabled
                    if (logChanges)
                    {
                        Debug.Log($"Changed layer of tilemap '{tilemap.name}' from '{LayerMask.LayerToName(tilemap.gameObject.layer)}' to '{groundLayerName}'");
                    }
                    
                    // Set the layer to Ground
                    tilemap.gameObject.layer = groundLayerIndex;
                    changedCount++;
                }
            }
            
            Debug.Log($"Layer setup complete. Changed {changedCount} out of {allTilemaps.Length} tilemaps to '{groundLayerName}' layer.");
        }
        else if (tilemapsToSet != null && tilemapsToSet.Count > 0)
        {
            // Use the manually specified tilemaps
            int changedCount = 0;
            
            foreach (Tilemap tilemap in tilemapsToSet)
            {
                if (tilemap != null && tilemap.gameObject.layer != groundLayerIndex)
                {
                    // Log the change if enabled
                    if (logChanges)
                    {
                        Debug.Log($"Changed layer of tilemap '{tilemap.name}' from '{LayerMask.LayerToName(tilemap.gameObject.layer)}' to '{groundLayerName}'");
                    }
                    
                    // Set the layer to Ground
                    tilemap.gameObject.layer = groundLayerIndex;
                    changedCount++;
                }
            }
            
            Debug.Log($"Layer setup complete. Changed {changedCount} out of {tilemapsToSet.Count} tilemaps to '{groundLayerName}' layer.");
        }
        else
        {
            Debug.LogWarning("No tilemaps specified and automatic finding is disabled. No changes made.");
        }
    }

    /// <summary>
    /// Sets the layer of all child tilemaps and colliders
    /// </summary>
    public void SetLayerForAllChildren()
    {
        int changedCount = 0;
        Tilemap[] childTilemaps = GetComponentsInChildren<Tilemap>();
        
        foreach (Tilemap tilemap in childTilemaps)
        {
            if (tilemap.gameObject.layer != groundLayerIndex)
            {
                // Log the change if enabled
                if (logChanges)
                {
                    Debug.Log($"Changed layer of child tilemap '{tilemap.name}' from '{LayerMask.LayerToName(tilemap.gameObject.layer)}' to '{groundLayerName}'");
                }
                
                // Set the layer to Ground
                tilemap.gameObject.layer = groundLayerIndex;
                changedCount++;
            }
        }
        
        // Also check for any CompositeCollider2D components
        CompositeCollider2D[] colliders = GetComponentsInChildren<CompositeCollider2D>();
        foreach (CompositeCollider2D collider in colliders)
        {
            if (collider.gameObject.layer != groundLayerIndex)
            {
                // Log the change if enabled
                if (logChanges)
                {
                    Debug.Log($"Changed layer of collider '{collider.name}' from '{LayerMask.LayerToName(collider.gameObject.layer)}' to '{groundLayerName}'");
                }
                
                // Set the layer to Ground
                collider.gameObject.layer = groundLayerIndex;
                changedCount++;
            }
        }
        
        Debug.Log($"Layer setup complete for children. Changed {changedCount} objects to '{groundLayerName}' layer.");
    }

    /// <summary>
    /// Validates the layer settings
    /// </summary>
    public void ValidateLayerSettings()
    {
        // Check if the layer exists
        string layerName = LayerMask.LayerToName(groundLayerIndex);
        if (string.IsNullOrEmpty(layerName))
        {
            Debug.LogError($"Layer index {groundLayerIndex} is not defined in the project's layer settings!");
        }
        else if (layerName != groundLayerName)
        {
            Debug.LogWarning($"Layer name mismatch: Index {groundLayerIndex} corresponds to '{layerName}' but '{groundLayerName}' was expected.");
        }
        else
        {
            Debug.Log($"Layer settings valid: '{groundLayerName}' is at index {groundLayerIndex}.");
        }
    }

#if UNITY_EDITOR
    // Editor button to manually apply the layer settings
    [UnityEditor.MenuItem("Tools/Setup Ground Layers")]
    public static void EditorApplyLayerSettings()
    {
        LayerSetup layerSetup = FindFirstObjectByType<LayerSetup>();
        if (layerSetup != null)
        {
            layerSetup.ApplyLayerSettings();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        else
        {
            Debug.LogError("No LayerSetup component found in the scene.");
        }
    }
#endif
}

