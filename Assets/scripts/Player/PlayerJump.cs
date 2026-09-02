using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(PlayerMov))]

public class PlayerJump : MonoBehaviour
{
    [SerializeField] float jumpSpeed = 2f;
    [SerializeField] float jumpPressBufferTime = .05f;
    [SerializeField] float jumpGroundedGraceTime = .02f;

    PlayerMov player;

    bool tryingToJump;
    float lastJumpPressTime;
    float lastGroundedTime;

    void Awake()
    {
        player = GetComponent<PlayerMov>();
    }

    void OnEnable()
    {
        player.OnBeforeMove += OnBeforeMove;
        player.OnGroundStateChange += OnGroundStateChange;
    }

    void OnDisable()
    {
        player.OnBeforeMove -= OnBeforeMove;
        player.OnGroundStateChange -= OnGroundStateChange;
    }

    void OnJump()
    {
        tryingToJump = true;
        lastJumpPressTime = Time.time;
    }

    void OnBeforeMove()
    {
        bool wasTryingToJump = Time.time - lastJumpPressTime < jumpPressBufferTime;
        bool wasGrounded = Time.time - lastGroundedTime < jumpGroundedGraceTime;

        bool isOrWasTryingToJump = tryingToJump || (wasTryingToJump && player.IsGrounded);
        bool isOrWasGrounded = player.IsGrounded || wasGrounded;

        if (isOrWasTryingToJump && isOrWasGrounded)
        {
            player.velocity.y += jumpSpeed;
        }
        tryingToJump = false;
    }

    void OnGroundStateChange(bool isGrounded)
    {
        if (!isGrounded) lastGroundedTime = Time.time;
    }
}
