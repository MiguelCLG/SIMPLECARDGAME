public class DrawEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Attack;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        for (int i = 0; i < Value; i++)
        {
            player.DrawCard();
        }
    }
}
