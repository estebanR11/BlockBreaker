using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float speed = 8f;
    [Min(0f)]
    [SerializeField] private float speedIncreasePerHit = 0.2f;
    [Min(0f)]
    [SerializeField] private float maxSpeed = 14f;
    [Range(0f, 1f)]
    [SerializeField] private float bounceInfluence = 0.9f;

    [Tooltip("Initial launch direction. Use Y negative to go down.")]
    [SerializeField] private Vector2 initialDirection = Vector2.down;

    private Rigidbody2D _rb;
    private bool _isLaunched;
    private GameManager _subscribedManager;
    private float _currentSpeed;

    public bool IsLaunched => _isLaunched;
    public float CurrentSpeed => _currentSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
        {
            Debug.LogError($"{name}: BallController requires a Rigidbody2D.");
            enabled = false;
            return;
        }

        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        _currentSpeed = speed;
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
    }

    private void Start()
    {
        // Fallback in case GameManager initializes later due to execution order.
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    private void FixedUpdate()
    {
        if (!_isLaunched)
            return;

        if (_rb.linearVelocity.sqrMagnitude < 0.0001f)
        {
            _rb.linearVelocity = GetSafeDirection() * _currentSpeed;
            return;
        }

        _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
    }

    public void Launch()
    {
        if (_isLaunched || _rb == null)
            return;

        _isLaunched = true;
        _rb.linearVelocity = GetSafeDirection() * _currentSpeed;
    }

    public void LaunchWithDirection(Vector2 direction)
    {
        if (_rb == null)
            return;

        Vector2 safeDirection = direction.sqrMagnitude < 0.0001f ? GetSafeDirection() : direction.normalized;
        _isLaunched = true;
        _rb.linearVelocity = safeDirection * _currentSpeed;
    }

    public void ResetBall()
    {
        _isLaunched = false;
        _currentSpeed = speed;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isLaunched)
            return;

        IncreaseSpeed();

        PaddleMovement paddle = collision.collider.GetComponent<PaddleMovement>();
        if (paddle == null)
            return;

        ApplyPaddleBounce(collision.collider);
    }

    private Vector2 GetSafeDirection()
    {
        if (initialDirection.sqrMagnitude < 0.0001f)
            return Vector2.down;

        return initialDirection.normalized;
    }

    private void SubscribeToGameManager()
    {
        if (_subscribedManager != null)
            return;

        if (GameManager.Instance == null)
            return;

        _subscribedManager = GameManager.Instance;
        _subscribedManager.OnStartGame += Launch;
    }

    private void UnsubscribeFromGameManager()
    {
        if (_subscribedManager == null)
            return;

        _subscribedManager.OnStartGame -= Launch;
        _subscribedManager = null;
    }

    private void IncreaseSpeed()
    {
        _currentSpeed = Mathf.Min(_currentSpeed + speedIncreasePerHit, maxSpeed);
    }

    private void ApplyPaddleBounce(Collider2D paddleCollider)
    {
        float paddleCenterX = paddleCollider.bounds.center.x;
        float halfWidth = Mathf.Max(0.01f, paddleCollider.bounds.extents.x);
        float offsetFromCenter = transform.position.x - paddleCenterX;
        float normalizedOffset = Mathf.Clamp(offsetFromCenter / halfWidth, -1f, 1f);

        float horizontal = normalizedOffset * bounceInfluence;
        float vertical = 1f - Mathf.Abs(horizontal);
        Vector2 bounceDirection = new Vector2(horizontal, Mathf.Max(0.2f, vertical)).normalized;

        _rb.linearVelocity = bounceDirection * _currentSpeed;
    }

    public void SetCurrentSpeed(float newSpeed)
    {
        _currentSpeed = Mathf.Clamp(newSpeed, 0f, maxSpeed);

        if (!_isLaunched || _rb == null)
            return;

        if (_rb.linearVelocity.sqrMagnitude < 0.0001f)
        {
            _rb.linearVelocity = GetSafeDirection() * _currentSpeed;
            return;
        }

        _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
    }

    public void PauseBall()
    {
        _isLaunched = false;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }
}
