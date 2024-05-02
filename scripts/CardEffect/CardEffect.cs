public enum CardEffectType
{
    Attack,
    Defense
}

public abstract class CardEffect
{
    public abstract CardEffectType Type { get; }
    public int Value;
    public abstract void ApplyEffect(Player player, Enemy enemy);
}
