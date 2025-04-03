using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Wrapper for the InputSystem_Actions asset, providing easy access to all input actions.
/// This class wraps around the auto-generated C# code from the Input System.
/// </summary>
public class PlayerInputActions
{
    private InputSystem_Actions _inputActions;

    /// <summary>
    /// Gets the Player action map containing all player controls.
    /// </summary>
    public PlayerActions Player { get; private set; }

    /// <summary>
    /// Gets the UI action map containing all UI input controls.
    /// </summary>
    public UIActions UI { get; private set; }

    /// <summary>
    /// Constructor: Creates a new instance of the input actions wrapper.
    /// </summary>
    public PlayerInputActions()
    {
        _inputActions = new InputSystem_Actions();
        Player = new PlayerActions(_inputActions.Player);
        UI = new UIActions(_inputActions.UI);
    }

    /// <summary>
    /// Enables all input actions.
    /// </summary>
    public void Enable()
    {
        _inputActions.Enable();
    }

    /// <summary>
    /// Disables all input actions.
    /// </summary>
    public void Disable()
    {
        _inputActions.Disable();
    }

    /// <summary>
    /// Wrapper class for all Player-related actions.
    /// </summary>
    public class PlayerActions
    {
        private InputSystem_Actions.PlayerActions _playerActions;

        /// <summary>
        /// Constructor: Initializes wrapper for player actions.
        /// </summary>
        /// <param name="playerActions">The underlying player action map</param>
        internal PlayerActions(InputSystem_Actions.PlayerActions playerActions)
        {
            _playerActions = playerActions;
        }

        /// <summary>
        /// Enables the Player action map.
        /// </summary>
        public void Enable()
        {
            _playerActions.Enable();
        }

        /// <summary>
        /// Disables the Player action map.
        /// </summary>
        public void Disable()
        {
            _playerActions.Disable();
        }

        /// <summary>
        /// Gets the movement input action (WASD, arrow keys, or gamepad stick).
        /// </summary>
        public InputAction Movement => _playerActions.Move;

        /// <summary>
        /// Gets the look input action (mouse movement or gamepad right stick).
        /// </summary>
        public InputAction Look => _playerActions.Look;

        /// <summary>
        /// Gets the melee attack input action (primary attack).
        /// </summary>
        public InputAction MeleeAttack => _playerActions.Attack;

        /// <summary>
        /// Gets the ranged attack input action (also mapped to the Attack input).
        /// </summary>
        public InputAction RangedAttack => _playerActions.Attack;

        /// <summary>
        /// Gets the interact input action (pick up items, talk to NPCs, etc.).
        /// </summary>
        public InputAction Interact => _playerActions.Interact;

        /// <summary>
        /// Gets the crouch input action.
        /// </summary>
        public InputAction Crouch => _playerActions.Crouch;

        /// <summary>
        /// Gets the jump input action.
        /// </summary>
        public InputAction Jump => _playerActions.Jump;

        /// <summary>
        /// Gets the previous item/weapon input action.
        /// </summary>
        public InputAction Previous => _playerActions.Previous;

        /// <summary>
        /// Gets the next item/weapon input action.
        /// </summary>
        public InputAction Next => _playerActions.Next;

        /// <summary>
        /// Gets the sprint input action.
        /// </summary>
        public InputAction Sprint => _playerActions.Sprint;
    }

    /// <summary>
    /// Wrapper class for all UI-related actions.
    /// </summary>
    public class UIActions
    {
        private InputSystem_Actions.UIActions _uiActions;

        /// <summary>
        /// Constructor: Initializes wrapper for UI actions.
        /// </summary>
        /// <param name="uiActions">The underlying UI action map</param>
        internal UIActions(InputSystem_Actions.UIActions uiActions)
        {
            _uiActions = uiActions;
        }

        /// <summary>
        /// Enables the UI action map.
        /// </summary>
        public void Enable()
        {
            _uiActions.Enable();
        }

        /// <summary>
        /// Disables the UI action map.
        /// </summary>
        public void Disable()
        {
            _uiActions.Disable();
        }

        /// <summary>
        /// Gets the UI navigation input action (move between UI elements).
        /// </summary>
        public InputAction Navigate => _uiActions.Navigate;

        /// <summary>
        /// Gets the UI submit input action (confirm selection).
        /// </summary>
        public InputAction Submit => _uiActions.Submit;

        /// <summary>
        /// Gets the UI cancel input action (go back or close UI).
        /// </summary>
        public InputAction Cancel => _uiActions.Cancel;

        /// <summary>
        /// Gets the UI point input action (mouse cursor position).
        /// </summary>
        public InputAction Point => _uiActions.Point;

        /// <summary>
        /// Gets the UI click input action (mouse left click).
        /// </summary>
        public InputAction Click => _uiActions.Click;

        /// <summary>
        /// Gets the UI right-click input action.
        /// </summary>
        public InputAction RightClick => _uiActions.RightClick;

        /// <summary>
        /// Gets the UI middle-click input action.
        /// </summary>
        public InputAction MiddleClick => _uiActions.MiddleClick;

        /// <summary>
        /// Gets the UI scroll wheel input action.
        /// </summary>
        public InputAction ScrollWheel => _uiActions.ScrollWheel;

        /// <summary>
        /// Gets the tracked device position input action (for VR/AR).
        /// </summary>
        public InputAction TrackedDevicePosition => _uiActions.TrackedDevicePosition;

        /// <summary>
        /// Gets the tracked device orientation input action (for VR/AR).
        /// </summary>
        public InputAction TrackedDeviceOrientation => _uiActions.TrackedDeviceOrientation;
    }
}

