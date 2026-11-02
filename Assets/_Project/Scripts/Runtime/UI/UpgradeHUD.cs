using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Нижние панели: слева — переход башни на следующий уровень (с ценой),
/// справа — улучшение скорострельности и урона (с ценами).
/// Интерфейс создаётся из кода; EventSystem добавляется автоматически, если его нет.
/// </summary>
public class UpgradeHUD : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TowerUpgradeManager upgrades;

    [Header("Layout")]
    [SerializeField] private Vector2 margin = new Vector2(30f, 30f);
    [SerializeField] private Vector2 buttonSize = new Vector2(360f, 90f);
    [SerializeField] private float spacing = 15f;

    private Button _towerButton;
    private Text _towerLabel;
    private Button _fireRateButton;
    private Text _fireRateLabel;
    private Button _damageButton;
    private Text _damageLabel;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (upgrades == null) upgrades = FindFirstObjectByType<TowerUpgradeManager>();

        if (gameManager == null || upgrades == null)
        {
            Debug.LogError($"{name}: нужны GameManager и TowerUpgradeManager в сцене.", this);
            enabled = false;
            return;
        }

        EnsureEventSystem();
        BuildUI();
    }

    private void OnEnable()
    {
        if (!enabled || upgrades == null) return;
        gameManager.OnGoldChanged += OnGoldChanged;
        upgrades.OnUpgradesChanged += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (upgrades == null || gameManager == null) return;
        gameManager.OnGoldChanged -= OnGoldChanged;
        upgrades.OnUpgradesChanged -= Refresh;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("Upgrade Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasGo.transform;

        // Слева внизу — апгрейд башни до следующего уровня.
        _towerButton = UIBar.CreateButton(root, "UpgradeTowerButton", buttonSize,
            new Color(0.2f, 0.45f, 0.8f), out _towerLabel);
        Anchor(_towerButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(margin.x, margin.y));
        _towerButton.onClick.AddListener(() => upgrades.TryUpgradeTower());

        // Справа внизу — характеристики (нижняя кнопка — скорострельность, над ней — урон).
        _fireRateButton = UIBar.CreateButton(root, "UpgradeFireRateButton", buttonSize,
            new Color(0.2f, 0.65f, 0.35f), out _fireRateLabel);
        Anchor(_fireRateButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-margin.x, margin.y));
        _fireRateButton.onClick.AddListener(() => upgrades.TryUpgradeFireRate());

        _damageButton = UIBar.CreateButton(root, "UpgradeDamageButton", buttonSize,
            new Color(0.8f, 0.35f, 0.2f), out _damageLabel);
        Anchor(_damageButton.GetComponent<RectTransform>(), new Vector2(1f, 0f),
            new Vector2(-margin.x, margin.y + buttonSize.y + spacing));
        _damageButton.onClick.AddListener(() => upgrades.TryUpgradeDamage());
    }

    private void OnGoldChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_towerButton == null) return;
        int gold = gameManager.Gold;

        // Башня.
        if (upgrades.CanUpgradeTower)
        {
            int cost = upgrades.TowerUpgradeCost;
            _towerLabel.text = $"Upgrade Tower  Lv {upgrades.TowerLevel} > {upgrades.TowerLevel + 1}\nCost: {cost}";
            _towerButton.interactable = gold >= cost;
        }
        else
        {
            _towerLabel.text = $"Tower Lv {upgrades.TowerLevel}\nMAX";
            _towerButton.interactable = false;
        }

        // Скорострельность.
        if (upgrades.FireRateMaxed)
        {
            _fireRateLabel.text = $"Fire Rate Lv {upgrades.FireRateLevel}\nMAX";
            _fireRateButton.interactable = false;
        }
        else
        {
            _fireRateLabel.text = $"Fire Rate Lv {upgrades.FireRateLevel}\nCost: {upgrades.FireRateCost}";
            _fireRateButton.interactable = gold >= upgrades.FireRateCost;
        }

        // Урон.
        if (upgrades.DamageMaxed)
        {
            _damageLabel.text = $"Damage Lv {upgrades.DamageLevel}\nMAX";
            _damageButton.interactable = false;
        }
        else
        {
            _damageLabel.text = $"Damage Lv {upgrades.DamageLevel}\nCost: {upgrades.DamageCost}";
            _damageButton.interactable = gold >= upgrades.DamageCost;
        }
    }

    private static void Anchor(RectTransform rect, Vector2 corner, Vector2 offset)
    {
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = offset;
    }
}
