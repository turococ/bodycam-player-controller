using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] internal float sensitivity = 3f;

    [SerializeField] float CameraYMax = 89f;
    [SerializeField] float CameraYMin = -89f;

    [SerializeField] float smoothTime = 0.125f;

    [SerializeField] Transform cameraTransform;

    Vector2 look;

    float yVel;
    float xVel;

    float currentLookY;
    float currentLookX;

    PlayerInput playerInput;
    InputAction lookAction;

    void Awake()
    {
        float initialCameraX = Mathf.DeltaAngle(0, cameraTransform.localEulerAngles.x);
        float initialPlayerY = Mathf.DeltaAngle(0, transform.localEulerAngles.y);

        look.y = initialCameraX;
        currentLookY = initialCameraX;

        look.x = initialPlayerY;
        currentLookX = initialPlayerY;
    }
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

        look.y -= lookInput.y * sensitivity;
        look.x += lookInput.x * sensitivity;

        look.y = Mathf.Clamp(look.y, CameraYMin, CameraYMax);

        currentLookY = Mathf.SmoothDampAngle(currentLookY, look.y, ref yVel, smoothTime);
        currentLookX = Mathf.SmoothDampAngle(currentLookX, look.x, ref xVel, smoothTime);

        cameraTransform.localRotation = Quaternion.Euler(currentLookY, 0, 0);
        transform.localRotation = Quaternion.Euler(0, currentLookX, 0);
    }
}
