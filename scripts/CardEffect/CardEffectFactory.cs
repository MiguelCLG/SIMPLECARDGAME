using Godot;

public static class CardEffectFactory
{
    public static CardEffect Create(string effectString)
    {
        if (string.IsNullOrEmpty(effectString))
        {
            GD.PushError($"CardEffectFactory.Create: empty EffectString");
            return null;
        }

        // Handle composite effects (comma-separated)
        if (effectString.Contains(","))
        {
            var composite = new CompositeCardEffect();
            var parts = effectString.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var effect = CreateSingle(trimmed);
                if (effect != null)
                {
                    composite.AddEffect(effect);
                }
            }
            return composite.Effects.Count > 0 ? composite : null;
        }

        return CreateSingle(effectString);
    }

    private static CardEffect CreateSingle(string effectString)
    {
        switch (effectString)
        {
            case "Attack":
                return new AttackEffect();
            case "Defense":
                return new DefenseEffect();
            case "Draw":
                return new DrawEffect();
            case "Mana":
                return new ManaEffect();
            case "Cleave":
                return new CleaveEffect();
            case "Poison":
                return new PoisonEffect();
            case "None":
                return new NoEffect();
            case "Discard":
                return new DiscardEffect();
            case "Summon":
                return new SummonEffect();
            case "OnDiscardMana":
                return new ManaOnDiscardEffect();
            case "OnDiscardDraw":
                return new DrawOnDiscardEffect();
            default:
                GD.PushError($"CardEffectFactory.CreateSingle: unknown EffectString '{effectString}'");
                return null;
        }
    }
}
