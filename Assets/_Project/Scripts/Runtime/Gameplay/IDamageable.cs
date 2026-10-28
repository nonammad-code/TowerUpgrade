/// <summary>
/// Объект, которому можно нанести урон (враг, база и т.д.).
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Наносит урон объекту.
    /// </summary>
    void TakeDamage(int amount);
}
