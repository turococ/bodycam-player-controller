using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMov : MonoBehaviour
{
    [SerializeField] float movementSpeed = 4f;
    [SerializeField] float mass = 1f;
    [SerializeField] float aceleration = 10f;

    bool wasGrounded;

    public bool IsGrounded => controller.isGrounded;
    public Vector2 MoveInput => moveAction.ReadValue<Vector2>();
    public float VerticalVelocity => velocity.y;

    public event Action OnBeforeMove;
    public event Action<bool> OnGroundStateChange;

    internal Vector3 velocity;
    internal float movementSpeedMultiplier;

    CharacterController controller;
    PlayerInput playerInput;

    InputAction moveAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["move"];
    }

    void Update()
    {
        UpdateGround();
        UpdateGravity();
        UpdateMovement();
    }

    void UpdateGround()
    {
        if (wasGrounded != IsGrounded)
        {
            OnGroundStateChange?.Invoke(IsGrounded);
            wasGrounded = IsGrounded;
        }
    }

    void UpdateGravity()
    {
        var gravity = Physics.gravity * mass * Time.deltaTime;
        velocity.y = controller.isGrounded ? -1f : velocity.y + gravity.y;
    }

    Vector3 GetMovementInput()
    {
        var moveInput = MoveInput;

        var input = new Vector3();

        input += transform.forward * moveInput.y;
        input += transform.right * moveInput.x;
        input = Vector3.ClampMagnitude(input, 1f);
        input *= movementSpeed * movementSpeedMultiplier;

        return input;
    }

    void UpdateMovement()
    {
        movementSpeedMultiplier = 1f;
        OnBeforeMove?.Invoke();

        var input = GetMovementInput();

        var factor = aceleration * Time.deltaTime;
        velocity.x = Mathf.Lerp(velocity.x, input.x, factor);
        velocity.z = Mathf.Lerp(velocity.z, input.z, factor);

        controller.Move(velocity * Time.deltaTime);
    }
}
