using UnityEngine;

// This script goes on the player (or the camera — either works).
// It spawns bullet prefabs at the muzzle point when you click.
public class PlayerShooting : MonoBehaviour
{
    // The bullet prefab — drag it into this slot in the Inspector.
    // This is a REFERENCE to the saved prefab asset, not a scene object.
    [SerializeField] private GameObject bulletPrefab;

    // The muzzle point — an empty GameObject positioned at the gun barrel tip.
    // This tells the script WHERE to spawn bullets.
    // An empty GameObject is just a transform (position/rotation) with no visuals.
    // Very common in 3D to use empties as "markers" or "spawn points."
    [SerializeField] private Transform muzzlePoint;

    // How fast the bullet flies. In SniperLoop this will eventually be
    // much higher for sniper-rifle speeds, but 40 is visible for testing.
    [SerializeField] private float bulletSpeed = 40f;

    void Update()
    {
        // Input.GetButtonDown("Fire1") triggers on the frame you press left mouse
        // button (or Ctrl). "Fire1" is a built-in input mapping — same system as
        // "Horizontal"/"Vertical" that we used in movement.
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Instantiate creates a copy of the bullet prefab.
        // Arguments: (what to copy, where to place it, what rotation to give it)
        //
        // muzzlePoint.position = spawn at the gun barrel tip
        // muzzlePoint.rotation = face the same direction the barrel points
        //
        // We cast the result to GameObject because Instantiate returns Object.
        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Get the Rigidbody from the spawned bullet so we can launch it.
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        // Set the bullet's velocity to fly forward.
        //
        // muzzlePoint.forward = a Vector3 pointing in the direction the muzzle
        // is facing. This is the 3D equivalent of transform.right in a 2D
        // side-scroller — it's the "natural" direction of the object.
        //
        // Multiplying a direction vector by a speed gives you a velocity vector.
        // Setting rb.velocity directly (instead of AddForce) gives instant,
        // predictable speed — the bullet leaves the barrel at exactly bulletSpeed.
        rb.linearVelocity = muzzlePoint.forward * bulletSpeed;
    }
}
