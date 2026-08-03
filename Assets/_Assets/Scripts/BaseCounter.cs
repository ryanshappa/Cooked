using UnityEngine;

/// Base for every kitchen station. A counter is a single-slot kitchen object
/// holder (IKitchenObjectParent) and can react to the player's Use action
/// (IInteractable) by overriding Interact.
public abstract class BaseCounter : MonoBehaviour, IKitchenObjectParent, IInteractable
{
    [SerializeField] private Transform counterTopPoint;

    private KitchenObject kitchenObject;

    // ----- IInteractable -----
    public virtual void Interact(Transform interactor) { }
    public virtual string GetInteractText() => "";
    public Transform GetTransform() => transform;

    // ----- IKitchenObjectParent -----
    public Transform GetKitchenObjectFollowTransform() => counterTopPoint;
    public void SetKitchenObject(KitchenObject obj) => kitchenObject = obj;
    public KitchenObject GetKitchenObject() => kitchenObject;
    public void ClearKitchenObject() => kitchenObject = null;
    public bool HasKitchenObject() => kitchenObject != null;
    public virtual bool CanAcceptKitchenObject(KitchenObject incoming) => counterTopPoint != null;
}
