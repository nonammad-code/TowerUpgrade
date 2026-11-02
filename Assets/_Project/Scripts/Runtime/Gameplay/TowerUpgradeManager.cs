using System;
using UnityEngine;

/// <summary>
/// Улучшения единственной башни: переход на следующий уровень (замена башни с полным
/// восстановлением здоровья базы) и улучшение характеристик (скорострельность, урон).
/// ScriptableObject-данные не изменяются: улучшения хранятся здесь и применяются множителями.
/// </summary>
public class TowerUpgradeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("Башня в сцене. Если не задана — ищется в сцене.")]
    [SerializeField] private TurretController tower;

    [Header("Fire rate upgrade")]
    [SerializeField] private int fireRateBaseCost = 50;
    [SerializeField] private int fireRateCostStep = 25;
    [Tooltip("Прирост скорострельности за уровень (0.15 = +15%).")]
    [SerializeField] private float fireRateBonusPerLevel = 0.15f;
    [SerializeField] private int fireRateMaxLevel = 5;

    [Header("Damage upgrade")]
    [SerializeField] private int damageBaseCost = 50;
    [SerializeField] private int damageCostStep = 25;
    [Tooltip("Прирост урона за уровень (0.2 = +20%).")]
    [SerializeField] private float damageBonusPerLevel = 0.2f;
    [SerializeField] private int damageMaxLevel = 5;

    /// <summary>Текущая башня.</summary>
    public TurretController Tower => tower;

    /// <summary>Уровень башни (1 — стартовая).</summary>
    public int TowerLevel { get; private set; } = 1;

    /// <summary>Уровень улучшения скорострельности (0 — не улучшалась).</summary>
    public int FireRateLevel { get; private set; }

    /// <summary>Уровень улучшения урона (0 — не улучшался).</summary>
    public int DamageLevel { get; private set; }

    /// <summary>Можно ли перейти на следующий уровень башни.</summary>
    public bool CanUpgradeTower => tower != null && tower.Data != null && tower.Data.nextLevel != null;

    /// <summary>Стоимость перехода на следующий уровень.</summary>
    public int TowerUpgradeCost => tower != null && tower.Data != null ? tower.Data.upgradeCost : 0;

    /// <summary>Максимум достигнут для скорострельности.</summary>
    public bool FireRateMaxed => FireRateLevel >= fireRateMaxLevel;

    /// <summary>Максимум достигнут для урона.</summary>
    public bool DamageMaxed => DamageLevel >= damageMaxLevel;

    /// <summary>Стоимость следующего улучшения скорострельности.</summary>
    public int FireRateCost => fireRateBaseCost + fireRateCostStep * FireRateLevel;

    /// <summary>Стоимость следующего улучшения урона.</summary>
    public int DamageCost => damageBaseCost + damageCostStep * DamageLevel;

    /// <summary>Что-то из улучшений изменилось (для обновления UI).</summary>
    public event Action OnUpgradesChanged;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (tower == null) tower = FindFirstObjectByType<TurretController>();

        if (gameManager == null || tower == null)
        {
            Debug.LogError($"{name}: нужны GameManager и TurretController в сцене.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        ApplyMultipliers();
        OnUpgradesChanged?.Invoke();
    }

    /// <summary>
    /// Заменяет башню на башню следующего уровня и полностью восстанавливает здоровье базы.
    /// </summary>
    public bool TryUpgradeTower()
    {
        if (!CanUpgradeTower || (gameManager.IsGameOver || gameManager.IsVictory)) return false;

        TowerData next = tower.Data.nextLevel;
        if (next.towerPrefab == null || !next.towerPrefab.TryGetComponent(out TurretController _))
        {
            Debug.LogError($"{next.name}: у towerPrefab нет TurretController.", next);
            return false;
        }

        if (!gameManager.TrySpendGold(tower.Data.upgradeCost)) return false;

        Transform old = tower.transform;
        GameObject newGo = Instantiate(next.towerPrefab, old.position, old.rotation, old.parent);
        TurretController newTower = newGo.GetComponent<TurretController>();

        Destroy(old.gameObject);
        tower = newTower;
        TowerLevel++;

        ApplyMultipliers();
        gameManager.RestoreBaseHealth();
        OnUpgradesChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Улучшает скорострельность башни.
    /// </summary>
    public bool TryUpgradeFireRate()
    {
        if (FireRateMaxed || (gameManager.IsGameOver || gameManager.IsVictory)) return false;
        if (!gameManager.TrySpendGold(FireRateCost)) return false;

        FireRateLevel++;
        ApplyMultipliers();
        OnUpgradesChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Улучшает урон башни.
    /// </summary>
    public bool TryUpgradeDamage()
    {
        if (DamageMaxed || (gameManager.IsGameOver || gameManager.IsVictory)) return false;
        if (!gameManager.TrySpendGold(DamageCost)) return false;

        DamageLevel++;
        ApplyMultipliers();
        OnUpgradesChanged?.Invoke();
        return true;
    }

    private void ApplyMultipliers()
    {
        tower.FireRateMultiplier = 1f + fireRateBonusPerLevel * FireRateLevel;
        tower.DamageMultiplier = 1f + damageBonusPerLevel * DamageLevel;
    }
}
