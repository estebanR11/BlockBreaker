using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action OnStartGame;

    [Header("References")]
    [SerializeField] private PaddleMovement paddle;
    [SerializeField] private BallController ball;
    [SerializeField] private BallController ballPrefab;

    [Header("Power Ups")]
    [SerializeField] private float paddleEffectDuration = 8f;
    [SerializeField] private float ballEffectDuration = 6f;
    [SerializeField] private float expandPaddleMultiplier = 1.5f;
    [SerializeField] private float shrinkPaddleMultiplier = 0.7f;
    [SerializeField] private float fastBallMultiplier = 1.35f;
    [SerializeField] private float slowBallMultiplier = 0.7f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private BallController extraBallPrefab;
    [SerializeField] private bool spawnExtraBallNextToCurrentBall = true;
    [SerializeField] private float extraBallSideOffsetX = 0.35f;
    [SerializeField] private float extraBallHorizontalSpread = 0.45f;
    [SerializeField] private float extraBallSpawnOffsetY = 0.2f;

    [Header("Respawn")]
    [SerializeField] private Transform ballRespawnPoint;
    [SerializeField] private float ballRespawnOffsetY = 0.5f;

    private InputAction _launchAction;
    private InputAction _restartAction;
    private bool _gameStarted;
    private bool _isGameOver;
    private bool _isLevelCompleted;
    private int _lives;
    private Coroutine _paddleEffectRoutine;
    private Coroutine _ballEffectRoutine;
    private readonly HashSet<BallController> _activeBalls = new();
    private readonly HashSet<BlockController> _activeBlocks = new();

    public int Lives => _lives;
    public int ActiveBallCount => _activeBalls.Count;
    public int RemainingBlockCount => _activeBlocks.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _launchAction = new InputAction("Launch", InputActionType.Button);
        _launchAction.AddBinding("<Keyboard>/space");
        _launchAction.AddBinding("<Gamepad>/buttonSouth");
        _restartAction = new InputAction("Restart", InputActionType.Button);
        _restartAction.AddBinding("<Keyboard>/r");

        _lives = Mathf.Max(1, startingLives);

        if (paddle == null)
            paddle = FindFirstObjectByType<PaddleMovement>();

        if (ball == null)
            ball = FindFirstObjectByType<BallController>();

        RegisterExistingBalls();
        RegisterExistingBlocks();
    }

    private void OnEnable()
    {
        _launchAction.Enable();
        _launchAction.performed += OnLaunchPerformed;
        _restartAction.Enable();
        _restartAction.performed += OnRestartPerformed;
    }

    private void OnDisable()
    {
        _launchAction.performed -= OnLaunchPerformed;
        _launchAction.Disable();
        _restartAction.performed -= OnRestartPerformed;
        _restartAction.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        _launchAction.Dispose();
        _restartAction.Dispose();
    }

    private void OnLaunchPerformed(InputAction.CallbackContext context)
    {
        if (_isGameOver || _isLevelCompleted)
            return;

        StartGame();
    }

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        if (!_isGameOver && !_isLevelCompleted)
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartGame()
    {
        if (_gameStarted)
            return;

        _gameStarted = true;
        OnStartGame?.Invoke();
    }

    public void RegisterBall(BallController ballController)
    {
        if (ballController == null)
            return;

        _activeBalls.Add(ballController);
    }

    public void RegisterBlock(BlockController blockController)
    {
        if (blockController == null)
            return;

        _activeBlocks.Add(blockController);
    }

    public void NotifyBlockDestroyed(BlockController destroyedBlock)
    {
        if (destroyedBlock != null)
            _activeBlocks.Remove(destroyedBlock);

        if (_activeBlocks.Count > 0)
            return;

        HandleLevelCompleted();
    }

    public void NotifyBallLost(BallController lostBall)
    {
        if (lostBall != null)
            _activeBalls.Remove(lostBall);

        ball = GetAnyActiveBall();

        if (_activeBalls.Count > 0)
            return;

        HandleAllBallsLost();
    }

    public void ApplyPowerUp(BlockController.PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case BlockController.PowerUpType.None:
                break;
            case BlockController.PowerUpType.ExpandPaddle:
                ApplyPaddleWidthTemporarily(expandPaddleMultiplier);
                break;
            case BlockController.PowerUpType.ShrinkPaddle:
                ApplyPaddleWidthTemporarily(shrinkPaddleMultiplier);
                break;
            case BlockController.PowerUpType.SlowBall:
                ApplyBallSpeedTemporarily(slowBallMultiplier);
                break;
            case BlockController.PowerUpType.FastBall:
                ApplyBallSpeedTemporarily(fastBallMultiplier);
                break;
            case BlockController.PowerUpType.ExtraLife:
                _lives++;
                break;
            case BlockController.PowerUpType.ExtraBall:
                SpawnExtraBall();
                break;
            default:
                Debug.LogWarning($"Unhandled power-up type: {powerUpType}");
                break;
        }
    }

    private void ApplyPaddleWidthTemporarily(float targetMultiplier)
    {
        if (paddle == null)
            return;

        if (_paddleEffectRoutine != null)
            StopCoroutine(_paddleEffectRoutine);

        _paddleEffectRoutine = StartCoroutine(PaddleWidthRoutine(targetMultiplier));
    }

    private IEnumerator PaddleWidthRoutine(float targetMultiplier)
    {
        float previousMultiplier = paddle.WidthMultiplier;
        paddle.SetWidthMultiplier(targetMultiplier);

        yield return new WaitForSeconds(paddleEffectDuration);

        if (paddle != null)
            paddle.SetWidthMultiplier(previousMultiplier);

        _paddleEffectRoutine = null;
    }

    private void ApplyBallSpeedTemporarily(float speedMultiplier)
    {
        if (ball == null)
            return;

        if (_ballEffectRoutine != null)
            StopCoroutine(_ballEffectRoutine);

        _ballEffectRoutine = StartCoroutine(BallSpeedRoutine(speedMultiplier));
    }

    private IEnumerator BallSpeedRoutine(float speedMultiplier)
    {
        float previousSpeed = ball.CurrentSpeed;
        float targetSpeed = previousSpeed * Mathf.Max(0.1f, speedMultiplier);
        ball.SetCurrentSpeed(targetSpeed);

        yield return new WaitForSeconds(ballEffectDuration);

        if (ball != null)
            ball.SetCurrentSpeed(previousSpeed);

        _ballEffectRoutine = null;
    }

    private void SpawnExtraBall()
    {
        BallController referenceBall = GetAnyActiveBall();
        BallController template = extraBallPrefab != null ? extraBallPrefab : (referenceBall != null ? referenceBall : ball);
        if (template == null)
        {
            Debug.LogWarning("ExtraBall could not spawn: no ball template found.");
            return;
        }

        Vector3 spawnPosition;
        if (spawnExtraBallNextToCurrentBall && referenceBall != null)
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            spawnPosition = referenceBall.transform.position +
                            new Vector3(side * Mathf.Abs(extraBallSideOffsetX), extraBallSpawnOffsetY, 0f);
        }
        else if (referenceBall != null)
        {
            spawnPosition = referenceBall.transform.position + Vector3.up * extraBallSpawnOffsetY;
        }
        else if (paddle != null)
        {
            spawnPosition = paddle.transform.position + Vector3.up * extraBallSpawnOffsetY;
        }
        else
        {
            spawnPosition = template.transform.position;
        }

        BallController spawnedBall = Instantiate(template, spawnPosition, Quaternion.identity);
        RegisterBall(spawnedBall);

        ball = referenceBall != null ? referenceBall : spawnedBall;

        float horizontal = UnityEngine.Random.Range(-extraBallHorizontalSpread, extraBallHorizontalSpread);
        Vector2 direction = new Vector2(horizontal, 1f).normalized;
        spawnedBall.LaunchWithDirection(direction);
    }

    private void RegisterExistingBalls()
    {
        BallController[] ballsInScene = FindObjectsByType<BallController>(FindObjectsSortMode.None);

        for (int i = 0; i < ballsInScene.Length; i++)
            RegisterBall(ballsInScene[i]);

        ball = GetAnyActiveBall();
    }

    private void RegisterExistingBlocks()
    {
        BlockController[] blocksInScene = FindObjectsByType<BlockController>(FindObjectsSortMode.None);

        for (int i = 0; i < blocksInScene.Length; i++)
            RegisterBlock(blocksInScene[i]);
    }

    private void HandleAllBallsLost()
    {
        if (_isLevelCompleted)
            return;

        _lives = Mathf.Max(0, _lives - 1);

        if (_lives <= 0)
        {
            _isGameOver = true;
            Debug.Log("Game Over: no lives left.");
            return;
        }

        SpawnReplacementBall();
        _gameStarted = false;
    }

    private void HandleLevelCompleted()
    {
        if (_isLevelCompleted)
            return;

        _isLevelCompleted = true;
        _gameStarted = false;
        PauseAllBalls();
        Debug.Log("Level Complete: all blocks destroyed. Press R to restart.");
    }

    private void PauseAllBalls()
    {
        List<BallController> toRemove = null;

        foreach (BallController activeBall in _activeBalls)
        {
            if (activeBall == null)
            {
                if (toRemove == null)
                    toRemove = new List<BallController>();

                toRemove.Add(activeBall);
                continue;
            }

            activeBall.PauseBall();
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            _activeBalls.Remove(toRemove[i]);
    }

    private void SpawnReplacementBall()
    {
        BallController template = ballPrefab != null ? ballPrefab : (extraBallPrefab != null ? extraBallPrefab : ball);
        if (template == null)
        {
            Debug.LogWarning("Cannot respawn ball: no ball prefab/template assigned.");
            return;
        }

        Vector3 spawnPosition;
        if (ballRespawnPoint != null)
        {
            spawnPosition = ballRespawnPoint.position;
        }
        else if (paddle != null)
        {
            spawnPosition = paddle.transform.position + Vector3.up * ballRespawnOffsetY;
        }
        else
        {
            spawnPosition = template.transform.position;
        }

        BallController spawnedBall = Instantiate(template, spawnPosition, Quaternion.identity);
        RegisterBall(spawnedBall);
        ball = spawnedBall;
    }

    private BallController GetAnyActiveBall()
    {
        BallController fallback = null;
        List<BallController> toRemove = null;

        foreach (BallController activeBall in _activeBalls)
        {
            if (activeBall == null)
            {
                if (toRemove == null)
                    toRemove = new List<BallController>();

                toRemove.Add(activeBall);
                continue;
            }

            fallback = activeBall;
            break;
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
                _activeBalls.Remove(toRemove[i]);
        }

        return fallback;
    }
}
