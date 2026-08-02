using UnityEngine;

/// A display prop (fridge shelf item, crate content): Use with empty hands
/// to take a fresh copy of its ingredient. The prop itself never depletes.
public class IngredientGrabPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    [SerializeField] private FridgeCounter fridge;   // optional: only grabbable while the fridge is open

    public void Interact(Transform interactor)
    {
        if (fridge != null && !fridge.IsOpen) return;

        var carry = interactor.GetComponent<PlayerCarry>();
        if (carry == null || carry.HasKitchenObject()) return;

        KitchenObject.Spawn(kitchenObjectSO, carry);
    }

    public string GetInteractText() =>
        kitchenObjectSO != null ? $"Take {kitchenObjectSO.objectName}" : "Take";

    public Transform GetTransform() => transform;
}
