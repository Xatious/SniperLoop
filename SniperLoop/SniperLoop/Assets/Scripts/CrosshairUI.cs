using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Manages the crosshair dot and item name display.
// This is purely visual — no gameplay logic lives here.
// The InteractionSystem calls ShowItemName/HideItemName to control it.
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private Image crosshairDot;
    [SerializeField] private TMP_Text itemNameText;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color interactableColor = Color.green;

    void Start()
    {
        HideItemName();
    }

    public void ShowItemName(string name)
    {
        itemNameText.text = name;
        itemNameText.enabled = true;
        crosshairDot.color = interactableColor;
    }

    public void HideItemName()
    {
        itemNameText.text = "";
        itemNameText.enabled = false;
        crosshairDot.color = defaultColor;
    }
}
