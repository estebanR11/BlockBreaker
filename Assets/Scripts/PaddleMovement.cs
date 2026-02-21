using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleMovement : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float speed = 10f;

    [Header("Horizontal Limits")]
    [SerializeField] private float minX = -7.5f;
    [SerializeField] private float maxX = 7.5f;

    private InputAction _moveAction;
    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;

        _moveAction = new InputAction("Move", InputActionType.Value);

        // 2D vector from keyboard arrows/WASD.
        _moveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s");

        _moveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow");

        // Gamepad left stick.
        _moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable()
    {
        _moveAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    private void OnDestroy()
    {
        _moveAction.Dispose();
    }

    private void Update()
    {
        ValidateLimits();

        Vector2 input = _moveAction.ReadValue<Vector2>();
        float horizontal = input.x;

        Vector3 position = transform.position;
        position.x += horizontal * speed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, minX, maxX);

        transform.position = position;
    }

    private void OnValidate()
    {
        ValidateLimits();
    }

    private void ValidateLimits()
    {
        if (minX > maxX)
        {
            float temp = minX;
            minX = maxX;
            maxX = temp;
        }
    }

    public float WidthMultiplier
    {
        get
        {
            if (_baseScale.x <= 0.0001f)
                return 1f;

            return transform.localScale.x / _baseScale.x;
        }
    }

    public void SetWidthMultiplier(float multiplier)
    {
        float safeMultiplier = Mathf.Max(0.1f, multiplier);
        Vector3 newScale = _baseScale;
        newScale.x *= safeMultiplier;
        transform.localScale = newScale;
    }

    public void ResetWidth()
    {
        transform.localScale = _baseScale;
    }
}
