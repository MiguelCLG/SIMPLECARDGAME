using Godot.Collections;

public class DrawEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;

    public override void ApplyEffect(Character player)
    {
        for (int i = 0; i < Amount; i++)
        {
            player.DrawCard();
        }
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        throw new System.NotImplementedException();
    }
}
