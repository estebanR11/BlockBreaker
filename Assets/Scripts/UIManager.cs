using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text stateText;

    [Header("Messages")]
    [SerializeField] private string livesPrefix = "Lives: ";
    [SerializeField] private string startMessage = "Press Space to start";
    [SerializeField] private string gameOverMessage = "You lost. Press R to restart";
    [SerializeField] private string levelCompleteMessage = "You won. Press R to restart";

    private GameManager _gameManager;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshFromSnapshot();
    }

    private void Start()
    {
        // Fallback in case GameManager initializes later due to execution order.
        TrySubscribe();
        RefreshFromSnapshot();
    }

    private void OnDisable()
    {
        if (_gameManager == null)
            return;

        _gameManager.OnLivesChanged -= HandleLivesChanged;
        _gameManager.OnWaitingForStartChanged -= HandleWaitingForStartChanged;
        _gameManager.OnGameOver -= HandleGameOver;
        _gameManager.OnLevelCompleted -= HandleLevelCompleted;
        _gameManager = null;
    }

    private void TrySubscribe()
    {
        if (_gameManager != null)
            return;

        if (GameManager.Instance == null)
            return;

        _gameManager = GameManager.Instance;
        _gameManager.OnLivesChanged += HandleLivesChanged;
        _gameManager.OnWaitingForStartChanged += HandleWaitingForStartChanged;
        _gameManager.OnGameOver += HandleGameOver;
        _gameManager.OnLevelCompleted += HandleLevelCompleted;
    }

    private void RefreshFromSnapshot()
    {
        if (_gameManager == null)
            return;

        HandleLivesChanged(_gameManager.Lives);

        if (_gameManager.IsGameOver)
        {
            HandleGameOver();
            return;
        }

        if (_gameManager.IsLevelCompleted)
        {
            HandleLevelCompleted();
            return;
        }

        HandleWaitingForStartChanged(!_gameManager.HasGameStarted);
    }

    private void HandleLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = $"{livesPrefix}{lives}";
    }

    private void HandleWaitingForStartChanged(bool isWaitingForStart)
    {
        if (stateText == null || _gameManager == null)
            return;

        if (_gameManager.IsGameOver || _gameManager.IsLevelCompleted)
            return;

        stateText.text = isWaitingForStart ? startMessage : string.Empty;
    }

    private void HandleGameOver()
    {
        if (stateText != null)
            stateText.text = gameOverMessage;
    }

    private void HandleLevelCompleted()
    {
        if (stateText != null)
            stateText.text = levelCompleteMessage;
    }
}
