using UnityEngine;

/// <summary>
/// Данные врага (монстра): здоровье, скорость, урон базе, награда и префаб.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "TowerDefense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    /// <summary>
    /// Максимальное здоровье.
    /// </summary>
    [Tooltip("Максимальное здоровье.")]
    [SerializeField] public int maxHealth;

    /// <summary>
    /// Скорость передвижения.
    /// </summary>
    [Tooltip("Скорость передвижения.")]
    [SerializeField] public float moveSpeed;

    /// <summary>
    /// Урон, который наносится базе игрока, если монстр дошел до конца.
    /// </summary>
    [Tooltip("Урон, который наносится базе игрока, если монстр дошел до конца.")]
    [SerializeField] public int damageToBase;

    /// <summary>
    /// Количество золота за убийство.
    /// </summary>
    [Tooltip("Количество золота за убийство.")]
    [SerializeField] public int rewardGold;

    /// <summary>
    /// Ссылка на префаб монстра (low_poly_monster_001).
    /// </summary>
    [Tooltip("Ссылка на префаб монстра (low_poly_monster_001).")]
    [SerializeField] public GameObject prefab;
}
