using Godot.Collections;

public class AttackEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Attack;

    public override void ApplyEffect(Character enemy)
    {
        for (int i = 0; i < Amount; i++) enemy.TakeDamage(Value);
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        throw new System.NotImplementedException();
    }
}
