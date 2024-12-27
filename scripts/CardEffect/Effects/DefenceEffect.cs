using Godot.Collections;

public class DefenseEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Defense;

    public override void ApplyEffect(Character player)
    {
        // Implement defense effect
        player.AddArmor(Value);
    }

    public override void ApplyEffect(Array<Character> enemies)
    {
        throw new System.NotImplementedException();
    }
}
