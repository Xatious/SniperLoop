using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Two-handed interaction system. Left click = left hand, right click = right hand.
// Each hand independently holds objects with physics-based spring forces.
// Both hands on the same object = two-handed grip (stable, rotatable).
// Context menu (scroll wheel) shows available actions on looked-at objects.
public class InteractionSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference leftClickAction;
    [SerializeField] private InputActionReference rightClickAction;
    [SerializeField] private InputActionReference scrollAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference freezeAction;

    [Header("Raycast")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Physics Hold")]
    [SerializeField] private float defaultSpringForce = 50f;
    [SerializeField] private float defaultDamping = 10f;
    [SerializeField] private float defaultHoldDistance = 1.5f;
    [SerializeField] private float maxHoldSpeed = 10f;
    // Horizontal offset when carrying two separate objects so they don't overlap
    [SerializeField] private float twoItemSpread = 0.3f;

    [Header("Grip")]
    [SerializeField] private float gripStrength = 5f;

    [Header("Rotation (Two-Handed)")]
    [SerializeField] private float rotationSensitivity = 1f;

    [Header("UI")]
    [SerializeField] private CrosshairUI crosshairUI;

    // Per-hand state — each hand tracks its own held object independently
    private class HandState
    {
        public Rigidbody heldRB;
        public Interactable heldInteractable;
        public MountPointOut heldMountOut;
        public Vector3 grabLocalPoint;
        public bool wasGravity;

        public bool IsHolding => heldRB != null;
    }

    private HandState leftHand = new HandState();
    private HandState rightHand = new HandState();

    private Camera playerCamera;
    private Interactable lookedAtInteractable;
    private List<InteractionAction> currentActions = new List<InteractionAction>();

    // True when both hands hold the same object — camera freezes for rotation.
    public bool IsTwoHanding { get; private set; }
    // True when R is held with a one-handed object — freezes object in space
    // so the player can look at a specific spot for their second hand.
    public bool IsFreezing { get; private set; }
    public bool IsHoldingAnything => leftHand.IsHolding || rightHand.IsHolding;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        leftClickAction?.action.Enable();
        rightClickAction?.action.Enable();
        scrollAction?.action.Enable();
        lookAction?.action.Enable();
        freezeAction?.action.Enable();
    }

    void OnDisable()
    {
        leftClickAction?.action.Disable();
        rightClickAction?.action.Disable();
        scrollAction?.action.Disable();
        lookAction?.action.Disable();
        freezeAction?.action.Disable();

        if (leftHand.IsHolding) DropHand(leftHand);
        if (rightHand.IsHolding) DropHand(rightHand);
    }

    void Update()
    {
        // --- RAYCAST DETECTION ---
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit = default;
        bool didHit = Physics.Raycast(ray, out hit, interactRange, interactableLayers);

        if (didHit)
        {
            var interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable != lookedAtInteractable)
            {
                // Hide the previous object's menu
                lookedAtInteractable?.HideMenu();

                lookedAtInteractable = interactable;
                currentActions = interactable.GetActions(this);
                interactable.ShowMenu(currentActions);
                crosshairUI?.SetInteractableMode(true);
            }
        }
        else if (lookedAtInteractable != null && !IsTwoHanding)
        {
            ClearLookedAt();
        }

        // --- SCROLL WHEEL ---
        Vector2 scroll = scrollAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        if (scroll.y > 0.1f)
            lookedAtInteractable?.ScrollMenu(-1);
        else if (scroll.y < -0.1f)
            lookedAtInteractable?.ScrollMenu(1);

        // --- SNAP POINT PREVIEW (for whichever hand is holding a snappable object) ---
        UpdateSnapPreview(leftHand);
        UpdateSnapPreview(rightHand);

        // --- LEFT CLICK ---
        bool leftPressed = leftClickAction?.action.WasPressedThisFrame() ?? false;
        bool leftHeld = leftClickAction?.action.IsPressed() ?? false;

        if (leftPressed && !leftHand.IsHolding)
        {
            ExecuteSelectedAction(HandSide.Left, hit);
        }
        else if (leftHand.IsHolding && !leftHeld)
        {
            DropFromHand(leftHand);
        }

        // --- RIGHT CLICK ---
        bool rightPressed = rightClickAction?.action.WasPressedThisFrame() ?? false;
        bool rightHeld = rightClickAction?.action.IsPressed() ?? false;

        if (rightPressed && !rightHand.IsHolding)
        {
            ExecuteSelectedAction(HandSide.Right, hit);
        }
        else if (rightHand.IsHolding && !rightHeld)
        {
            DropFromHand(rightHand);
        }

        // --- FREEZE MODE (R key) ---
        // Hold R while one-handing an object to freeze it in space.
        // This lets you look around freely to find a spot for your second hand.
        // The object goes kinematic (no physics) and stays put while you aim.
        bool freezeHeld = freezeAction?.action.IsPressed() ?? false;
        bool onlyOneHand = (leftHand.IsHolding ^ rightHand.IsHolding);

        if (freezeHeld && onlyOneHand && !IsTwoHanding)
        {
            var activeHand = leftHand.IsHolding ? leftHand : rightHand;
            if (!IsFreezing)
            {
                activeHand.heldRB.isKinematic = true;
                activeHand.heldRB.linearVelocity = Vector3.zero;
                activeHand.heldRB.angularVelocity = Vector3.zero;
                IsFreezing = true;
            }
        }
        else if (IsFreezing)
        {
            // Unfreeze — restore physics on whichever hand was holding
            var activeHand = leftHand.IsHolding ? leftHand : rightHand;
            if (activeHand.IsHolding && activeHand.heldRB.isKinematic)
            {
                activeHand.heldRB.isKinematic = false;
                activeHand.heldRB.linearVelocity = Vector3.zero;
                activeHand.heldRB.angularVelocity = Vector3.zero;
            }
            IsFreezing = false;
        }

        // --- TWO-HANDED DETECTION ---
        bool wasTwoHanding = IsTwoHanding;
        IsTwoHanding = leftHand.IsHolding && rightHand.IsHolding
                     && leftHand.heldRB == rightHand.heldRB;

        // --- TWO-HANDED ROTATION ---
        if (IsTwoHanding)
        {
            Vector2 mouseDelta = lookAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
            if (mouseDelta.sqrMagnitude > 0.01f)
            {
                float rotX = -mouseDelta.y * rotationSensitivity;
                float rotY = mouseDelta.x * rotationSensitivity;

                Quaternion yaw = Quaternion.AngleAxis(rotY, Vector3.up);
                Quaternion pitch = Quaternion.AngleAxis(rotX, playerCamera.transform.right);
                leftHand.heldRB.MoveRotation(pitch * yaw * leftHand.heldRB.rotation);
            }
        }
    }

    void FixedUpdate()
    {
        // Skip spring forces while frozen — object is kinematic, stays in place
        if (IsFreezing) return;

        if (IsTwoHanding)
        {
            // Two-handed: both springs pull the same object from different points.
            // This naturally stabilizes it — no droop, no tilt.
            ApplyHoldForces(leftHand, Vector3.zero);
            ApplyHoldForces(rightHand, Vector3.zero);
        }
        else
        {
            // Offset left/right when carrying two separate objects
            Vector3 leftOffset = rightHand.IsHolding ? -playerCamera.transform.right * twoItemSpread : Vector3.zero;
            Vector3 rightOffset = leftHand.IsHolding ? playerCamera.transform.right * twoItemSpread : Vector3.zero;

            if (leftHand.IsHolding)
                ApplyHoldForces(leftHand, leftOffset);
            if (rightHand.IsHolding)
                ApplyHoldForces(rightHand, rightOffset);
        }
    }

    void ApplyHoldForces(HandState hand, Vector3 positionOffset)
    {
        if (!hand.IsHolding) return;

        float holdDist = (hand.heldInteractable != null && hand.heldInteractable.HoldDistance > 0)
            ? hand.heldInteractable.HoldDistance
            : defaultHoldDistance;

        Vector3 targetPos = playerCamera.transform.position
                          + playerCamera.transform.forward * holdDist
                          + positionOffset;

        Vector3 currentGrabPos = hand.heldRB.transform.TransformPoint(hand.grabLocalPoint);
        Vector3 displacement = targetPos - currentGrabPos;

        if (displacement.magnitude > interactRange * 2f)
        {
            DropHand(hand);
            return;
        }

        float k = (hand.heldInteractable != null && hand.heldInteractable.SpringForce > 0)
            ? hand.heldInteractable.SpringForce
            : defaultSpringForce;
        if (IsTwoHanding) k *= 1.5f;
        Vector3 springForce = displacement * k;

        float c = (hand.heldInteractable != null && hand.heldInteractable.Damping > 0)
            ? hand.heldInteractable.Damping
            : defaultDamping;
        Vector3 dampingForce = -hand.heldRB.linearVelocity * c;

        Vector3 grabWorldPoint = hand.heldRB.transform.TransformPoint(hand.grabLocalPoint);
        hand.heldRB.AddForceAtPosition(springForce + dampingForce, grabWorldPoint, ForceMode.Force);

        if (hand.heldRB.linearVelocity.magnitude > maxHoldSpeed)
            hand.heldRB.linearVelocity = hand.heldRB.linearVelocity.normalized * maxHoldSpeed;

        // Grip torque — fights gravity tilt. Stronger when two-handing.
        if (gripStrength > 0f && !IsTwoHanding)
        {
            Vector3 currentUp = hand.heldRB.transform.up;
            Vector3 torqueAxis = Vector3.Cross(currentUp, Vector3.up);
            hand.heldRB.AddTorque(torqueAxis * gripStrength, ForceMode.Acceleration);
        }

        if (!IsTwoHanding)
            hand.heldRB.angularVelocity *= 0.85f;
    }

    // Called by Interactable.GetActions — this is the public pickup entry point.
    public void PickupWithHand(HandSide side, Interactable target)
    {
        // Can't pick up if that hand is already holding something
        var hand = side == HandSide.Left ? leftHand : rightHand;
        if (hand.IsHolding) return;

        var rb = target.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Unsnap if mounted
        var mountOut = target.GetComponentInChildren<MountPointOut>();
        if (mountOut != null && mountOut.IsSnapped)
            mountOut.Unsnap();

        // If kinematic (was snapped), restore to dynamic
        if (rb.isKinematic)
            rb.isKinematic = false;

        hand.heldRB = rb;
        hand.heldInteractable = target;
        hand.heldMountOut = mountOut;
        hand.wasGravity = rb.useGravity;

        // Find the grab point from a raycast at the object
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        var cols = rb.GetComponentsInChildren<Collider>();
        bool foundGrab = false;
        foreach (var col in cols)
        {
            if (col.Raycast(ray, out RaycastHit grabHit, interactRange * 2f))
            {
                hand.grabLocalPoint = rb.transform.InverseTransformPoint(grabHit.point);
                foundGrab = true;
                break;
            }
        }
        if (!foundGrab)
            hand.grabLocalPoint = Vector3.zero;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.angularVelocity = Vector3.zero;

        ClearLookedAt();
    }

    void DropFromHand(HandState hand)
    {
        if (!hand.IsHolding) return;

        // Check for snap point before normal drop
        if (hand.heldMountOut != null)
        {
            var nearestMount = hand.heldMountOut.FindNearestMountIn(interactRange);
            if (nearestMount != null)
            {
                hand.heldMountOut.HideGhostPreview();
                hand.heldMountOut.Snap(nearestMount);
                ClearHandState(hand);
                return;
            }
        }

        DropHand(hand);
    }

    void DropHand(HandState hand)
    {
        if (!hand.IsHolding) return;

        hand.heldMountOut?.HideGhostPreview();

        if (hand.heldRB.isKinematic)
            hand.heldRB.isKinematic = false;

        hand.heldRB.useGravity = hand.wasGravity;
        hand.heldRB.interpolation = RigidbodyInterpolation.None;
        hand.heldRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

        ClearHandState(hand);
    }

    void ClearHandState(HandState hand)
    {
        hand.heldRB = null;
        hand.heldInteractable = null;
        hand.heldMountOut = null;
        hand.grabLocalPoint = Vector3.zero;
    }

    void ExecuteSelectedAction(HandSide side, RaycastHit hit)
    {
        if (lookedAtInteractable == null) return;
        int index = lookedAtInteractable.GetSelectedIndex();
        if (index >= 0 && index < currentActions.Count)
        {
            currentActions[index].Execute(side);
        }
    }

    void UpdateSnapPreview(HandState hand)
    {
        if (!hand.IsHolding || hand.heldMountOut == null) return;
        var nearestMount = hand.heldMountOut.FindNearestMountIn(interactRange);
        if (nearestMount != null)
            hand.heldMountOut.ShowGhostPreview(nearestMount);
        else
            hand.heldMountOut.HideGhostPreview();
    }

    void ClearLookedAt()
    {
        lookedAtInteractable?.HideMenu();
        lookedAtInteractable = null;
        currentActions.Clear();
        crosshairUI?.SetInteractableMode(false);
    }
}
