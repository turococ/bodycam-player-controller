using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] internal float sensetivity = 3f;

    [SerializeField] float CameraYMax = 89f;
    [SerializeField] float CameraYMin = -89f;

    [SerializeField] Transform cameraTransform;

    Vector2 look;

    PlayerInput playerInput;
    InputAction lookAction;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Look"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        UpdateCameraLook();
    }

    void UpdateCameraLook()
    {
        var lookInput = lookAction.ReadValue<Vector2>();

        look.y += lookInput.y * sensetivity;
        look.x += lookInput.x * sensetivity;

        look.y = Mathf.Clamp(look.y, CameraYMin, CameraYMax);

        cameraTransform.localRotation = Quaternion.Euler(-look.y, 0, 0);
        transform.localRotation = Quaternion.Euler(0, look.x, 0);
    }
}
