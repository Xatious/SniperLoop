using UnityEngine;
using UnityEngine.InputSystem;

// This script goes on the CAMERA, not the player body.
// It handles both rotations:
//   - Left/right mouse → rotates the PLAYER BODY on Y axis (spin)
//   - Up/down mouse → rotates the CAMERA on X axis (tilt head)
//
// The camera is a CHILD of the player body. So when the body spins,
// the camera spins with it (because children inherit parent transforms).
// But when the camera tilts, only the camera moves — the body stays level.
public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.3f;

    [SerializeField] private Transform playerBody;

    // InputActionReference links to the "Look" action in your .inputactions asset.
    // The Look action is bound to <Pointer>/delta — that's mouse movement delta,
    // which gives you pixels moved per frame (not normalized -1 to 1 like the old system).
    // That's why sensitivity is lower here (0.3) — the raw delta values are much larger.
    [SerializeField] private InputActionReference lookAction;

    // When the InteractionSystem is in rotate mode (holding R with an object),
    // we freeze camera look so the mouse only rotates the held object.
    [SerializeField] private InteractionSystem interactionSystem;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnEnable()
    {
        lookAction?.action.Enable();
    }

    void OnDisable()
    {
        lookAction?.action.Disable();
    }

    void Update()
    {
        // Skip camera rotation when two-handing an object.
        // Mouse input goes to InteractionSystem for rotation instead.
        if (interactionSystem != null && interactionSystem.IsTwoHanding)
            return;

        Vector2 lookInput = lookAction?.action.ReadValue<Vector2>() ?? Vector2.zero;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Subtract mouseY: mouse up = positive Y, but looking up = negative X rotation.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Tilt camera only (local to the player body parent)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Spin the player body (camera follows because it's a child)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
