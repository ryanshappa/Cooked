using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private GameObject container;          // parent GameObject of the UI
    [SerializeField] private TextMeshProUGUI label;         // the text component

    void Update()
    {
        if (playerInteract.HasPrompt(out string text))
        {
            container.SetActive(true);
            label.text = text;
        }
        else
        {
            container.SetActive(false);
        }
    }
}
