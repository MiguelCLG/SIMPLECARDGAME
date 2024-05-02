public class AttackEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Attack;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        enemy.TakeDamage(Value);
    }
}
