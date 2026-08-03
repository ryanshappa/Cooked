using System;
using UnityEngine;

/// Ingredient source (fridge, crate): Use with empty hands to take a fresh
/// ingredient. You cannot place anything on/in it.
public class ContainerCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    // For door-open animation / sound hooks later.
    public event Action OnPlayerGrabbedObject;

    public override void Interact(Transform interactor)
    {
        var carry = interactor.GetComponent<PlayerCarry>();
        if (carry == null || carry.HasKitchenObject()) return;

        KitchenObject.Spawn(kitchenObjectSO, carry);
        OnPlayerGrabbedObject?.Invoke();
    }

    public override string GetInteractText() =>
        kitchenObjectSO != null ? $"Take {kitchenObjectSO.objectName}" : "Take";

    public override bool CanAcceptKitchenObject(KitchenObject incoming) => false;
}
