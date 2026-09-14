/*
 * Copyright (c) 2026 turococ
 * Licensed under the MIT License. See LICENSE file in the project root for full license information.
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRun : MonoBehaviour
{
    PlayerMov playerMov;

    PlayerInput playerInput;
    InputAction runningAction;

    bool IsRunning;

    internal bool RunFlag;

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
        bool IsRunning = runningAction.IsPressed();
        
        RunFlag = IsRunning ? true : false;

        playerMov.movementSpeed = IsRunning ? playerMov.runningSpeed : playerMov.walkSpeedOrigin;
    }
}
