using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// TilemapManager handles the generation and management of a chunk-based open world tilemap.
/// Features include:
/// - Forest biome generation throughout the world
/// - Chunk-based loading system for optimization
/// - Fog of war system that reveals areas as the player explores
/// - Resource distribution across the forest
/// </summary>
public class TilemapManager : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap groundTilemap;
    public Tilemap resourceTilemap;
    public Tilemap fogTilemap;

    [Header("Tile Assets")]
    public TileBase[] forestTiles;
    public TileBase fogTile;

    [Header("Resource Tiles")]
    public TileBase[] forestResources; // Trees, bushes, etc.

    [Header("Generation Settings")]
    public int chunkSize = 16;         // Size of each chunk (16x16 tiles)
    public int worldSize = 100;        // World size in chunks (resulting in worldSize x worldSize chunks)
    public int seed = 0;               // Seed for random generation

    [Header("Resource Settings")]
    [Range(0, 100)]
    public int resourceDensity = 10;   // Percentage chance of resource spawn per tile

    [Header("Fog of War")]
    public int revealRadius = 5;       // How many tiles around the player to reveal
    public bool permanentReveal = true; // If true, once revealed, tiles stay visible

    // Private variables
    private Dictionary<Vector2Int, bool> loadedChunks = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, BiomeType> biomeMap = new Dictionary<Vector2Int, BiomeType>();
    private HashSet<Vector3Int> revealedTiles = new HashSet<Vector3Int>();
    private Transform playerTransform;

    // Enum to track biome types
    public enum BiomeType
    {
        Forest
    }

    void Start()
    {
        // Initialize the system
        InitializeWorld();
        
        // Find the player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("Player not found! Fog of war will not function correctly.");
        }
        
        // Apply initial fog of war
        if (fogTilemap != null && playerTransform != null)
        {
            StartCoroutine(UpdateFogOfWarRoutine());
        }
    }

    void Update()
    {
        // Check if player moved to a new chunk and generate surrounding chunks
        if (playerTransform != null)
        {
            Vector2Int currentChunk = WorldToChunkCoordinates(playerTransform.position);
            CheckAndGenerateSurroundingChunks(currentChunk);
        }
    }

    /// <summary>
    /// Initializes the world by setting the random seed
    /// </summary>
    private void InitializeWorld()
    {
        // Set the random seed for consistent generation
        Random.InitState(seed);
        
        // If there's a player, generate the starting chunks
        if (playerTransform != null)
        {
            Vector2Int playerChunk = WorldToChunkCoordinates(playerTransform.position);
            GenerateChunk(playerChunk);
            
            // Generate surrounding chunks for initial view
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int nearbyChunk = new Vector2Int(playerChunk.x + x, playerChunk.y + y);
                    GenerateChunk(nearbyChunk);
                }
            }
        }
    }

    /// <summary>
    /// Converts world position to chunk coordinates
    /// </summary>
    private Vector2Int WorldToChunkCoordinates(Vector3 worldPosition)
    {
        // Convert world position to tilemap cell position, then to chunk coordinates
        Vector3Int cellPosition = groundTilemap.WorldToCell(worldPosition);
        return new Vector2Int(
            Mathf.FloorToInt(cellPosition.x / (float)chunkSize),
            Mathf.FloorToInt(cellPosition.y / (float)chunkSize)
        );
    }

    /// <summary>
    /// Checks and generates chunks surrounding the player's current chunk
    /// </summary>
    private void CheckAndGenerateSurroundingChunks(Vector2Int currentChunk)
    {
        // Generate chunks in a 3x3 area around the player
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int chunkPos = new Vector2Int(currentChunk.x + x, currentChunk.y + y);
                
                // Check if this chunk is already loaded
                if (!loadedChunks.ContainsKey(chunkPos) || !loadedChunks[chunkPos])
                {
                    GenerateChunk(chunkPos);
                }
            }
        }
    }

    /// <summary>
    /// Generates a single chunk of the world
    /// </summary>
    private void GenerateChunk(Vector2Int chunkPos)
    {
        // Mark this chunk as loaded
        loadedChunks[chunkPos] = true;
        
        // Calculate the starting position for this chunk in the tilemap
        Vector3Int chunkStartPos = new Vector3Int(
            chunkPos.x * chunkSize,
            chunkPos.y * chunkSize,
            0
        );
        
        // Generate each tile within the chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = chunkStartPos + new Vector3Int(x, y, 0);
                
                // All tiles are Forest biome
                BiomeType biome = BiomeType.Forest;
                biomeMap[new Vector2Int(tilePos.x, tilePos.y)] = biome;
                
                // Add the ground tile
                PlaceGroundTile(tilePos);
                
                // Add resources with some probability
                if (Random.Range(0, 100) < resourceDensity)
                {
                    PlaceResourceTile(tilePos, biome);
                }
                
                // Apply fog of war (if enabled)
                if (fogTilemap != null && !revealedTiles.Contains(tilePos))
                {
                    fogTilemap.SetTile(tilePos, fogTile);
                }
            }
        }
    }

    /// <summary>
    /// Returns the Forest biome type (only biome in the game)
    /// </summary>
    private BiomeType GenerateBiome(Vector3Int position)
    {
        // Always return Forest biome
        return BiomeType.Forest;
    }

    /// <summary>
    /// Places a ground tile at the specified position based on the biome
    /// </summary>
    private void PlaceGroundTile(Vector3Int position)
    {
        // Select a random forest tile
        TileBase tileToPlace = forestTiles[Random.Range(0, forestTiles.Length)];
        
        // Place the selected tile
        if (tileToPlace != null)
        {
            groundTilemap.SetTile(position, tileToPlace);
        }
    }

    /// <summary>
    /// Places a resource tile at the specified position based on the biome
    /// </summary>
    private void PlaceResourceTile(Vector3Int position, BiomeType biome)
    {
        TileBase resourceToPlace = null;
        
        // Select a random resource from the forest resources
        if (forestResources.Length > 0)
        {
            resourceToPlace = forestResources[Random.Range(0, forestResources.Length)];
        }
        
        // Place the selected resource
        if (resourceToPlace != null)
        {
            resourceTilemap.SetTile(position, resourceToPlace);
        }
    }

    /// <summary>
    /// Coroutine that updates the fog of war as the player moves
    /// </summary>
    private IEnumerator UpdateFogOfWarRoutine()
    {
        while (true)
        {
            // Update fog of war around the player
            if (playerTransform != null)
            {
                UpdateFogOfWar();
            }
            
            // Wait for a short delay to avoid updating every frame
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// Updates the fog of war around the player's current position
    /// </summary>
    private void UpdateFogOfWar()
    {
        // Convert player position to cell coordinates
        Vector3Int playerCell = groundTilemap.WorldToCell(playerTransform.position);
        
        // Reveal tiles in a radius around the player
        for (int x = -revealRadius; x <= revealRadius; x++)
        {
            for (int y = -revealRadius; y <= revealRadius; y++)
            {
                Vector3Int tilePos = playerCell + new Vector3Int(x, y, 0);
                
                // Check if this position is within the reveal radius
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= revealRadius)
                {
                    // Remove fog from this tile
                    fogTilemap.SetTile(tilePos, null);
                    
                    // Track which tiles have been revealed
                    if (permanentReveal)
                    {
                        revealedTiles.Add(tilePos);
                    }
                }
                else if (!permanentReveal && !revealedTiles.Contains(tilePos))
                {
                    // If not permanent reveal, add fog back to tiles outside the current radius
                    fogTilemap.SetTile(tilePos, fogTile);
                }
            }
        }
    }

    /// <summary>
    /// Gets the biome type at a specific world position
    /// </summary>
    public BiomeType GetBiomeAt(Vector3 worldPosition)
    {
        // Always return Forest biome
        return BiomeType.Forest;
    }

    /// <summary>
    /// Checks if a specific world position has a resource
    /// </summary>
    public bool HasResourceAt(Vector3 worldPosition)
    {
        // Convert world position to tilemap cell position
        Vector3Int cellPosition = resourceTilemap.WorldToCell(worldPosition);
        
        // Check if there's a tile at this position
        return resourceTilemap.HasTile(cellPosition);
    }

    /// <summary>
    /// Reveals the entire map (for debugging or special game events)
    /// </summary>
    public void RevealEntireMap()
    {
        // Clear all fog tiles
        fogTilemap.ClearAllTiles();
        
        // If using permanent reveal, add all tiles to revealed set
        if (permanentReveal)
        {
            // This is memory intensive, so only use for debugging
            for (int x = -worldSize * chunkSize / 2; x < worldSize * chunkSize / 2; x++)
            {
                for (int y = -worldSize * chunkSize / 2; y < worldSize * chunkSize / 2; y++)
                {
                    revealedTiles.Add(new Vector3Int(x, y, 0));
                }
            }
        }
    }

    /// <summary>
    /// Saves the current world state (can be expanded for game saving features)
    /// </summary>
    public void SaveWorldState()
    {
        // This is a placeholder for implementing save functionality
        Debug.Log("World state saving would be implemented here");
        
        // Potential information to save:
        // - Generated chunks
        // - Biome map
        // - Resource positions
        // - Revealed tiles
    }
}

