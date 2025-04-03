using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Isometric Settings")]
    [SerializeField] private float isometricAngle = 45f;
    private Matrix4x4 _isometricMatrix;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [Header("Animation")]
    private Animator _animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsometricXHash = Animator.StringToHash("IsometricX");
    private static readonly int IsometricYHash = Animator.StringToHash("IsometricY");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private SpriteRenderer _spriteRenderer;

    private Rigidbody2D _rigidbody;
    private Vector2 _movementInput;
    private bool _isSprinting;
    private void Awake()
    {
        _isometricMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, isometricAngle, 0));
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private Vector2 ConvertToIsometric(Vector2 input)
    {
        // Convert 2D input to isometric space
        Vector3 input3D = new Vector3(input.x, 0, input.y);
        Vector3 isometric = _isometricMatrix.MultiplyVector(input3D);
        return new Vector2(isometric.x, isometric.z);
    }

    private Vector2 GetIsometricMovementVector(Vector2 input)
    {
        // Normalize input if magnitude is greater than 1
        if (input.magnitude > 1f)
            input.Normalize();
        
        // Convert to isometric and maintain consistent speed
        Vector2 isometricDir = ConvertToIsometric(input);
        if (isometricDir.magnitude > 0)
            isometricDir.Normalize();
            
        return isometricDir * input.magnitude;
    }

    private void HandleMovement()
    {
        // Convert input to isometric space
        Vector2 isometricMovement = GetIsometricMovementVector(_movementInput);
        
        // Calculate movement velocity
        float currentSpeed = moveSpeed * (_isSprinting ? sprintMultiplier : 1f);
        Vector2 targetVelocity = isometricMovement * currentSpeed;
        
        // Update rigidbody velocity
        _rigidbody.velocity = targetVelocity;
    }

    private void UpdateAnimations()
    {
        if (_movementInput.magnitude > 0.1f)
        {
            Vector2 isometricDirection = ConvertToIsometric(_movementInput.normalized);
            _animator.SetFloat(IsometricXHash, isometricDirection.x);
            _animator.SetFloat(IsometricYHash, isometricDirection.y);
            _animator.SetFloat(SpeedHash, _movementInput.magnitude);
            
            // Flip sprite based on movement direction
            if (isometricDirection.x != 0)
            {
                _spriteRenderer.flipX = isometricDirection.x < 0;
            }
        }
        else
        {
            _animator.SetFloat(SpeedHash, 0);
        }
        
        _animator.SetBool(IsSprintingHash, _isSprinting);
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimations();
    }

    // Method to receive input (this will be connected to your input system)
    public void OnMove(Vector2 input)
    {
        _movementInput = input;
    }

    public void OnSprint(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }
}
