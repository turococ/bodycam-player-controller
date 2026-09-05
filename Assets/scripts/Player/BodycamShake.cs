using Unity.Collections;
using UnityEngine;

public class BodycamShake : MonoBehaviour
{
    [Header("Walk & Run Impact")]
    [SerializeField] float walkFrequency = 10f;
    [SerializeField] float RunFrequency = 20f;
    [SerializeField] float verticalBobAmount = 0.04f;
    [SerializeField] float horizontalSway = 0.015f;

    [Header("Elastic Tilt (walk)")]
    [SerializeField] float walkTiltAmount = 1.5f;
    [SerializeField] float walkTiltSpeed = 1f;

    [Header("Elastic Tilt (run)")]
    [SerializeField] float runTiltAmount = 7.0f;
    [SerializeField] float runTiltSpeed = 8f;

    [Header("Perlin Organic Noise (Walk)")]
    [SerializeField] float walkNoiseFrequency = 2f;
    [SerializeField] float walkNoisePosAmount = 0.005f;
    [SerializeField] float walkNoiseRotAmount = 0.25f;

    [Header("Perlin Organic Noise (Run)")]
    [SerializeField] float runNoiseFrequency = 5f;
    [SerializeField] float runNoisePosAmount = 0.015f;
    [SerializeField] float runNoiseRotAmount = 0.75f;

    [Header("Inertia & Lean")]
    [SerializeField] float mouseTiltAmount = 1.5f;

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.08f;
    [SerializeField] float moveThreshold = 0.1f;
    [SerializeField] float runTransitionSpeed = 5f;

    [Header("Reference")]
    [SerializeField] Transform cameraTransform;

    Vector3 basePosition;

    PlayerMov player;
    CameraController plCam;
    PlayerRun plRun;

    Vector3 targetPos;
    Vector3 currentPos;
    Vector3 posVel;

    Vector3 targetRot;
    Vector3 currentRot;
    Vector3 rotVel;

    float stepCycle = 0f;
    float lastStepSin = 0f;
    float impactProgress = 1f;
    float tiltDirection = 1f;

    float runFactor = 0f;

    void Awake()
    {
        player = GetComponent<PlayerMov>();
        plCam = GetComponent<CameraController>();
        plRun = GetComponent<PlayerRun>();
        basePosition = cameraTransform.localPosition;
    }

    void LateUpdate()
    {
        var IsRunning = plRun != null && plRun.RunFlag;
        var input = player.MoveInput;
        var look = plCam.Look;
        var moveMag = input.magnitude;
        var isMoving = player.IsGrounded && moveMag > moveThreshold;

        var targetRunFactor = (IsRunning && isMoving) ? 1f : 0f;
        runFactor = Mathf.MoveTowards(runFactor, targetRunFactor, Time.deltaTime * runTransitionSpeed);

        var currentNoiseFreq = Mathf.Lerp(walkNoiseFrequency, runNoiseFrequency, runFactor);
        var currentNoisePos = Mathf.Lerp(walkNoisePosAmount, runNoisePosAmount, runFactor);
        var currentNoiseRot = Mathf.Lerp(walkNoiseRotAmount, runNoiseRotAmount, runFactor);

        var noiseX = (Mathf.PerlinNoise(Time.time * currentNoiseFreq, 0f) - 0.5f) * 2f;
        var noiseY = (Mathf.PerlinNoise(0f, Time.time * currentNoiseFreq) - 0.5f) * 2f;
        var noiseZ = (Mathf.PerlinNoise(Time.time * currentNoiseFreq, Time.time * currentNoiseFreq) - 0.5f) * 2f;

        var idlePosNoise = new Vector3(noiseX, noiseY, 0f) * currentNoisePos;
        var idleRotNoise = new Vector3(noiseY, noiseX, noiseZ) * currentNoiseRot;

        var walkPosOffset = Vector3.zero;

        var currentTiltAmount = Mathf.Lerp(walkTiltAmount, runTiltAmount, runFactor);
        var currentTiltSpeed = Mathf.Lerp(walkTiltSpeed, runTiltSpeed, runFactor);
        var currentStepFreq = Mathf.Lerp(walkFrequency, RunFrequency, runFactor);

        if (isMoving)
        {
            stepCycle += Time.deltaTime * currentStepFreq * moveMag;
            var currentSin = Mathf.Sin(stepCycle);

            if (lastStepSin > 0f && currentSin <= 0f)
            {
                impactProgress = 0f;
                tiltDirection *= -1f;
            }
            lastStepSin = currentSin;

            var stepVertical = -Mathf.Abs(currentSin) * verticalBobAmount;
            var stepHorizontal = Mathf.Cos(stepCycle * 0.5f) * horizontalSway;

            walkPosOffset = new Vector3(stepHorizontal, stepVertical, 0f);
        }

        var elasticRoll = 0f;

        if (impactProgress < 1f)
        {
            impactProgress += Time.deltaTime * currentTiltSpeed;
            var elasticFactor = 1f - EaseOutElastic(impactProgress);

            elasticRoll = elasticFactor * currentTiltAmount * tiltDirection;
        }

        var mouseLean = -look.x * mouseTiltAmount;

        targetPos = basePosition + idlePosNoise + walkPosOffset;
        targetRot = idleRotNoise + new Vector3(0f, 0f, elasticRoll + mouseLean);

        currentPos = Vector3.SmoothDamp(currentPos, targetPos, ref posVel, smoothTime);
        currentRot = Vector3.SmoothDamp(currentRot, targetRot, ref rotVel, smoothTime);

        cameraTransform.localPosition = currentPos;

        var currentLocalEuler = cameraTransform.localEulerAngles;
        var pitch = NormalizeAngle(currentLocalEuler.x);
        var yaw = NormalizeAngle(currentLocalEuler.y);

        cameraTransform.localRotation = Quaternion.Euler(pitch + currentRot.x, yaw + currentRot.y, currentRot.z);
    }

    float EaseOutElastic(float x)
    {
        x = Mathf.Clamp01(x);

        if (x == 0f) return 0f;
        if (x == 1f) return 1f;

        const float c4 = (2f * Mathf.PI) / 3f;

        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        else if (angle <= -180f)
            angle += 360f;

        return angle;
    }
}