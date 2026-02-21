using UnityEngine;

public class BlockController : MonoBehaviour
{
    public enum PowerUpType
    {
        None = 0,
        ExpandPaddle = 1,
        ShrinkPaddle = 2,
        ExtraBall = 3,
        SlowBall = 4,
        FastBall = 5,
        ExtraLife = 6
    }

    [Header("Hit Detection")]
    [SerializeField] private string ballTag = "Player";
    [SerializeField] private bool destroyWhenBallHits = true;

    [Header("Special Block")]
    [SerializeField] private bool isSpecialBlock;
    [SerializeField] private PowerUpType selectedPowerUp = PowerUpType.ExpandPaddle;
    [Tooltip("Optional prefab to spawn when this special block is destroyed.")]
    [SerializeField] private GameObject powerUpPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip breakSfx;
    [Range(0f, 1f)]
    [SerializeField] private float breakSfxVolume = 1f;

    public static event System.Action<PowerUpType, Vector3> OnSpecialBlockDestroyed;
    private bool _hasBeenHit;
    private bool _isRegistered;

    private void OnEnable()
    {
        TryRegisterBlock();
    }

    private void Start()
    {
        TryRegisterBlock();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!destroyWhenBallHits)
            return;

        if (!collision.collider.CompareTag(ballTag))
            return;

        HandleBallHit();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!destroyWhenBallHits)
            return;

        if (!other.CompareTag(ballTag))
            return;

        HandleBallHit();
    }

    private void HandleBallHit()
    {
        if (_hasBeenHit)
            return;

        _hasBeenHit = true;

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBlockDestroyed(this);

        if (isSpecialBlock)
            TriggerSpecialPowerUp();

        PlayBreakSfx();
        Destroy(gameObject);
    }

    private void TriggerSpecialPowerUp()
    {
        if (powerUpPrefab != null)
        {
            GameObject spawnedPowerUp = Instantiate(powerUpPrefab, transform.position, Quaternion.identity);
            PowerUpPickup pickup = spawnedPowerUp.GetComponent<PowerUpPickup>();

            if (pickup != null)
                pickup.Configure(selectedPowerUp);
        }
        else if (GameManager.Instance != null)
        {
            // If no pickup prefab is assigned, apply immediately.
            GameManager.Instance.ApplyPowerUp(selectedPowerUp);
        }

        OnSpecialBlockDestroyed?.Invoke(selectedPowerUp, transform.position);
    }

    private void PlayBreakSfx()
    {
        if (breakSfx == null)
            return;

        AudioSource.PlayClipAtPoint(breakSfx, transform.position, breakSfxVolume);
    }

    private void TryRegisterBlock()
    {
        if (_isRegistered)
            return;

        if (GameManager.Instance == null)
            return;

        GameManager.Instance.RegisterBlock(this);
        _isRegistered = true;
    }
}
