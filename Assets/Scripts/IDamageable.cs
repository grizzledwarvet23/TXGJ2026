//anything that can take damage from a deflected bullet (Enemy, MitochondriaBoss, ...) implements
//this so Bullet.cs doesn't need to know about each concrete type.
public interface IDamageable
{
    void TakeDamage(int amount);
}
