using Godot.Collections;

public partial class NoEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;
    public override void ApplyEffect(Character character) { }
    public override void ApplyEffect(Array<Character> characters) { }
}
