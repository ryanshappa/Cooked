using UnityEngine;

public interface IKitchenObjectParent
{
    public Transform GetKitchenObjectFollowTransform();
    public void SetKitchenObject(KitchenObject kitchenObject);
    public KitchenObject GetKitchenObject();
    public void ClearKitchenObject();
    public bool HasKitchenObject();

    // Whether the player may place this specific object here (a fridge says
    // no to everything; a cutting board only accepts items it has a recipe
    // for; a plate refuses other plates). Occupancy is checked separately
    // via HasKitchenObject.
    public bool CanAcceptKitchenObject(KitchenObject incoming);
}
