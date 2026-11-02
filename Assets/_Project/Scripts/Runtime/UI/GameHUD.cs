using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD: здоровье базы в левом верхнем углу, золото в правом верхнем.
/// Весь интерфейс создаётся из кода, настраивать Canvas в сцене не нужно.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Tooltip("Менеджер игры. Если не задан — ищется в сцене.")]
    [SerializeField] private GameManager gameManager;

    [Header("Base health bar")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(360f, 36f);
    [SerializeField] private Color healthBackColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color healthFillColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    [Header("Layout")]
    [SerializeField] private Vector2 margin = new Vector2(30f, 30f);

    private UIBar _healthBar;
    private Text _healthText;
    private Text _goldText;
    private Text _waveText;
    private Text _gameOverText;
    private EnemySpawner _spawner;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError($"{name}: в сцене нет GameManager.", this);
            enabled = false;
            return;
        }

        _spawner = FindFirstObjectByType<EnemySpawner>();
        BuildUI();
    }

    private void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.OnGoldChanged += UpdateGold;
        gameManager.OnBaseHealthChanged += UpdateHealth;
        gameManager.OnGameOver += ShowGameOver;
        if (_spawner != null) _spawner.OnWaveStarted += UpdateWave;
    }

    private void Start()
    {
        // Начальные значения читаем в Start, когда все Awake уже выполнены.
        if (gameManager == null) return;
        UpdateGold(gameManager.Gold);
        UpdateHealth(gameManager.BaseHealth, gameManager.MaxBaseHealth);
        if (_spawner != null) UpdateWave(Mathf.Max(0, _spawner.CurrentWaveIndex));
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.OnGoldChanged -= UpdateGold;
        gameManager.OnBaseHealthChanged -= UpdateHealth;
        gameManager.OnGameOver -= ShowGameOver;
        if (_spawner != null) _spawner.OnWaveStarted -= UpdateWave;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Здоровье базы — левый верхний угол.
        _healthBar = UIBar.Create(canvasGo.transform, "BaseHealthBar", healthBarSize, healthBackColor, healthFillColor);
        Anchor(_healthBar.Root, new Vector2(0f, 1f), new Vector2(margin.x, -margin.y));

        _healthText = UIBar.CreateText(_healthBar.Root, "HealthText", 24, TextAnchor.MiddleCenter, Color.white);
        Stretch(_healthText.rectTransform);

        // Золото — правый верхний угол.
        _goldText = UIBar.CreateText(canvasGo.transform, "GoldText", 40, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.2f));
        _goldText.rectTransform.sizeDelta = new Vector2(400f, 60f);
        Anchor(_goldText.rectTransform, new Vector2(1f, 1f), new Vector2(-margin.x, -margin.y));

        // Номер волны — слева от золота.
        _waveText = UIBar.CreateText(canvasGo.transform, "WaveText", 40, TextAnchor.UpperRight, Color.white);
        _waveText.rectTransform.sizeDelta = new Vector2(400f, 60f);
        Anchor(_waveText.rectTransform, new Vector2(1f, 1f), new Vector2(-margin.x - 320f, -margin.y));

        // Надпись об окончании игры.
        _gameOverText = UIBar.CreateText(canvasGo.transform, "GameOverText", 90, TextAnchor.MiddleCenter, Color.white);
        _gameOverText.text = "GAME OVER";
        _gameOverText.rectTransform.anchorMin = _gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _gameOverText.rectTransform.sizeDelta = new Vector2(1000f, 200f);
        _gameOverText.gameObject.SetActive(false);
    }

    private void UpdateGold(int gold)
    {
        _goldText.text = $"Gold: {gold}";
    }

    private void UpdateWave(int waveIndex)
    {
        _waveText.text = $"Wave: {waveIndex + 1} / {_spawner.WaveCount}";
    }

    private void UpdateHealth(int current, int max)
    {
        _healthBar.SetValue(max > 0 ? (float)current / max : 0f);
        _healthText.text = $"{current} / {max}";
    }

    private void ShowGameOver()
    {
        _gameOverText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Привязывает элемент к углу канваса: pivot и anchor совпадают с corner.
    /// </summary>
    private static void Anchor(RectTransform rect, Vector2 corner, Vector2 offset)
    {
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = offset;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
