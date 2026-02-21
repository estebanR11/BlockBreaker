using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Header("Power Up")]
    [SerializeField] private BlockController.PowerUpType powerUpType = BlockController.PowerUpType.ExpandPaddle;

    [Header("Fall")]
    [Min(0f)]
    [SerializeField] private float fallSpeed = 3.5f;
    [SerializeField] private float destroyY = -7f;

    [Header("Pickup")]
    [SerializeField] private string paddleTag = "Paddle";

    public void Configure(BlockController.PowerUpType selectedType)
    {
        powerUpType = selectedType;
    }

    private void Update()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

        if (transform.position.y <= destroyY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(paddleTag))
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.ApplyPowerUp(powerUpType);

        Destroy(gameObject);
    }
}
