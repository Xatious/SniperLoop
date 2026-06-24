using UnityEngine;

// Drop this component on any GameObject with a mesh to give it full physics.
// It auto-adds a Rigidbody and configures a convex MeshCollider from the
// object's mesh. One component = ready for gravity, collisions, and interaction.
//
// If you also want the player to pick it up, add the Interactable component too.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public class PhysicsObject : MonoBehaviour
{
    [Header("Physics Defaults")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float drag = 0.1f;
    [SerializeField] private float angularDrag = 0.5f;

    // Reset() is called by Unity when the component is first added in the
    // editor (or when you right-click > Reset). This is where we auto-configure
    // the Rigidbody and MeshCollider with good defaults — saves manual setup.
    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        var meshCol = GetComponent<MeshCollider>();
        meshCol.convex = true;

        // If there's a MeshFilter, the MeshCollider automatically uses its mesh.
        // If not (e.g. the mesh is on a child), try to find it and assign it.
        if (meshCol.sharedMesh == null)
        {
            var mf = GetComponentInChildren<MeshFilter>();
            if (mf != null)
                meshCol.sharedMesh = mf.sharedMesh;
        }
    }

    // Also configure on Awake in case Reset wasn't triggered (runtime instantiation)
    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb.mass == 1f && mass != 1f)
        {
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
        }

        var meshCol = GetComponent<MeshCollider>();
        meshCol.convex = true;

        if (meshCol.sharedMesh == null)
        {
            var mf = GetComponentInChildren<MeshFilter>();
            if (mf != null)
                meshCol.sharedMesh = mf.sharedMesh;
        }
    }
}
