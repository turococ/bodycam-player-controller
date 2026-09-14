/*
 * Copyright (c) 2026 turococ
 * Licensed under the MIT License. See LICENSE file in the project root for full license information.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMov : MonoBehaviour
{
    [SerializeField] internal float movementSpeed = 1f;
    [SerializeField] internal float runningSpeed = 3f;
    [SerializeField] float mass = 1f;
    [SerializeField] float acceleration = 10f;

    [SerializeField] Transform cameraTransform;

    internal float walkSpeedOrigin = 0f;

    bool wasGrounded;

    public bool IsGrounded => controller.isGrounded;
    public Vector2 MoveInput => moveAction.ReadValue<Vector2>();

    public event Action OnBeforeMove;
    public event Action<bool> OnGroundStateChange;

    internal Vector3 velocity;
    internal float movementSpeedMultiplier;

    CharacterController controller;
    PlayerInput playerInput;

    InputAction moveAction;

    void Awake()
    {
        walkSpeedOrigin = movementSpeed;
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["move"];
        
        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
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

        if (cameraTransform != null)
        {
            var cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward = cameraForward.normalized;

            var cameraRight = new Vector3(cameraForward.z, 0f, -cameraForward.x);

            input += cameraForward * moveInput.y;
            input += cameraRight * moveInput.x;
        }
        else
        {
            input += transform.forward * moveInput.y;
            input += transform.right * moveInput.x;
        }

        input = Vector3.ClampMagnitude(input, 1f);
        input *= movementSpeed * movementSpeedMultiplier;

        return input;
    }

    void UpdateMovement()
    {
        movementSpeedMultiplier = 1f;
        OnBeforeMove?.Invoke();

        var input = GetMovementInput();

        var factor = acceleration * Time.deltaTime;
        velocity.x = Mathf.Lerp(velocity.x, input.x, factor);
        velocity.z = Mathf.Lerp(velocity.z, input.z, factor);


        controller.Move(velocity * Time.deltaTime);
    }
}
