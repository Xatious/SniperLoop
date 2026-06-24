using UnityEngine;

// MountPointOut is a "plug" — place it as a child of any weapon to mark
// where it connects to a MountPointIn. Position it at the weapon's
// attachment point (e.g. bipod, underside of barrel).
//
// When the player drops a held weapon near a MountPointIn, this Out point
// aligns to the In point and the weapon locks into place.
public class MountPointOut : MonoBehaviour
{
    [SerializeField] private Material ghostPreviewMaterial;

    private MountPointIn currentMountPoint;
    private GameObject ghostPreview;
    private bool isSnapped;

    void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    public bool IsSnapped => isSnapped;
    public MountPointIn CurrentMountPoint => currentMountPoint;

    // Search all MountPointIn objects in the scene for the closest unoccupied
    // one within range. FindObjectsByType is the Unity 6 replacement for
    // the deprecated FindObjectsOfType — it's faster and lets you specify
    // whether you need sorted results.
    public MountPointIn FindNearestMountIn(float range)
    {
        var allMountIns = FindObjectsByType<MountPointIn>(FindObjectsSortMode.None);
        MountPointIn nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var mountIn in allMountIns)
        {
            if (mountIn.IsOccupied) continue;

            float dist = Vector3.Distance(transform.position, mountIn.transform.position);
            if (dist <= mountIn.SnapRadius && dist < nearestDist)
            {
                nearest = mountIn;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    // Snap the weapon to a MountPointIn. Aligns this Out point to the In point,
    // then makes the Rigidbody kinematic so it stays put.
    public void Snap(MountPointIn target)
    {
        currentMountPoint = target;
        isSnapped = true;
        target.SetOccupant(transform.root.gameObject);

        // Align the weapon so that this MountPointOut sits exactly at the
        // MountPointIn's position and rotation.
        //
        // Step 1: Set the weapon's rotation to match the mount point.
        // Step 2: Calculate where the MountPointOut ends up in world space
        //         after that rotation, then shift the weapon so it lands
        //         exactly on the MountPointIn position.
        Transform weapon = transform.root;
        weapon.rotation = target.transform.rotation;

        // After rotating, the MountPointOut is at some world position.
        // The difference between that and the target is how far we need to shift.
        Vector3 outWorldPos = transform.position;
        Vector3 offset = target.transform.position - outWorldPos;
        weapon.position += offset;

        var rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        HideGhostPreview();
    }

    // Release the weapon from its mount point, restoring physics.
    public void Unsnap()
    {
        if (currentMountPoint != null)
            currentMountPoint.ClearOccupant();

        currentMountPoint = null;
        isSnapped = false;

        var rb = transform.root.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;
    }

    // Show a transparent copy of the weapon at the snap position so the
    // player can see where it will land before dropping.
    public void ShowGhostPreview(MountPointIn target)
    {
        if (ghostPreviewMaterial == null) return;

        if (ghostPreview == null)
            CreateGhostPreview();

        if (ghostPreview == null) return;

        ghostPreview.SetActive(true);

        // Position the ghost the same way Snap() would position the real weapon.
        // The MountPointOut's local position relative to the weapon root tells us
        // the offset. We apply the target rotation, then shift so the Out point
        // lands on the In point.
        ghostPreview.transform.rotation = target.transform.rotation;

        // Calculate where the Out point would be on the ghost, then shift
        Vector3 scaledLocalPos = Vector3.Scale(transform.localPosition, transform.root.localScale);
        Vector3 rotatedOffset = target.transform.rotation * scaledLocalPos;
        ghostPreview.transform.position = target.transform.position - rotatedOffset;
    }

    public void HideGhostPreview()
    {
        if (ghostPreview != null)
            ghostPreview.SetActive(false);
    }

    // Build a ghost preview by cloning the weapon's mesh with the transparent material.
    // This runs once and caches the result.
    void CreateGhostPreview()
    {
        var sourceMeshFilter = transform.root.GetComponent<MeshFilter>();
        var sourceMeshRenderer = transform.root.GetComponent<MeshRenderer>();
        if (sourceMeshFilter == null || sourceMeshRenderer == null) return;

        ghostPreview = new GameObject("GhostPreview");
        // Don't save this in the scene — it's a runtime-only object.
        ghostPreview.hideFlags = HideFlags.HideAndDontSave;

        var mf = ghostPreview.AddComponent<MeshFilter>();
        mf.mesh = sourceMeshFilter.sharedMesh;

        var mr = ghostPreview.AddComponent<MeshRenderer>();
        mr.material = ghostPreviewMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Match the weapon's scale
        ghostPreview.transform.localScale = transform.root.localScale;
        ghostPreview.SetActive(false);
    }

    void OnDestroy()
    {
        if (ghostPreview != null)
            Destroy(ghostPreview);
    }

    // Small gizmo so you can see where the attachment point is on the weapon
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawRay(transform.position, -transform.up * 0.1f);
    }
}
