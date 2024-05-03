public class ManaEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        player.AddMana(Value);
    }
}
