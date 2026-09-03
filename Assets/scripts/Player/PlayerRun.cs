using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRun : MonoBehaviour
{
    PlayerMov playerMov;

    PlayerInput playerInput;
    InputAction runningAction;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerMov = GetComponent<PlayerMov>();

        runningAction = playerInput.actions["sprint"];
    }

    void Update()
    {
        RunningAction();
    }

    void RunningAction()
    {
        bool isRunning = runningAction.IsPressed();

        playerMov.movementSpeed = isRunning ? playerMov.runningSpeed : playerMov.walkSpeedOrigin;
    }
}
