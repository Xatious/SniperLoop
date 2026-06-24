using UnityEngine;

// Marker component — put this on any object the player can pick up.
// It tells the InteractionSystem "this object is interactable" and
// stores per-object settings like the display name and physics tuning.
[RequireComponent(typeof(Rigidbody))]
public class Interactable : MonoBehaviour
{
    // The name shown on screen when the player looks at this object.
    [SerializeField] private string itemName = "Item";

    // How far in front of the camera this object floats when held.
    // A small tool might be closer (1.0), a long rifle further (1.5).
    [SerializeField] private float holdDistance = 1.5f;

    // Per-object physics tuning. These override the defaults in InteractionSystem.
    // Heavier or bulkier objects can have lower spring force to feel weightier.
    [Header("Physics Overrides (0 = use system defaults)")]
    [SerializeField] private float springForce = 0f;
    [SerializeField] private float damping = 0f;

    public string ItemName => itemName;
    public float HoldDistance => holdDistance;
    public float SpringForce => springForce;
    public float Damping => damping;
}
