using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float CrouchTransitionSpeed = 1f;

    float standartHeight;
    float currentHeight;
    internal bool isCrouching;
    
    CharacterController controller;
    PlayerInput  playerInput;
    InputAction crouchAction;
        
    
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<CharacterController>();
        
        crouchAction = playerInput.actions["Crouch"];
    }

    void Start()
    {
        standartHeight = currentHeight = controller.height;
    }

    void Update()
    {
        CrouchUpdate();
    }

    void CrouchUpdate()
    {
        isCrouching = crouchAction.IsPressed();

        float crouchDelta = Time.deltaTime * CrouchTransitionSpeed;

        if (isCrouching)
        {
            currentHeight = Mathf.Lerp(currentHeight, crouchHeight, crouchDelta);
            controller.height = currentHeight;
        }
        else
        {
            float heightTarget = standartHeight;

            if (!isCrouching && isCrouching)
            {
                var castOrigin = transform.position + new Vector3(0, currentHeight / 2, 0);
                if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, 0.2f))
                {
                    var DistanceToCeiling = hit.point.y - castOrigin.y;
                    heightTarget = Mathf.Max
                    (
                        currentHeight + DistanceToCeiling - 0.1f,
                        crouchHeight
                    );
                }
            }
            
            currentHeight = Mathf.Lerp(currentHeight, heightTarget, crouchDelta);
            controller.height = currentHeight;
        }
    }
}
