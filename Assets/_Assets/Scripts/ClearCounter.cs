using UnityEngine;

public class ClearCounter : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Use Counter";

    public void Interact(Transform interactorTransform)
    {
        Debug.Log($"INTERACT! {name} used by {interactorTransform.name}");
        // TODO: later — place/pick up logic here
    }

    public string GetInteractText() => prompt;

    public Transform GetTransform() => transform;
}
