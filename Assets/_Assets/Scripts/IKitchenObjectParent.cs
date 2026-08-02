using UnityEngine;

public interface IKitchenObjectParent
{
    public Transform GetKitchenObjectFollowTransform();
    public void SetKitchenObject(KitchenObject kitchenObject);
    public KitchenObject GetKitchenObject();
    public void ClearKitchenObject();
    public bool HasKitchenObject();

    // Whether the player may place a held object here (a fridge says no;
    // an occupied slot is checked separately via HasKitchenObject).
    public bool CanAcceptKitchenObject();
}
