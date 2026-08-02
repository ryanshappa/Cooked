using UnityEngine;

/// One hinged fridge door: Use toggles it open/closed. To close an open door,
/// click the door itself again — its collider swings with it. The door mesh
/// origin must sit at the hinge edge (Pandazole doors are authored this way).
public class FridgeDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private float openAngle = 100f;   // positive = right door, negative = left door
    [SerializeField] private float swingSpeed = 6f;

    private bool isOpen;
    private float currentAngle;

    public bool IsOpen => isOpen;

    public void Interact(Transform interactor) => isOpen = !isOpen;
    public string GetInteractText() => isOpen ? "Close door" : "Open door";
    public Transform GetTransform() => transform;

    void Update()
    {
        float target = isOpen ? openAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, target, swingSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}
