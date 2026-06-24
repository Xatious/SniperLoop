using UnityEngine;
using UnityEngine.InputSystem;

// NEW INPUT SYSTEM vs OLD:
// The old system used Input.GetAxis("Horizontal") — simple but inflexible.
// The new Input System uses "InputAction" objects that map to physical controls.
// You define bindings in a .inputactions asset (like your InputSystem_Actions),
// then reference those actions in code. The big advantage: you can remap controls
// at runtime, support multiple devices (keyboard, gamepad, etc.) from one action.
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 0.4f;

    [Header("Crouch")]
    // Crouch works by shrinking the CharacterController's capsule height
    // and lowering the camera. The controller height changes so you can
    // fit under low geometry, and the camera drops so it feels like ducking.
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float crouchCameraOffset = 0.9f;
    // How fast the crouch transition happens (lerp speed).
    // Instant snapping feels robotic; smooth lerp feels natural.
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference crouchAction;

    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform;
    private float verticalVelocity;
    private bool isCrouching;

    // Store the original controller values so we can restore them when standing.
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Cache the standing values from whatever the CharacterController
        // is configured to in the Inspector — keeps this portable across
        // different player models with different heights.
        standingHeight = controller.height;
        standingCenter = controller.center;

        // Find the camera (child of this object) to move it during crouch.
        cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (cameraTransform != null)
            standingCameraY = cameraTransform.localPosition.y;
    }

    // OnEnable/OnDisable: the new Input System requires you to explicitly
    // enable and disable actions. If you forget, the action won't read input.
    // This is a safety feature — disabled actions use zero CPU.
    void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        crouchAction?.action.Enable();
    }

    void OnDisable()
    {
        moveAction?.action.Disable();
        lookAction?.action.Disable();
        jumpAction?.action.Disable();
        crouchAction?.action.Disable();
    }

    void Update()
    {
        // --- READ MOVEMENT INPUT ---
        // ReadValue<Vector2>() replaces the old Input.GetAxis() calls.
        // The "Move" action is configured as a Vector2 in your .inputactions file,
        // with WASD mapped as a 2D composite (up/down/left/right → y/x).
        Vector2 input = moveAction?.action.ReadValue<Vector2>() ?? Vector2.zero;

        // --- CROUCH TOGGLE ---
        // WasPressedThisFrame gives us one press event per tap.
        // Toggle: first press crouches, second press stands back up.
        bool crouchPressed = crouchAction?.action.WasPressedThisFrame() ?? false;
        if (crouchPressed)
        {
            // Don't allow standing if there's something above us.
            // SphereCast upward from the player's head to check for obstacles.
            if (isCrouching)
            {
                // Check if there's room to stand: cast from current position
                // upward by the difference between standing and crouching height.
                float headRoom = standingHeight - crouchHeight;
                bool blocked = Physics.SphereCast(
                    transform.position + Vector3.up * crouchHeight,
                    controller.radius,
                    Vector3.up,
                    out _,
                    headRoom,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );
                if (!blocked)
                    isCrouching = false;
            }
            else
            {
                isCrouching = true;
            }
        }

        // Smoothly transition the CharacterController height and camera position.
        // Lerp gives us a smooth blend rather than an instant snap.
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);

        if (cameraTransform != null)
        {
            float targetCamY = isCrouching ? crouchCameraOffset : standingCameraY;
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = camPos;
        }

        // Use slower speed when crouching
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        // --- GRAVITY & JUMP ---
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Jump when grounded and the jump button is pressed this frame.
        // WasPressedThisFrame() is the new Input System equivalent of
        // Input.GetButtonDown() — it returns true only on the frame
        // the button goes down, not while it's held.
        bool jumpPressed = jumpAction?.action.WasPressedThisFrame() ?? false;
        if (jumpPressed && controller.isGrounded)
        {
            // Physics formula: to reach a given height, initial velocity must be
            // v = sqrt(2 * g * h). The Mathf.Abs handles our negative gravity value.
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- DRIVE ANIMATION ---
        if (animator != null)
        {
            float speed = input.magnitude * currentSpeed;
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsGroundedHash, controller.isGrounded);

            // "Jump" is a trigger, not a bool. Triggers fire once and auto-reset,
            // like a one-shot event. Perfect for jump because you only want the
            // jump animation to start once per press, not loop while held.
            if (jumpPressed && controller.isGrounded)
                animator.SetTrigger(JumpHash);
        }
    }
}
