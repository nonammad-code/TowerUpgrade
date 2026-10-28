using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Спавнит монстров по данным WaveData. Пул ObjectPool&lt;EnemyController&gt;
/// создаётся автоматически для каждого EnemyData по префабу из его поля prefab.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>
    /// Пул одного типа врага вместе с закэшированным делегатом возврата (без аллокаций при спавне).
    /// </summary>
    private class EnemyPool
    {
        public ObjectPool<EnemyController> Pool;
        public Action<EnemyController> Release;
    }

    [Header("Waves")]
    [Tooltip("Волны по порядку. Каждая следующая стартует через timeBeforeNextWave после предыдущей.")]
    [SerializeField] private WaveData[] waves;

    [Tooltip("Запускать волны автоматически при старте сцены.")]
    [SerializeField] private bool autoStart = true;

    [Header("Path")]
    [Tooltip("Точки маршрута, по которым пойдут монстры.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Родитель для создаваемых монстров (необязательно).")]
    [SerializeField] private Transform enemiesParent;

    private readonly Dictionary<EnemyData, EnemyPool> _pools = new Dictionary<EnemyData, EnemyPool>();
    private Coroutine _routine;

    /// <summary>
    /// Номер текущей волны (с 0). -1, если волны не запущены.
    /// </summary>
    public int CurrentWaveIndex { get; private set; } = -1;

    /// <summary>
    /// Вызывается при старте волны. Параметр — индекс волны.
    /// </summary>
    public event Action<int> OnWaveStarted;

    /// <summary>
    /// Вызывается, когда все волны полностью отспавнены.
    /// </summary>
    public event Action OnAllWavesSpawned;

    private void Start()
    {
        if (autoStart) StartWaves();
    }

    private void OnDestroy()
    {
        foreach (EnemyPool entry in _pools.Values)
            entry.Pool.Dispose();
        _pools.Clear();
    }

    /// <summary>
    /// Запускает спавн всех волн с начала.
    /// </summary>
    public void StartWaves()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{name}: не заданы waypoints.", this);
            return;
        }
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError($"{name}: не заданы волны.", this);
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        for (int w = 0; w < waves.Length; w++)
        {
            WaveData wave = waves[w];
            if (wave == null) continue;

            CurrentWaveIndex = w;
            OnWaveStarted?.Invoke(w);

            List<WaveData.EnemyGroup> groups = wave.enemies;
            for (int g = 0; g < groups.Count; g++)
            {
                WaveData.EnemyGroup group = groups[g];
                if (group == null || group.enemyType == null) continue;

                yield return Wait(group.spawnDelay);

                for (int i = 0; i < group.count; i++)
                {
                    Spawn(group.enemyType);
                    if (i < group.count - 1)
                        yield return Wait(wave.timeBetweenSpawns);
                }

                if (g < groups.Count - 1)
                    yield return Wait(wave.timeBetweenSpawns);
            }

            if (w < waves.Length - 1)
                yield return Wait(wave.timeBeforeNextWave);
        }

        _routine = null;
        OnAllWavesSpawned?.Invoke();
    }

    /// <summary>
    /// Ожидание без аллокаций (вместо new WaitForSeconds).
    /// </summary>
    private static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
            yield return null;
    }

    private void Spawn(EnemyData data)
    {
        EnemyPool entry = GetOrCreatePool(data);
        if (entry == null) return;

        EnemyController enemy = entry.Pool.Get();
        enemy.Init(data, waypoints, entry.Release);
    }

    private EnemyPool GetOrCreatePool(EnemyData data)
    {
        if (_pools.TryGetValue(data, out EnemyPool existing))
            return existing;

        if (data.prefab == null)
        {
            Debug.LogError($"{data.name}: не задан prefab.", data);
            return null;
        }

        var entry = new EnemyPool();
        entry.Pool = new ObjectPool<EnemyController>(
            createFunc: () => CreateEnemy(data),
            actionOnGet: e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 200);
        entry.Release = entry.Pool.Release;

        _pools.Add(data, entry);
        return entry;
    }

    private EnemyController CreateEnemy(EnemyData data)
    {
        GameObject go = Instantiate(data.prefab, enemiesParent);
        if (!go.TryGetComponent(out EnemyController enemy))
            enemy = go.AddComponent<EnemyController>();
        return enemy;
    }
}
