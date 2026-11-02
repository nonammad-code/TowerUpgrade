using System;
using UnityEngine;

/// <summary>
/// Состояние игры: золото и здоровье базы. Слушает события EnemyController.
/// </summary>
[DefaultExecutionOrder(-100)] // Awake раньше HUD и остальных, чтобы значения были готовы к первому чтению.
public class GameManager : MonoBehaviour
{
    [Tooltip("Стартовое золото.")]
    [SerializeField] private int startingGold = 100;

    [Tooltip("Максимальное здоровье базы.")]
    [SerializeField] private int maxBaseHealth = 20;

    /// <summary>Текущее золото.</summary>
    public int Gold { get; private set; }

    /// <summary>Текущее здоровье базы.</summary>
    public int BaseHealth { get; private set; }

    /// <summary>Максимальное здоровье базы.</summary>
    public int MaxBaseHealth => maxBaseHealth;

    /// <summary>Игра окончена (здоровье базы закончилось).</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>Золото изменилось. Параметр — новое значение.</summary>
    public event Action<int> OnGoldChanged;

    /// <summary>Здоровье базы изменилось. Параметры — текущее и максимальное.</summary>
    public event Action<int, int> OnBaseHealthChanged;

    /// <summary>База уничтожена.</summary>
    public event Action OnGameOver;

    /// <summary>Победа: все волны отспавнены и все монстры уничтожены.</summary>
    public bool IsVictory { get; private set; }

    /// <summary>Игрок победил.</summary>
    public event Action OnVictory;

    private EnemySpawner _spawner;
    private bool _allWavesSpawned;

    private void Awake()
    {
        Time.timeScale = 1f;
        Gold = startingGold;
        BaseHealth = maxBaseHealth;
        _spawner = FindFirstObjectByType<EnemySpawner>();
    }

    private void OnEnable()
    {
        EnemyController.OnEnemyKilled += HandleEnemyKilled;
        EnemyController.OnEnemyReachedBase += HandleEnemyReachedBase;
        if (_spawner != null) _spawner.OnAllWavesSpawned += HandleAllWavesSpawned;
    }

    private void OnDisable()
    {
        EnemyController.OnEnemyKilled -= HandleEnemyKilled;
        EnemyController.OnEnemyReachedBase -= HandleEnemyReachedBase;
        if (_spawner != null) _spawner.OnAllWavesSpawned -= HandleAllWavesSpawned;
    }

    private void HandleEnemyKilled(int reward)
    {
        AddGold(reward);
        CheckVictory();
    }

    private void HandleEnemyReachedBase(int damage)
    {
        DamageBase(damage);
        CheckVictory();
    }

    private void HandleAllWavesSpawned()
    {
        _allWavesSpawned = true;
        CheckVictory();
    }

    /// <summary>
    /// Победа наступает, когда последняя волна отспавнена и на сцене не осталось монстров.
    /// К моменту вызова погибший монстр уже убран из ActiveEnemies.
    /// </summary>
    private void CheckVictory()
    {
        if (IsVictory || IsGameOver || !_allWavesSpawned) return;
        if (EnemyController.ActiveEnemies.Count > 0) return;

        IsVictory = true;
        Time.timeScale = 0f;
        OnVictory?.Invoke();
    }

    /// <summary>
    /// Начисляет золото.
    /// </summary>
    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    /// <summary>
    /// Пытается потратить золото (для покупки и улучшения башен).
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    /// <summary>
    /// Полностью восстанавливает здоровье базы (например, после улучшения башни).
    /// </summary>
    public void RestoreBaseHealth()
    {
        if (IsGameOver) return;
        BaseHealth = maxBaseHealth;
        OnBaseHealthChanged?.Invoke(BaseHealth, maxBaseHealth);
    }

    /// <summary>
    /// Наносит урон базе.
    /// </summary>
    public void DamageBase(int amount)
    {
        if (IsGameOver) return;

        BaseHealth = Mathf.Max(0, BaseHealth - amount);
        OnBaseHealthChanged?.Invoke(BaseHealth, maxBaseHealth);

        if (BaseHealth <= 0)
        {
            IsGameOver = true;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }
    }
}
