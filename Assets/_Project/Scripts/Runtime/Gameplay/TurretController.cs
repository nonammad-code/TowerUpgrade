using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Управляет турелью: берёт параметры из TowerData, ищет ближайшего врага на всей локации
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

    [Tooltip("Скорость поворота к цели (градусов в секунду).")]
    [SerializeField] private float rotationSpeed = 360f;

    [Tooltip("Стрелять только когда голова повернута к цели с точностью до этого угла (градусы).")]
    [SerializeField] private float aimTolerance = 8f;

    [Header("Muzzle (ствол)")]
    [Tooltip("Автоматически определить направление и конец ствола по размерам головы. " +
             "Точка вылета пуль будет следовать за поворотом головы. Выключите, чтобы использовать firePoint.")]
    [SerializeField] private bool autoDetectMuzzle = true;

    [Tooltip("Высота ствола в долях высоты головы (0 — низ, 1 — верх). Для low_poly_turret_top_001 ≈ 0.4.")]
    [SerializeField, Range(0f, 1f)] private float muzzleHeightFraction = 0.4f;

    private Vector3 _barrelLocalDir = Vector3.forward;
    private Vector3 _muzzleLocal;
    private bool _hasAutoMuzzle;
    private System.Action<Projectile> _releaseProjectile;

    private ObjectPool<Projectile> _pool;
    private Transform _target;
    private float _fireCooldown;
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
        _releaseProjectile = _pool.Release;

        SetupBarrel();
    }

    /// <summary>
    /// Определяет, в какую сторону смотрит ствол и где его конец. Выполняется один раз в Awake.
    /// Ствол — это выступающая в одну сторону часть головы, поэтому направление ствола —
    /// от оси вращения (позиция головы) к центру габаритов головы, а конец — край габаритов.
    /// </summary>
    private void SetupBarrel()
    {
        if (rotatingPart == null) return;

        Renderer[] renderers = rotatingPart.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 pivot = rotatingPart.position;
        Vector3 offset = bounds.center - pivot;
        offset.y = 0f;

        if (offset.magnitude < 0.01f)
        {
            Debug.LogWarning($"{name}: не удалось определить направление ствола (голова симметрична). " +
                             "Используется forward головы.", this);
            _barrelLocalDir = Quaternion.Inverse(rotatingPart.rotation) * Vector3.ProjectOnPlane(rotatingPart.forward, Vector3.up).normalized;
            return;
        }

        Vector3 dir = offset.normalized;
        _barrelLocalDir = Quaternion.Inverse(rotatingPart.rotation) * dir;

        if (!autoDetectMuzzle) return;

        float extentAlong = Mathf.Abs(dir.x) * bounds.extents.x + Mathf.Abs(dir.z) * bounds.extents.z;
        float distance = offset.magnitude + extentAlong;

        Vector3 tip = new Vector3(pivot.x, 0f, pivot.z) + dir * distance;
        tip.y = bounds.min.y + muzzleHeightFraction * bounds.size.y;

        _muzzleLocal = rotatingPart.InverseTransformPoint(tip);
        _hasAutoMuzzle = true;
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
    }

    private void Update()
    {
        _fireCooldown -= Time.deltaTime;

        // Цель выбирается каждый кадр: это всегда ближайший враг на всей локации.
        // Если текущая цель погибла или появился враг ближе — цель меняется сразу.
        _target = FindNearestEnemy();
        if (_target == null) return;

        float aimError = AimAt(_target.position);

        if (_fireCooldown <= 0f && aimError <= aimTolerance)
        {
            Shoot();
            _fireCooldown = 1f / Mathf.Max(0.01f, towerData.fireRate);
        }
    }

    private Transform FindNearestEnemy()
    {
        IReadOnlyList<EnemyController> enemies = EnemyController.ActiveEnemies;
        Vector3 origin = transform.position;

        Transform nearest = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            Transform t = enemies[i].transform;
            float sqr = (t.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = t;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Поворачивает голову вокруг вертикальной оси так, чтобы ствол смотрел на точку.
    /// Исходный наклон головы из модели сохраняется (вращение только по Y в мировом пространстве).
    /// </summary>
    /// <returns>Оставшийся угол до цели в градусах (0 — наведено).</returns>
    private float AimAt(Vector3 point)
    {
        if (rotatingPart == null) return 0f;

        Vector3 desired = point - rotatingPart.position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return 0f;

        Vector3 current = rotatingPart.rotation * _barrelLocalDir;
        current.y = 0f;

        float angle = Vector3.SignedAngle(current, desired, Vector3.up);
        float maxStep = rotationSpeed * Time.deltaTime;
        float step = Mathf.Clamp(angle, -maxStep, maxStep);

        rotatingPart.Rotate(Vector3.up, step, Space.World);
        return Mathf.Abs(angle - step);
    }

    private Vector3 GetMuzzlePosition()
    {
        if (_hasAutoMuzzle) return rotatingPart.TransformPoint(_muzzleLocal);
        return firePoint != null ? firePoint.position : transform.position;
    }

    private void Shoot()
    {
        Projectile projectile = _pool.Get();
        Vector3 muzzle = GetMuzzlePosition();
        projectile.transform.SetPositionAndRotation(muzzle, Quaternion.identity);
        projectile.Launch(towerData.projectileData, _target, _releaseProjectile);
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

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(GetMuzzlePosition(), 0.05f);
        }
    }
}
