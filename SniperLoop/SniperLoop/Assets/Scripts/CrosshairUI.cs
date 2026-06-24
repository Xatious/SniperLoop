using UnityEngine;
using UnityEngine.UI;

// Manages the crosshair dot in the center of the screen.
// The dot changes color when looking at an interactable object.
// The actual action menu is now on each Interactable's world-space canvas.
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private Image crosshairDot;

    [Header("Colors")]
    [SerializeField] private Color defaultDotColor = Color.white;
    [SerializeField] private Color interactableDotColor = Color.green;

    public void SetInteractableMode(bool looking)
    {
        if (crosshairDot != null)
            crosshairDot.color = looking ? interactableDotColor : defaultDotColor;
    }
}
