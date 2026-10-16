using Godot.Collections;

public partial class ManaOnDiscardEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;
    public override void ApplyEffect(Character character)
    {
        var player = character as Player;
        if (player != null)
        {
            player.AddMana(Value);
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
