public class DefenseEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Defense;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        // Implement defense effect
        player.AddArmor(Value);
    }
}
