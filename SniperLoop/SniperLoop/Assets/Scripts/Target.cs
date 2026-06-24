using UnityEngine;

// Attach this to any GameObject you want to be a "target."
// When a bullet hits it, it changes color to confirm the hit.
//
// CONCEPT: Tags
// Unity lets you assign "tags" to GameObjects — string labels like "Player",
// "Enemy", "Bullet". You can check these in collision callbacks to know
// WHAT hit you. We'll tag our bullet prefab as "Bullet" so the target
// can distinguish bullet hits from other collisions (like hitting the ground).
public class Target : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    // Store the original color so we can flash back to it.
    private Color originalColor;

    void Start()
    {
        // MeshRenderer is the component that actually draws a 3D object.
        // In 2D you had SpriteRenderer — MeshRenderer is the 3D equivalent.
        // It holds a reference to a Material, which controls the object's appearance.
        meshRenderer = GetComponent<MeshRenderer>();
        originalColor = meshRenderer.material.color;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the thing that hit us is tagged "Bullet."
        // collision.gameObject = the OTHER object involved in the collision.
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Change color to red to show we got hit.
            // .material.color accesses the main color of the object's material.
            // In URP this maps to the "_BaseColor" property of the shader.
            meshRenderer.material.color = Color.red;

            // Reset color after 0.5 seconds using Invoke.
            // Invoke calls a method by name after a delay — simple timer.
            // For more complex timing you'd use Coroutines, but Invoke
            // works fine for one-shot delayed calls.
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.5f);
        }
    }

    void ResetColor()
    {
        meshRenderer.material.color = originalColor;
    }
}
