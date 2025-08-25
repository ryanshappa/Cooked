using UnityEngine;

public class PlayerCarry : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private Transform holdPoint; // make this under CameraHolder

    private KitchenObject held;

    public Transform GetKitchenObjectFollowTransform() => holdPoint;
    public void SetKitchenObject(KitchenObject obj) => held = obj;
    public KitchenObject GetKitchenObject() => held;
    public void ClearKitchenObject() => held = null;
    public bool HasKitchenObject() => held != null;
}
