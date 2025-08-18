using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [Header("Actions")]
    [SerializeField] private InputActionAsset actionsAsset;   // your InputSystem_Actions

    // maps
    private InputActionMap playerMap;
    private InputActionMap uiMap;

    // actions
    public InputAction Move { get; private set; }
    public InputAction Look { get; private set; }
    public InputAction Jump { get; private set; }
    public InputAction Sprint { get; private set; }
    public InputAction Interact { get; private set; }

    // simple "service" access (optional)
    public static GameInput Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        // Find action maps
        playerMap = actionsAsset.FindActionMap("Player", throwIfNotFound: true);
        uiMap = actionsAsset.FindActionMap("UI", throwIfNotFound: false);

        // Find actions (set throwIfNotFound to false for optional actions)
        Move = playerMap.FindAction("Move", true);
        Look = playerMap.FindAction("Look", true);
        Jump = playerMap.FindAction("Jump", false);
        Sprint = playerMap.FindAction("Sprint", false);
        Interact = playerMap.FindAction("Interact", false);
    }

    void OnEnable()
    {
        // Enable the actions asset
        actionsAsset?.Enable();
        
        // Start with player map enabled, UI disabled
        playerMap?.Enable();
        uiMap?.Disable();
    }

    void OnDisable()
    {
        actionsAsset?.Disable();
    }

    public void SetPlayerInputActive(bool active)
    {
        if (active) 
            playerMap?.Enable();
        else 
            playerMap?.Disable();
    }

    public void SetUIInputActive(bool active)
    {
        if (active) 
            uiMap?.Enable();
        else 
            uiMap?.Disable();
    }

    // Convenience helpers for reading input values
    public Vector2 ReadMove() => Move?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 ReadLook() => Look?.ReadValue<Vector2>() ?? Vector2.zero;
    public bool IsJumpPressed() => Jump?.WasPressedThisFrame() ?? false;
    public bool IsJumpHeld() => Jump?.IsPressed() ?? false;
    public bool IsSprintHeld() => Sprint?.IsPressed() ?? false;
    public bool IsInteractPressed() => Interact?.WasPressedThisFrame() ?? false;
}
