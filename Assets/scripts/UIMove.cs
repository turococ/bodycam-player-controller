using UnityEngine;

public class UIMove : MonoBehaviour
{
    [SerializeField] PlayerMov player;
    [SerializeField] RectTransform rectTransform;

    [SerializeField] float sideShift = 18f;
    [SerializeField] float jumpDrop = 20f;
    [SerializeField] float fallRise = 12f;
    [SerializeField] float forwardZoom = 0.08f;
    [SerializeField] float shakeAmount = 3f;
    [SerializeField] float shakeSpeed = 12f;
    [SerializeField] float smoothness = 10f;
    [SerializeField] float verticalSmoothTime = 0.15f;

    Vector2 startPosition;
    Vector3 startScale;
    float timer;
    float verticalVelocity;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("Assign CharacterImage to Rect Transform in UIMove.", this);
            enabled = false;
            return;
        }

        startPosition = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;
    }

    void OnEnable()
    {
        player.OnBeforeMove += UpdateUI;
    }

    void OnDisable()
    {
        player.OnBeforeMove -= UpdateUI;
    }

    void UpdateUI()
    {
        Vector2 moveInput = player.MoveInput;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        Vector3 localMovement = new Vector3(moveInput.x, 0, moveInput.y);

        Vector2 targetPosition = startPosition;
        targetPosition.x -= localMovement.x * sideShift;

        if (!player.IsGrounded)
        {
            targetPosition.y += player.VerticalVelocity > 0f
                ? -jumpDrop
                : fallRise;
        }

        if (isMoving)
        {
            timer += Time.deltaTime * shakeSpeed;
            targetPosition += new Vector2(Mathf.Sin(timer) * shakeAmount, Mathf.Cos(timer * 2f) * shakeAmount);
        }

        float zoom = Mathf.Clamp(1f + localMovement.z * forwardZoom, 0.1f, 2f);

        Vector2 newPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * smoothness
        );

        newPosition.y = Mathf.SmoothDamp(
            rectTransform.anchoredPosition.y,
            targetPosition.y,
            ref verticalVelocity,
            verticalSmoothTime
        );

        rectTransform.anchoredPosition = newPosition;

        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, startScale * zoom, Time.deltaTime * smoothness);
    }
}
