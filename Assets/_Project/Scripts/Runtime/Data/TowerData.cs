using UnityEngine;

/// <summary>
/// Данные башни: радиус атаки, скорострельность, стоимость, снаряд и префаб.
/// </summary>
[CreateAssetMenu(fileName = "NewTower", menuName = "TowerDefense/Tower Data")]
public class TowerData : ScriptableObject
{
    /// <summary>
    /// Радиус атаки.
    /// </summary>
    [Tooltip("Радиус атаки.")]
    [SerializeField] public float range;

    /// <summary>
    /// Скорострельность (выстрелов в секунду).
    /// </summary>
    [Tooltip("Скорострельность (выстрелов в секунду).")]
    [SerializeField] public float fireRate;

    /// <summary>
    /// Базовый урон одной пули этой башни (улучшается вместе с башней).
    /// </summary>
    [Tooltip("Базовый урон одной пули этой башни.")]
    [SerializeField] public int damage = 10;

    /// <summary>
    /// Стоимость постройки башни.
    /// </summary>
    [Tooltip("Стоимость постройки башни.")]
    [SerializeField] public int cost;

    /// <summary>
    /// Стоимость улучшения на следующий уровень.
    /// </summary>
    [Tooltip("Стоимость улучшения на следующий уровень.")]
    [SerializeField] public int upgradeCost;

    /// <summary>
    /// Данные башни следующего уровня. Если не задано — это максимальный уровень.
    /// Префаб следующего уровня (towerPrefab) должен содержать TurretController с собственным TowerData.
    /// </summary>
    [Tooltip("Данные башни следующего уровня. Пусто — максимальный уровень.")]
    [SerializeField] public TowerData nextLevel;

    /// <summary>
    /// Ссылка на ScriptableObject пули, которой стреляет эта башня.
    /// </summary>
    [Tooltip("Ссылка на ScriptableObject пули, которой стреляет эта башня.")]
    [SerializeField] public ProjectileData projectileData;

    /// <summary>
    /// Ссылка на префаб самой турели (Low_poly_turret_001).
    /// </summary>
    [Tooltip("Ссылка на префаб самой турели (Low_poly_turret_001).")]
    [SerializeField] public GameObject towerPrefab;
}
