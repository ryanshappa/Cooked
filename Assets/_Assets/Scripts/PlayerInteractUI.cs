using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private GameObject container;          // parent GameObject of the UI
    [SerializeField] private TextMeshProUGUI label;         // the text component

    void Update()
    {
        var i = playerInteract.GetInteractableObject();
        if (i != null)
        {
            container.SetActive(true);
            label.text = i.GetInteractText();
        }
        else
        {
            container.SetActive(false);
        }
    }
}
