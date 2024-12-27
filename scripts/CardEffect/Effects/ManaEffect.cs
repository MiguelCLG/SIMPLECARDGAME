using Godot.Collections;

public class ManaEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;

    public override void ApplyEffect(Character player)
    {
        player.AddMana(Value);
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        throw new System.NotImplementedException();
    }
}
