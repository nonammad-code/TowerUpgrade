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
