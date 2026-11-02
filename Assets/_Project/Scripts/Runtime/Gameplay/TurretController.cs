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

    [Tooltip("Количество стволов. Каждый выстрел выпускает по одной пуле из каждого ствола.")]
    [SerializeField, Min(1)] private int barrelCount = 1;

    [Tooltip("Расстояние между осями соседних стволов (метры). Для low_poly_turret_002 ≈ 1.3.")]
    [SerializeField] private float barrelSpacing = 1.3f;

    [Tooltip("Допуск попадания пули: расстояние от пули до коллайдера монстра (метры). " +
             "Для многоствольных башен автоматически увеличивается на половину расстояния между стволами.")]
    [SerializeField] private float bulletHitRadius = 0.3f;

    [Tooltip("Если rotatingPart не назначен, голова ищется среди дочерних объектов по этой части имени.")]
    [SerializeField] private string headNameContains = "top";

    /// <summary>
    /// Данные этой башни (уровень, стоимость, снаряд).
    /// </summary>
    public TowerData Data => towerData;

    /// <summary>
    /// Множитель скорострельности от улучшений (1 — без улучшений).
    /// </summary>
    public float FireRateMultiplier { get; set; } = 1f;

    /// <summary>
    /// Множитель урона от улучшений (1 — без улучшений).
    /// </summary>
    public float DamageMultiplier { get; set; } = 1f;

    private Vector3 _barrelLocalDir = Vector3.forward;
    private Vector3[] _muzzleLocals;
    private bool _hasAutoMuzzle;
    private System.Action<Projectile> _releaseProjectile;

    private ObjectPool<Projectile> _pool;
    private EnemyController _target;
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

        if (rotatingPart == null) FindRotatingPart();
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
        Vector3 localDir = Quaternion.Inverse(rotatingPart.rotation) * dir;

        // Ствол почти всегда идёт строго вдоль одной из осей модели. Если направление близко к оси
        // (в пределах ~10°), «прилипаем» к ней: асимметрия габаритов не должна давать лишний перекос.
        localDir = SnapToAxis(localDir, 10f);
        dir = rotatingPart.rotation * localDir;
        dir.y = 0f;
        dir.Normalize();
        _barrelLocalDir = localDir;

        if (!autoDetectMuzzle) return;

        float extentAlong = Mathf.Abs(dir.x) * bounds.extents.x + Mathf.Abs(dir.z) * bounds.extents.z;
        float distance = offset.magnitude + extentAlong;

        Vector3 tip = new Vector3(pivot.x, 0f, pivot.z) + dir * distance;
        tip.y = bounds.min.y + muzzleHeightFraction * bounds.size.y;

        // Стволы расставляются симметрично поперёк направления выстрела.
        int count = Mathf.Max(1, barrelCount);
        Vector3 lateral = Vector3.Cross(Vector3.up, dir).normalized;
        _muzzleLocals = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float shift = (i - (count - 1) * 0.5f) * barrelSpacing;
            _muzzleLocals[i] = rotatingPart.InverseTransformPoint(tip + lateral * shift);
        }
        _hasAutoMuzzle = true;
    }

    private static Vector3 SnapToAxis(Vector3 v, float maxAngle)
    {
        Vector3 axis;
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
        if (ax >= ay && ax >= az) axis = new Vector3(Mathf.Sign(v.x), 0f, 0f);
        else if (ay >= az) axis = new Vector3(0f, Mathf.Sign(v.y), 0f);
        else axis = new Vector3(0f, 0f, Mathf.Sign(v.z));

        return Vector3.Angle(v, axis) <= maxAngle ? axis : v.normalized;
    }

    /// <summary>
    /// Ищет голову турели среди дочерних объектов по имени, если rotatingPart не назначен.
    /// </summary>
    private void FindRotatingPart()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != transform &&
                children[i].name.IndexOf(headNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                rotatingPart = children[i];
                return;
            }
        }
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

        // Пули летят по прямой, поэтому целимся с упреждением — в точку, где окажется монстр.
        Vector3 aimPoint = PredictAimPoint(_target);
        float aimError = AimAt(aimPoint);

        if (_fireCooldown <= 0f && aimError <= aimTolerance)
        {
            Shoot(aimPoint);
            _fireCooldown = 1f / Mathf.Max(0.01f, towerData.fireRate * FireRateMultiplier);
        }
    }

    private EnemyController FindNearestEnemy()
    {
        IReadOnlyList<EnemyController> enemies = EnemyController.ActiveEnemies;
        Vector3 origin = transform.position;

        EnemyController nearest = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            float sqr = (enemies[i].transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = enemies[i];
            }
        }
        return nearest;
    }

    /// <summary>
    /// Точка перехвата: где окажется монстр к моменту прилёта пули (несколько итераций).
    /// </summary>
    private Vector3 PredictAimPoint(EnemyController enemy)
    {
        Vector3 from = GetMuzzleCenter();
        Vector3 position = enemy.AimPoint;
        Vector3 velocity = enemy.Velocity;
        float bulletSpeed = Mathf.Max(0.01f, towerData.projectileData.speed);

        Vector3 aim = position;
        for (int i = 0; i < 3; i++)
        {
            float time = Vector3.Distance(from, aim) / bulletSpeed;
            aim = position + velocity * time;
        }
        return aim;
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

    private int MuzzleCount => _hasAutoMuzzle ? _muzzleLocals.Length : 1;

    private Vector3 GetMuzzlePosition(int index)
    {
        if (_hasAutoMuzzle) return rotatingPart.TransformPoint(_muzzleLocals[index]);
        return firePoint != null ? firePoint.position : transform.position;
    }

    /// <summary>
    /// Середина между всеми стволами (для расчёта направления выстрела).
    /// </summary>
    private Vector3 GetMuzzleCenter()
    {
        int count = MuzzleCount;
        if (count == 1) return GetMuzzlePosition(0);

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < count; i++)
            sum += GetMuzzlePosition(i);
        return sum / count;
    }

    private void Shoot(Vector3 aimPoint)
    {
        // Урон башни за выстрел задан строго и не зависит от числа стволов:
        // он делится между пулями (остаток от деления достаётся первым пулям).
        int count = MuzzleCount;
        int totalDamage = Mathf.Max(1, Mathf.RoundToInt(towerData.damage * DamageMultiplier));
        int perBullet = totalDamage / count;
        int remainder = totalDamage % count;

        // Пули идут параллельно, и крайние стволы смещены от линии прицела на половину расстояния между
        // ними. Допуск попадания должен это покрывать, иначе пули пролетают мимо монстра по бокам.
        float hitRadius = count > 1
            ? Mathf.Max(bulletHitRadius, barrelSpacing * 0.5f + bulletHitRadius)
            : bulletHitRadius;

        // Направление одно на все стволы: пули вылетают из середины своего ствола параллельно друг другу.
        Vector3 direction = aimPoint - GetMuzzleCenter();
        if (direction.sqrMagnitude < 0.0001f) direction = rotatingPart != null
            ? rotatingPart.rotation * _barrelLocalDir
            : transform.forward;

        for (int i = 0; i < count; i++)
        {
            Projectile projectile = _pool.Get();
            projectile.transform.position = GetMuzzlePosition(i);
            int damage = perBullet + (i < remainder ? 1 : 0);
            projectile.Launch(towerData.projectileData, direction, damage, hitRadius, _releaseProjectile);
        }
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
            for (int i = 0; i < MuzzleCount; i++)
                Gizmos.DrawSphere(GetMuzzlePosition(i), 0.05f);
        }
    }
}
