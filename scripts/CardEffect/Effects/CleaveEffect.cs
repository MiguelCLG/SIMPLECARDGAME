
using Godot.Collections;

public class CleaveEffect : CardEffect
{
  public override CardEffectType Type => CardEffectType.Attack;

  public override void ApplyEffect(Array<Character> enemies)
  {
    foreach (Enemy enemy in enemies)
      for (int i = 0; i < Amount; i++) enemy.TakeDamage(Value);
  }

  public override void ApplyEffect(Character enemy)
  {
  }
}
