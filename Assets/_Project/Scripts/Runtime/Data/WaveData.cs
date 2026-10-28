using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Данные волны: группы врагов и тайминги спавна.
/// </summary>
[CreateAssetMenu(fileName = "NewWave", menuName = "TowerDefense/Wave Data")]
public class WaveData : ScriptableObject
{
    /// <summary>
    /// Группа врагов одного типа внутри волны.
    /// </summary>
    [System.Serializable]
    public class EnemyGroup
    {
        /// <summary>
        /// Тип врага.
        /// </summary>
        [Tooltip("Тип врага.")]
        [SerializeField] public EnemyData enemyType;

        /// <summary>
        /// Количество таких врагов в волне.
        /// </summary>
        [Tooltip("Количество таких врагов в волне.")]
        [SerializeField] public int count;

        /// <summary>
        /// Задержка перед спавном этого типа (в секундах).
        /// </summary>
        [Tooltip("Задержка перед спавном этого типа (в секундах).")]
        [SerializeField] public float spawnDelay;
    }

    /// <summary>
    /// Список групп врагов для этой волны.
    /// </summary>
    [Tooltip("Список групп врагов для этой волны.")]
    [SerializeField] public List<EnemyGroup> enemies = new List<EnemyGroup>();

    /// <summary>
    /// Базовая задержка между спавнами врагов (в секундах).
    /// </summary>
    [Tooltip("Базовая задержка между спавнами врагов (в секундах).")]
    [SerializeField] public float timeBetweenSpawns;

    /// <summary>
    /// Время отдыха перед следующей волной (в секундах).
    /// </summary>
    [Tooltip("Время отдыха перед следующей волной (в секундах).")]
    [SerializeField] public float timeBeforeNextWave;
}
