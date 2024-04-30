public class AttackEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Attack;
    public int Damage = 10;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        enemy.TakeDamage(Damage);
    }
}
