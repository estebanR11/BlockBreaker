using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private string ballTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null)
            ball = other.GetComponentInParent<BallController>();

        if (ball == null)
            return;

        if (!ball.CompareTag(ballTag))
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBallLost(ball);

        Destroy(ball.gameObject);
    }
}
