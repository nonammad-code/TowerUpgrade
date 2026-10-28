using UnityEngine;

/// <summary>
/// Данные снаряда (пули): скорость, время жизни, урон и префаб.
/// </summary>
[CreateAssetMenu(fileName = "NewProjectile", menuName = "TowerDefense/Projectile Data")]
public class ProjectileData : ScriptableObject
{
    /// <summary>
    /// Скорость полета пули.
    /// </summary>
    [Tooltip("Скорость полета пули.")]
    [SerializeField] public float speed;

    /// <summary>
    /// Время жизни пули в секундах (если она ни во что не попала).
    /// </summary>
    [Tooltip("Время жизни пули в секундах (если она ни во что не попала).")]
    [SerializeField] public float lifeTime;

    /// <summary>
    /// Базовый урон.
    /// </summary>
    [Tooltip("Базовый урон.")]
    [SerializeField] public int baseDamage;

    /// <summary>
    /// Ссылка на префаб пули (Low_poly_bullet_001).
    /// </summary>
    [Tooltip("Ссылка на префаб пули (Low_poly_bullet_001).")]
    [SerializeField] public GameObject prefab;
}
