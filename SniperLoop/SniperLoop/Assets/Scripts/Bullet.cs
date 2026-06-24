using UnityEngine;

// This script goes on the bullet prefab.
// The bullet is a small Sphere with a Rigidbody and SphereCollider.
//
// KEY CONCEPT: Prefabs
// A prefab is a saved, reusable GameObject template. You create a GameObject
// in the scene, configure it (add components, set values), then drag it into
// the Assets folder to save it as a prefab. After that, you can Instantiate()
// copies of it from code — each copy is independent but starts with the same
// setup. You used this same pattern for pipe spawning in Flappy Bird.
//
// The bullet's Rigidbody has useGravity = true, so it drops over time.
// This is the foundation for SniperLoop's physics-based ballistics —
// no hitscan (instant-hit raycast), real projectile travel.
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    // Custom gravity multiplier for this bullet.
    // 1.0 = normal Earth gravity (-9.81 m/s²)
    // 0.5 = half gravity (bullet drops slower — more "floaty")
    // 2.0 = double gravity (bullet drops fast — more arcadey)
    // 0.0 = no gravity at all (bullet flies perfectly straight)
    //
    // This lets you tune bullet drop independently of the global physics
    // gravity, which is important because different weapons might have
    // different drop characteristics (e.g. a sniper round vs a pistol).
    [SerializeField] private float gravityMultiplier = 1f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Disable the Rigidbody's built-in gravity so we can apply our own.
        // If we left this on AND applied custom gravity, we'd get double gravity.
        rb.useGravity = false;

        Destroy(gameObject, lifetime);
    }

    // FixedUpdate runs on the PHYSICS clock, not the rendering clock.
    // Regular Update() runs once per frame (variable rate — tied to FPS).
    // FixedUpdate() runs at a fixed interval (default 50 times/second).
    // All physics-related code (forces, velocity changes) should go in
    // FixedUpdate so the physics simulation stays consistent regardless
    // of frame rate. This is the same concept as in 2D.
    void FixedUpdate()
    {
        // Apply custom gravity as a force each physics step.
        // Physics.gravity is Unity's global gravity vector (0, -9.81, 0).
        // We multiply it by our gravityMultiplier to scale the effect.
        //
        // ForceMode.Acceleration ignores the object's mass — gravity
        // accelerates all objects equally (thanks, Newton). If we used
        // ForceMode.Force instead, heavier bullets would fall slower,
        // which isn't how real gravity works.
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
