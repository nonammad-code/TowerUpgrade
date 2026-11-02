using System;
using UnityEngine;

/// <summary>
/// Пуля: летит по прямой в заданном направлении (без самонаведения), поэтому пули из
/// разных стволов остаются параллельными. При попадании в монстра наносит урон и
/// возвращается в пул; если ни во что не попала — исчезает по истечении времени жизни.
/// </summary>
public class Projectile : MonoBehaviour
{
    private float _hitRadius;
    private ProjectileData _data;
    private Action<Projectile> _release;
    private Transform _transform;
    private Vector3 _direction;
    private int _damage;
    private float _lifeLeft;
    private bool _active;

    private void Awake()
    {
        _transform = transform;
    }

    /// <summary>
    /// Запускает пулю в заданном направлении. Вызывается турелью после получения из пула.
    /// </summary>
    /// <param name="hitRadius">Допуск попадания: расстояние от пули до области попадания монстра (метры).</param>
    public void Launch(ProjectileData data, Vector3 direction, int damage, float hitRadius, Action<Projectile> release)
    {
        _data = data;
        _hitRadius = hitRadius;
        _damage = damage;
        _release = release;
        _lifeLeft = data.lifeTime;
        _direction = direction.normalized;
        _transform.rotation = Quaternion.LookRotation(_direction);
        _active = true;
    }

    private void Update()
    {
        if (!_active) return;

        _lifeLeft -= Time.deltaTime;
        if (_lifeLeft <= 0f)
        {
            Release();
            return;
        }

        Vector3 from = _transform.position;
        Vector3 to = from + _direction * (_data.speed * Time.deltaTime);

        if (TryHitEnemy(from, to))
        {
            Release();
            return;
        }

        _transform.position = to;
    }

    /// <summary>
    /// Проверяет пересечение отрезка движения пули за кадр с областью попадания любого монстра.
    /// Проверка по отрезку не даёт быстрой пуле «проскочить» сквозь монстра.
    /// </summary>
    private bool TryHitEnemy(Vector3 from, Vector3 to)
    {
        var enemies = EnemyController.ActiveEnemies;
        Vector3 segment = to - from;
        float segmentSqr = segment.sqrMagnitude;
        float radiusSqr = _hitRadius * _hitRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            Bounds bounds = enemies[i].HitBounds;

            float t = segmentSqr > 0f
                ? Mathf.Clamp01(Vector3.Dot(bounds.center - from, segment) / segmentSqr)
                : 0f;
            Vector3 closest = from + segment * t;

            if (bounds.SqrDistance(closest) <= radiusSqr)
            {
                enemies[i].TakeDamage(_damage);
                return true;
            }
        }
        return false;
    }

    private void Release()
    {
        if (!_active) return;
        _active = false;
        _release(this);
    }
}
