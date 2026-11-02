using UnityEngine;

/// <summary>
/// Полоска здоровья над монстром (World Space канвас, всегда повёрнута к камере).
/// Создаётся один раз вместе с объектом монстра и переиспользуется через пул.
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Ширина полоски в метрах.")]
    [SerializeField] private float width = 1f;

    [Tooltip("Высота полоски в метрах.")]
    [SerializeField] private float height = 0.12f;

    [Tooltip("Отступ полоски над верхней точкой модели (метры).")]
    [SerializeField] private float heightOffset = 2.5f;

    private const float PixelsPerMeter = 100f;

    private EnemyController _enemy;
    private Transform _enemyTransform;
    private Transform _barTransform;
    private Transform _camera;
    private UIBar _bar;
    private float _barHeight;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        _enemyTransform = transform;
        _barHeight = CalculateModelHeight() + heightOffset;
        BuildBar();
    }

    private void OnEnable()
    {
        if (_camera == null && Camera.main != null)
            _camera = Camera.main.transform;

        _enemy.OnHealthChanged += SetHealth;
        SetHealth(_enemy.CurrentHealth, _enemy.MaxHealth);
    }

    private void OnDisable()
    {
        _enemy.OnHealthChanged -= SetHealth;
    }

    private void LateUpdate()
    {
        _barTransform.position = _enemyTransform.position + Vector3.up * _barHeight;
        if (_camera != null)
            _barTransform.rotation = _camera.rotation;
    }

    private void SetHealth(int current, int max)
    {
        float t = max > 0 ? (float)current / max : 0f;
        _bar.SetValue(t);
        _bar.SetFillColor(Color.Lerp(Color.red, Color.green, t));
    }

    private float CalculateModelHeight()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 1f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(0.1f, bounds.max.y - _enemyTransform.position.y);
    }

    private void BuildBar()
    {
        var canvasGo = new GameObject("HealthBar Canvas", typeof(Canvas));
        _barTransform = canvasGo.transform;
        _barTransform.SetParent(_enemyTransform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width * PixelsPerMeter, height * PixelsPerMeter);

        // Компенсируем масштаб монстра, чтобы размер полоски был в метрах.
        float parentScale = Mathf.Max(0.0001f, _enemyTransform.lossyScale.x);
        _barTransform.localScale = Vector3.one * (1f / PixelsPerMeter / parentScale);

        _bar = UIBar.Create(_barTransform, "Bar", rect.sizeDelta,
            new Color(0f, 0f, 0f, 0.7f), Color.green);
        _bar.Root.anchorMin = _bar.Root.anchorMax = new Vector2(0.5f, 0.5f);
        _bar.Root.anchoredPosition = Vector2.zero;
    }
}
