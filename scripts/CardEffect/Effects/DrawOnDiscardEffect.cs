using Godot.Collections;

public partial class DrawOnDiscardEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;
    public override void ApplyEffect(Character character)
    {
        var player = character as Player;
        if (player != null)
        {
            for (int i = 0; i < Amount; i++)
            {
                player.DrawCard();
            }
        }
    }
    public override void ApplyEffect(Array<Character> characters)
    {
        if (characters.Count > 0)
        {
            ApplyEffect(characters[0]);
        }
    }
}
