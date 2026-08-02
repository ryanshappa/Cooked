using UnityEngine;

/// Fridge station: Use toggles the (right) door, which swings on a hinge
/// pivot. Ingredients inside are IngredientGrabPoint display props — the
/// fridge itself doesn't dispense anything directly.
public class FridgeCounter : BaseCounter
{
    [Header("Door")]
    [SerializeField] private Transform doorPivot;      // empty at the hinge edge; door mesh is its child
    [SerializeField] private float openAngle = 110f;   // yaw when open
    [SerializeField] private float swingSpeed = 6f;    // lerp factor (ease-out swing)

    private bool isOpen;
    private float currentAngle;

    public bool IsOpen => isOpen;

    public override void Interact(Transform interactor)
    {
        isOpen = !isOpen;
    }

    public override string GetInteractText() => isOpen ? "Close fridge" : "Open fridge";

    public override bool CanAcceptKitchenObject() => false;

    void Update()
    {
        if (doorPivot == null) return;
        float target = isOpen ? openAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, target, swingSpeed * Time.deltaTime);
        doorPivot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}
