public class DefenseEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Defense;
    public int Armor = 1;

    public override void ApplyEffect(Player player, Enemy enemy)
    {
        // Implement defense effect
        player.AddArmor(Armor);
    }
}
