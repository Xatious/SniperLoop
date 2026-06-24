using UnityEngine;
using UnityEngine.InputSystem;

// Core interaction system for first-person object pickup, hold, rotate, and drop.
// Goes on the Main Camera. Uses physics-based holding (spring-damper forces)
// so held objects sway naturally rather than snapping rigidly to a point.
public class InteractionSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference lookAction;

    [Header("Raycast")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Physics Hold")]
    [SerializeField] private float defaultSpringForce = 50f;
    [SerializeField] private float defaultDamping = 10f;
    [SerializeField] private float defaultHoldDistance = 1.5f;
    [SerializeField] private float maxHoldSpeed = 10f;

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 1f;

    [Header("UI")]
    [SerializeField] private CrosshairUI crosshairUI;

    private Camera playerCamera;
    private Rigidbody heldRigidbody;
    private Interactable heldInteractable;
    private Interactable lookedAtInteractable;
    private bool wasGravity;

    // Other scripts (MouseLook) check this to know when to freeze camera rotation.
    public bool IsRotating { get; private set; }

    // Tracks whether we're holding something — useful for other systems
    // (e.g. PlayerShooting can check this to prevent firing while carrying).
    public bool IsHoldingObject => heldRigidbody != null;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        attackAction?.action.Enable();
        rotateAction?.action.Enable();
        lookAction?.action.Enable();
    }

    void OnDisable()
    {
        attackAction?.action.Disable();
        rotateAction?.action.Disable();
        lookAction?.action.Disable();

        if (heldRigidbody != null)
            Drop();
    }

    void Update()
    {
        // --- RAYCAST DETECTION ---
        // Cast a ray from the exact center of the camera (where the crosshair dot is)
        // forward into the scene. This is how we "look at" objects.
        // Physics.Raycast returns true if it hits something within range.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayers))
        {
            // GetComponentInParent walks up the hierarchy — useful when the collider
            // is on a child object but the Interactable component is on the parent.
            var interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null && heldRigidbody == null)
            {
                lookedAtInteractable = interactable;
                crosshairUI?.ShowItemName(interactable.ItemName);
            }
            else if (heldRigidbody == null)
            {
                ClearLookedAt();
            }
        }
        else if (heldRigidbody == null)
        {
            ClearLookedAt();
        }

        // --- PICKUP ---
        bool attackPressed = attackAction?.action.WasPressedThisFrame() ?? false;
        if (attackPressed && heldRigidbody == null && lookedAtInteractable != null)
        {
            Pickup(lookedAtInteractable);
        }

        // --- DROP ---
        bool attackHeld = attackAction?.action.IsPressed() ?? false;
        if (heldRigidbody != null && !attackHeld)
        {
            Drop();
        }

        // --- ROTATE MODE ---
        // When holding R with an object, mouse movement rotates the object
        // instead of the camera. MouseLook checks IsRotating to freeze.
        if (heldRigidbody != null && (rotateAction?.action.IsPressed() ?? false))
        {
            IsRotating = true;

            Vector2 mouseDelta = lookAction?.action.ReadValue<Vector2>() ?? Vector2.zero;

            // Mouse X rotates around the world Y axis (spin left/right).
            // Mouse Y rotates around the camera's right axis (tilt forward/back).
            // This feels intuitive: moving the mouse left spins the object left.
            float rotX = -mouseDelta.y * rotationSensitivity;
            float rotY = mouseDelta.x * rotationSensitivity;

            // MoveRotation is the physics-safe way to rotate a Rigidbody.
            // Direct transform.rotation changes can cause physics glitches.
            Quaternion deltaRotation = Quaternion.Euler(
                playerCamera.transform.right * rotX +
                Vector3.up * rotY
            );

            // Build rotation incrementally from current rotation
            Quaternion yaw = Quaternion.AngleAxis(rotY, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(rotX, playerCamera.transform.right);
            heldRigidbody.MoveRotation(pitch * yaw * heldRigidbody.rotation);
        }
        else
        {
            IsRotating = false;
        }
    }

    void FixedUpdate()
    {
        if (heldRigidbody == null) return;

        // --- SPRING-DAMPER PHYSICS ---
        // This is the core of the physics hold. Instead of teleporting the object
        // to a fixed point (which ignores collisions), we apply forces that PULL
        // it toward the target. This creates the natural sway/lag effect.

        float holdDist = (heldInteractable != null && heldInteractable.HoldDistance > 0)
            ? heldInteractable.HoldDistance
            : defaultHoldDistance;

        // Target position: a point floating in front of the camera
        Vector3 targetPos = playerCamera.transform.position
                          + playerCamera.transform.forward * holdDist;

        // Displacement: how far the object is from where it should be
        Vector3 displacement = targetPos - heldRigidbody.position;

        // Safety check: if the object got stuck behind geometry and is too far away,
        // force-drop it rather than yanking it through walls.
        if (displacement.magnitude > interactRange * 2f)
        {
            Drop();
            return;
        }

        // Spring force (Hooke's Law): F = k * x
        // The further from the target, the stronger the pull.
        float k = (heldInteractable != null && heldInteractable.SpringForce > 0)
            ? heldInteractable.SpringForce
            : defaultSpringForce;
        Vector3 springForce = displacement * k;

        // Damping force: F = -c * v
        // Opposes current velocity to prevent oscillation (bouncing back and forth).
        // Without this, the object would overshoot the target and swing like a pendulum.
        float c = (heldInteractable != null && heldInteractable.Damping > 0)
            ? heldInteractable.Damping
            : defaultDamping;
        Vector3 dampingForce = -heldRigidbody.linearVelocity * c;

        heldRigidbody.AddForce(springForce + dampingForce, ForceMode.Force);

        // Clamp velocity so the object can't shoot through walls when the player
        // whips the camera around. It'll lag behind instead — which looks natural.
        if (heldRigidbody.linearVelocity.magnitude > maxHoldSpeed)
            heldRigidbody.linearVelocity = heldRigidbody.linearVelocity.normalized * maxHoldSpeed;

        // Gradually reduce spinning — but only when NOT in rotate mode,
        // otherwise it fights the player's intentional rotation input.
        if (!IsRotating)
            heldRigidbody.angularVelocity *= 0.9f;
    }

    void Pickup(Interactable target)
    {
        var rb = target.GetComponent<Rigidbody>();
        if (rb == null) return;

        heldRigidbody = rb;
        heldInteractable = target;

        // Save and disable gravity so the object floats while held
        wasGravity = rb.useGravity;
        rb.useGravity = false;

        // Interpolation smooths the visual position between physics frames.
        // Without it, the held object jitters because FixedUpdate runs at a
        // different rate than the render loop.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Continuous collision detection prevents fast-moving objects from
        // tunneling through thin walls (checks collision along the path, not just at endpoints).
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Kill any existing spin so the object doesn't whirl when grabbed
        rb.angularVelocity = Vector3.zero;

        ClearLookedAt();
    }

    void Drop()
    {
        // Re-enable gravity — the object falls naturally from here.
        // It keeps whatever velocity it had, so if you were moving,
        // it gets tossed in that direction. Physics drop.
        heldRigidbody.useGravity = wasGravity;
        heldRigidbody.interpolation = RigidbodyInterpolation.None;
        heldRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

        heldRigidbody = null;
        heldInteractable = null;
        IsRotating = false;
    }

    void ClearLookedAt()
    {
        lookedAtInteractable = null;
        crosshairUI?.HideItemName();
    }
}
