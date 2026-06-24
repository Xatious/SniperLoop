using UnityEngine;

// MountPointIn is a "receiver socket" — place it on environment surfaces
// (tables, ledges, shooting positions) to define where weapons can be mounted.
// Drop this as a child of any surface GameObject, position and rotate it to
// control where and how the weapon sits when snapped.
//
// The transform's forward direction = where the rifle will point.
// The transform's position = where the weapon's MountPointOut will align to.
public class MountPointIn : MonoBehaviour
{
    [SerializeField] private float snapRadius = 1.0f;

    // Swivel constraints — how far the weapon can rotate from this mount's
    // forward direction. Each mount point can have different values, so
    // one position might give a wide 90-degree sweep while another is a
    // narrow 30-degree window. Adds tactical choice to positioning.
    [Header("Swivel Constraints")]
    [SerializeField] private float horizontalArc = 45f;
    [SerializeField] private float verticalArc = 20f;

    // Runtime state — managed by the snap system, not set in Inspector.
    private GameObject occupant;

    public float SnapRadius => snapRadius;
    public float HorizontalArc => horizontalArc;
    public float VerticalArc => verticalArc;
    public bool IsOccupied => occupant != null;
    public GameObject Occupant => occupant;

    public void SetOccupant(GameObject obj)
    {
        occupant = obj;
    }

    public void ClearOccupant()
    {
        occupant = null;
    }

    void Start()
    {
        // Hide the visual mesh at runtime — it's only for editor positioning.
        // The MeshRenderer is on this same GameObject (not a child).
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    // Gizmos are editor-only debug visuals — they show up in the Scene view
    // but NOT in the Game view or builds. Perfect for visualizing invisible
    // things like snap zones, trigger areas, spawn points, etc.
    // OnDrawGizmos runs every frame in the editor; OnDrawGizmosSelected
    // only runs when the object is selected.
    void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, snapRadius);

        // Forward direction — center of the arc, where the rifle defaults to pointing
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);

        // Horizontal arc edges — the left/right limits of the swivel
        Gizmos.color = Color.cyan;
        Vector3 leftEdge = Quaternion.Euler(0, -horizontalArc, 0) * transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, horizontalArc, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftEdge * 0.8f);
        Gizmos.DrawRay(transform.position, rightEdge * 0.8f);

        // Vertical arc edges — the up/down limits
        Gizmos.color = Color.yellow;
        Vector3 upEdge = Quaternion.AngleAxis(-verticalArc, transform.right) * transform.forward;
        Vector3 downEdge = Quaternion.AngleAxis(verticalArc, transform.right) * transform.forward;
        Gizmos.DrawRay(transform.position, upEdge * 0.6f);
        Gizmos.DrawRay(transform.position, downEdge * 0.6f);
    }
}
