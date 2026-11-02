using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Монстр: двигается по точкам, получает урон, при смерти или достижении базы
/// возвращается в пул и сообщает об этом через статические события.
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    /// <summary>
    /// Вызывается при убийстве монстра. Параметр — награда в золоте.
    /// </summary>
    public static event Action<int> OnEnemyKilled;

    /// <summary>
    /// Вызывается, когда монстр дошёл до базы. Параметр — урон базе.
    /// </summary>
    public static event Action<int> OnEnemyReachedBase;

    private static readonly List<EnemyController> _activeEnemies = new List<EnemyController>(64);

    /// <summary>
    /// Все активные на сцене монстры (для поиска целей турелями без физики и аллокаций).
    /// </summary>
    public static IReadOnlyList<EnemyController> ActiveEnemies => _activeEnemies;

    /// <summary>
    /// Вызывается при изменении здоровья этого монстра. Параметры — текущее и максимальное здоровье.
    /// </summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>
    /// Текущее здоровье.
    /// </summary>
    public int CurrentHealth => _currentHealth;

    /// <summary>
    /// Максимальное здоровье (из EnemyData).
    /// </summary>
    public int MaxHealth => _data != null ? _data.maxHealth : 0;

    [Tooltip("Расстояние до точки, при котором она считается достигнутой.")]
    [SerializeField] private float waypointReachDistance = 0.1f;

    [Tooltip("Поворачивать монстра в сторону движения.")]
    [SerializeField] private bool rotateToMovement = true;

    private Transform _transform;
    private EnemyData _data;
    private Transform[] _waypoints;
    private Action<EnemyController> _release;
    private int _currentHealth;
    private int _waypointIndex;
    private bool _active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _activeEnemies.Clear();
        OnEnemyKilled = null;
        OnEnemyReachedBase = null;
    }

    private void Awake()
    {
        _transform = transform;
    }

    private void OnEnable()
    {
        _activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        _activeEnemies.Remove(this);
    }

    /// <summary>
    /// Инициализирует монстра после получения из пула (вызывает спавнер).
    /// </summary>
    /// <param name="data">Данные монстра.</param>
    /// <param name="waypoints">Точки маршрута.</param>
    /// <param name="release">Колбэк возврата в пул.</param>
    public void Init(EnemyData data, Transform[] waypoints, Action<EnemyController> release)
    {
        if (data == null || waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{name}: не заданы EnemyData или Waypoints.", this);
            release?.Invoke(this);
            return;
        }

        _data = data;
        _waypoints = waypoints;
        _release = release;
        _currentHealth = data.maxHealth;
        _waypointIndex = 0;
        _transform.position = waypoints[0].position;
        _active = true;
    }

    private void Update()
    {
        if (!_active) return;

        Vector3 position = _transform.position;
        Vector3 targetPos = _waypoints[_waypointIndex].position;
        Vector3 toTarget = targetPos - position;
        float step = _data.moveSpeed * Time.deltaTime;

        if (toTarget.magnitude <= Mathf.Max(step, waypointReachDistance))
        {
            _transform.position = targetPos;
            _waypointIndex++;

            if (_waypointIndex >= _waypoints.Length)
            {
                ReachBase();
            }
            return;
        }

        Vector3 direction = toTarget.normalized;
        _transform.position = position + direction * step;

        if (rotateToMovement)
            _transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <inheritdoc />
    public void TakeDamage(int amount)
    {
        if (!_active) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, _data.maxHealth);

        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        int reward = _data.rewardGold;
        Deactivate();
        OnEnemyKilled?.Invoke(reward);
    }

    private void ReachBase()
    {
        int damage = _data.damageToBase;
        Deactivate();
        OnEnemyReachedBase?.Invoke(damage);
    }

    private void Deactivate()
    {
        _active = false;
        _release?.Invoke(this);
    }
}
