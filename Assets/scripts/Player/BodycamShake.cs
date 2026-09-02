using UnityEngine;

public class BodycamShake : MonoBehaviour
{
    [Header("Movement Shake")]
    [SerializeField] float walkBobAmplitude = 0.02f;
    [SerializeField] float runBobAmplitude = 0.05f;
    [SerializeField] float bobSpeed = 8f;
    [SerializeField] float horizontalShake = 0.005f;
    [SerializeField] float rollAmount = 0.3f;

    [Header("Idle Breathing")]
    [SerializeField] float idleAmplitude = 0.003f;
    [SerializeField] float idleSpeed = 1.5f;
    [SerializeField] float idleHorizontal = 0.002f;
    [SerializeField] float idleRoll = 0.1f;

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.1f;
    [SerializeField] float moveThreshold = 0.1f;

    [Header("Reference")]
    [SerializeField] Transform cameraTransform;

    PlayerMov player;

    float bobVel;
    float hxVel;
    float hyVel;
    float rollVel;
    float currentRoll;
    float currentBob = 0f;
    float currentHX = 0f;
    float currentHY = 0f;

    void Awake()
    {
        player = GetComponent<PlayerMov>();
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    void LateUpdate()
    {
        float moveMag = player.MoveInput.magnitude;
        bool moving = player.IsGrounded && moveMag > moveThreshold;

        float amplitude = moving ? Mathf.Lerp(walkBobAmplitude, runBobAmplitude, moveMag) : idleAmplitude;
        float speed = moving ? bobSpeed : idleSpeed;
        float hShake = moving ? horizontalShake : idleHorizontal;
        float rAmount = moving ? rollAmount : idleRoll;

        float targetBob = Mathf.Sin(Time.time * speed) * amplitude;
        float targetHX = Mathf.Sin(Time.time * speed * 0.61f) * hShake;
        float targetHY = Mathf.Cos(Time.time * speed * 0.73f) * hShake;
        float targetRoll = Mathf.Sin(Time.time * speed * 0.5f) * rAmount;

        currentBob = Mathf.SmoothDamp(currentBob, targetBob, ref bobVel, smoothTime);
        currentHX = Mathf.SmoothDamp(currentHX, targetHX, ref hxVel, smoothTime);
        currentHY = Mathf.SmoothDamp(currentHY, targetHY, ref hyVel, smoothTime);
        currentRoll = Mathf.SmoothDampAngle(currentRoll, targetRoll, ref rollVel, smoothTime);

        cameraTransform.localPosition = new Vector3(currentHX, currentBob, currentHY);

        Vector3 angles = cameraTransform.localEulerAngles;
        float pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
    }

    public void ResetShake()
    {
        bobVel = 0f;
        hxVel = 0f;
        hyVel = 0f;
        rollVel = 0f;
        currentRoll = 0f;
        cameraTransform.localPosition = Vector3.zero;

        Vector3 angles = cameraTransform.localEulerAngles;
        float pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}