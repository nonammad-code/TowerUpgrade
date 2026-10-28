using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Управляет турелью: берёт параметры из TowerData, ищет ближайшего врага в радиусе
/// и стреляет пулями из Object Pool с частотой fireRate.
/// </summary>
public class TurretController : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Данные башни (радиус, скорострельность, снаряд).")]
    [SerializeField] private TowerData towerData;

    [Header("Setup")]
    [Tooltip("Точка, из которой вылетают пули. Если не задана — позиция турели.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Часть турели, которая поворачивается к цели (необязательно).")]
    [SerializeField] private Transform rotatingPart;

    [Tooltip("Слой, на котором находятся враги.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Как часто (сек) искать ближайшего врага.")]
    [SerializeField] private float retargetInterval = 0.2f;

    [Tooltip("Скорость поворота к цели (градусов в секунду).")]
    [SerializeField] private float rotationSpeed = 360f;

    private const int MaxColliders = 32;

    private readonly Collider[] _hits = new Collider[MaxColliders];
    private ObjectPool<Projectile> _pool;
    private Transform _target;
    private float _fireCooldown;
    private float _retargetTimer;

    private void Awake()
    {
        if (towerData == null || towerData.projectileData == null || towerData.projectileData.prefab == null)
        {
            Debug.LogError($"{name}: не заданы TowerData / ProjectileData / prefab пули.", this);
            enabled = false;
            return;
        }

        _pool = new ObjectPool<Projectile>(
            createFunc: CreateProjectile,
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 100);
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
    }

    private void Update()
    {
        _fireCooldown -= Time.deltaTime;
        _retargetTimer -= Time.deltaTime;

        if (_retargetTimer <= 0f || !IsTargetValid(_target))
        {
            _target = FindNearestEnemy();
            _retargetTimer = retargetInterval;
        }

        if (_target == null) return;

        AimAt(_target.position);

        if (_fireCooldown <= 0f)
        {
            Shoot();
            _fireCooldown = 1f / Mathf.Max(0.01f, towerData.fireRate);
        }
    }

    private Transform FindNearestEnemy()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, towerData.range, _hits, enemyLayer);

        Transform nearest = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            float sqr = (_hits[i].transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = _hits[i].transform;
            }
        }
        return nearest;
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        float rangeSqr = towerData.range * towerData.range;
        return (target.position - transform.position).sqrMagnitude <= rangeSqr;
    }

    private void AimAt(Vector3 point)
    {
        if (rotatingPart == null) return;

        Vector3 dir = point - rotatingPart.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        rotatingPart.rotation = Quaternion.RotateTowards(
            rotatingPart.rotation, look, rotationSpeed * Time.deltaTime);
    }

    private void Shoot()
    {
        Projectile projectile = _pool.Get();
        Transform origin = firePoint != null ? firePoint : transform;
        projectile.transform.SetPositionAndRotation(origin.position, origin.rotation);
        projectile.Launch(towerData.projectileData, _target, p => _pool.Release(p));
    }

    private Projectile CreateProjectile()
    {
        GameObject go = Instantiate(towerData.projectileData.prefab);
        if (!go.TryGetComponent(out Projectile projectile))
            projectile = go.AddComponent<Projectile>();
        return projectile;
    }

    private void OnDrawGizmosSelected()
    {
        if (towerData == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, towerData.range);
    }
}
