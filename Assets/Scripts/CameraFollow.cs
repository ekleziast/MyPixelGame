using UnityEngine;

/// <summary>
/// CameraFollow script for a 2D open world game.
/// This script handles smooth camera following of the player character while respecting defined boundaries.
/// It ensures the camera only moves within generated or discovered areas of the game world.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform of the player or object to follow")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("How quickly the camera moves to the target position")]
    [Range(0.1f, 10f)]
    public float smoothSpeed = 3.0f;
    
    [Tooltip("Offset from the target position")]
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Boundary Settings")]
    [Tooltip("Enable boundaries to restrict camera movement")]
    public bool useBoundaries = true;
    
    [Tooltip("Minimum X position the camera can move to")]
    public float minX = -Mathf.Infinity;
    
    [Tooltip("Maximum X position the camera can move to")]
    public float maxX = Mathf.Infinity;
    
    [Tooltip("Minimum Y position the camera can move to")]
    public float minY = -Mathf.Infinity;
    
    [Tooltip("Maximum Y position the camera can move to")]
    public float maxY = Mathf.Infinity;

    [Header("Advanced Settings")]
    [Tooltip("Keep Z position fixed to the offset value")]
    public bool fixedZPosition = true;
    
    [Tooltip("Use a deadzone where small target movements don't move the camera")]
    public bool useDeadZone = false;
    
    [Tooltip("Size of the deadzone (distance target can move without moving camera)")]
    public float deadZoneSize = 0.1f;

    // Reference to the camera's dimensions for boundary calculations
    private float cameraHeight;
    private float cameraWidth;
    private Camera mainCamera;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes camera dimensions for boundary calculations.
    /// </summary>
    private void Awake()
    {
        // Get a reference to the main camera
        mainCamera = GetComponent<Camera>();
        
        if (mainCamera == null)
        {
            Debug.LogError("CameraFollow script requires a Camera component on the same GameObject.");
            enabled = false;
            return;
        }
        
        // Calculate camera dimensions for boundary handling
        CalculateCameraDimensions();
    }

    /// <summary>
    /// Calculate the width and height of the camera view in world units.
    /// </summary>
    private void CalculateCameraDimensions()
    {
        cameraHeight = 2f * mainCamera.orthographicSize;
        cameraWidth = cameraHeight * mainCamera.aspect;
    }

    /// <summary>
    /// Called after all Update functions have been called.
    /// Handles the smooth following of the target.
    /// </summary>
    private void LateUpdate()
    {
        // Don't follow if no target is assigned
        if (target == null)
        {
            Debug.LogWarning("CameraFollow has no target assigned. Please assign a target in the Inspector.");
            return;
        }

        // Get the desired position (target position + offset)
        Vector3 desiredPosition = target.position + offset;
        
        // Check if target movement is within deadzone
        if (useDeadZone)
        {
            Vector3 currentPositionWithoutZ = transform.position;
            currentPositionWithoutZ.z = 0;
            
            Vector3 targetPositionWithoutZ = target.position;
            targetPositionWithoutZ.z = 0;
            
            // If the target hasn't moved beyond the deadzone, don't move the camera
            if (Vector3.Distance(currentPositionWithoutZ, targetPositionWithoutZ) < deadZoneSize)
            {
                desiredPosition = transform.position;
            }
        }
        
        // Apply camera boundaries if enabled
        if (useBoundaries)
        {
            // Apply the boundaries with half-size offsets to ensure the camera view stays within bounds
            float halfWidth = cameraWidth * 0.5f;
            float halfHeight = cameraHeight * 0.5f;
            
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX + halfWidth, maxX - halfWidth);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY + halfHeight, maxY - halfHeight);
        }
        
        // Keep Z position fixed to the offset if enabled
        if (fixedZPosition)
        {
            desiredPosition.z = offset.z;
        }
        
        // Smoothly move the camera to the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// Dynamically update camera boundaries based on newly generated or discovered areas.
    /// Call this method from your world generation or fog-of-war system when new areas are revealed.
    /// </summary>
    /// <param name="newMinX">New minimum X boundary</param>
    /// <param name="newMaxX">New maximum X boundary</param>
    /// <param name="newMinY">New minimum Y boundary</param>
    /// <param name="newMaxY">New maximum Y boundary</param>
    public void UpdateBoundaries(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
        
        Debug.Log($"Camera boundaries updated: X({minX} to {maxX}), Y({minY} to {maxY})");
    }

    /// <summary>
    /// Immediately center the camera on the target without smoothing.
    /// Useful when teleporting the player or during scene transitions.
    /// </summary>
    public void CenterOnTarget()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            
            if (useBoundaries)
            {
                float halfWidth = cameraWidth * 0.5f;
                float halfHeight = cameraHeight * 0.5f;
                
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX + halfWidth, maxX - halfWidth);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY + halfHeight, maxY - halfHeight);
            }
            
            if (fixedZPosition)
            {
                targetPosition.z = offset.z;
            }
            
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// Sets a new target for the camera to follow.
    /// Updates the target reference and optionally centers the camera on the new target immediately.
    /// </summary>
    /// <param name="newTarget">The new Transform that the camera should follow</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CenterOnTarget(); // Immediately center on the new target
    }

    /// <summary>
    /// Called when values are changed in the inspector.
    /// Recalculates camera dimensions if the camera properties have changed.
    /// </summary>
    private void OnValidate()
    {
        if (mainCamera != null)
        {
            CalculateCameraDimensions();
        }
    }

    /// <summary>
    /// Visualize the camera boundaries in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!useBoundaries) return;
        
        Gizmos.color = Color.yellow;
        
        // Draw the camera boundary rectangle
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}

