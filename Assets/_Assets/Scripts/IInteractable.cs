using UnityEngine;

public interface IInteractable
{
    // Called when the player presses the interact button
    void Interact(Transform interactorTransform);

    // Text for the prompt, e.g. "Use Counter"
    string GetInteractText();

    // Where to anchor highlights/prompts (we’ll keep it simple for now)
    Transform GetTransform();
}
