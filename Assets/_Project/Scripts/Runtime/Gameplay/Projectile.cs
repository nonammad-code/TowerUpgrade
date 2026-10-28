using System;
using UnityEngine;

/// <summary>
/// Пуля: летит к цели, наносит урон при попадании и возвращается в пул.
/// Если цель пропала, летит прямо до конца времени жизни.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Расстояние до цели, при котором считается попадание.")]
    [SerializeField] private float hitRadius = 0.3f;

    private ProjectileData _data;
    private Transform _target;
    private Action<Projectile> _release;
    private Vector3 _direction;
    private float _lifeLeft;
    private bool _active;

    /// <summary>
    /// Запускает пулю. Вызывается турелью после получения из пула.
    /// </summary>
    public void Launch(ProjectileData data, Transform target, Action<Projectile> release)
    {
        _data = data;
        _target = target;
        _release = release;
        _lifeLeft = data.lifeTime;
        _direction = (target.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(_direction);
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

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = _target.position - transform.position;
            if (toTarget.magnitude <= hitRadius)
            {
                Hit();
                return;
            }
            _direction = toTarget.normalized;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        transform.position += _direction * (_data.speed * Time.deltaTime);
    }

    private void Hit()
    {
        if (_target.TryGetComponent(out IDamageable damageable))
            damageable.TakeDamage(_data.baseDamage);
        Release();
    }

    private void Release()
    {
        if (!_active) return;
        _active = false;
        _target = null;
        _release(this);
    }
}
