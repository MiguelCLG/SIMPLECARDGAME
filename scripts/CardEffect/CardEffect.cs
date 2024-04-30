public enum CardEffectType
{
    Attack,
    Defense
}

public abstract class CardEffect
{
    public abstract CardEffectType Type { get; }
    public abstract void ApplyEffect(Player player, Enemy enemy);
}
