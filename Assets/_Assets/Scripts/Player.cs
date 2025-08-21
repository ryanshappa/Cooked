using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Collision")]
    [SerializeField] float playerRadius = 0.7f;
    [SerializeField] float playerHeight = 2f;
    [SerializeField] LayerMask collisionLayers = -1;

    [Header("Refs")]
    [SerializeField] Transform cameraHolder;     // head anchor
    [SerializeField] GameInput input;            // drag the GameInput in Inspector

    Vector2 moveInput;
    bool isWalking;

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Get input
        Vector2 inputVector = input.ReadMove();
        
        // Calculate movement direction relative to camera
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        
        // Convert to world space relative to camera
        Vector3 fwd = cameraHolder.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cameraHolder.right; right.y = 0; right.Normalize();
        moveDir = (fwd * moveDir.z + right * moveDir.x);
        
        float moveDistance = moveSpeed * Time.deltaTime;
        
        // Try to move with collision detection
        bool canMove = !Physics.CapsuleCast(
            transform.position, 
            transform.position + Vector3.up * playerHeight, 
            playerRadius, 
            moveDir, 
            moveDistance,
            collisionLayers
        );

        if (!canMove)
        {
            // Cannot move towards moveDir
            
            // Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0);
            canMove = !Physics.CapsuleCast(
                transform.position, 
                transform.position + Vector3.up * playerHeight, 
                playerRadius, 
                moveDirX, 
                moveDistance,
                collisionLayers
            );

            if (canMove)
            {
                // Can move only on the X
                moveDir = moveDirX;
            }
            else
            {
                // Cannot move only on the X
                
                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z);
                canMove = !Physics.CapsuleCast(
                    transform.position, 
                    transform.position + Vector3.up * playerHeight, 
                    playerRadius, 
                    moveDirZ, 
                    moveDistance,
                    collisionLayers
                );

                if (canMove)
                {
                    // Can move only on the Z
                    moveDir = moveDirZ;
                }
                else
                {
                    // Cannot move in any direction
                }
            }
        }

        if (canMove)
        {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        // Face camera yaw (optional)
        if (fwd.sqrMagnitude > 0.0001f)
            transform.forward = Vector3.Slerp(transform.forward, fwd, 20f * Time.deltaTime);
    }

    // Animator helpers (if you need them)
    public bool IsWalking() => isWalking;
    public Vector2 GetMoveInput() => input.ReadMove();
}
