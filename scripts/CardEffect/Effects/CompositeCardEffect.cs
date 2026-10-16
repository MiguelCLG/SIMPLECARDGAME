using System.Collections.Generic;
using Godot.Collections;

public partial class CompositeCardEffect : CardEffect
{
    public List<CardEffect> Effects { get; private set; } = new List<CardEffect>();
    public override CardEffectType Type
    {
        get
        {
            foreach (var e in Effects)
            {
                if (e != null && e.Type == CardEffectType.Attack) return CardEffectType.Attack;
            }
            foreach (var e in Effects)
            {
                if (e != null && e.Type == CardEffectType.Defense) return CardEffectType.Defense;
            }
            return CardEffectType.Support;
        }
    }

    public void AddEffect(CardEffect effect)
    {
        if (effect != null)
        {
            Effects.Add(effect);
        }
    }

    public override bool RequiresHandTarget
    {
        get
        {
            foreach (var e in Effects)
            {
                if (e != null && e.RequiresHandTarget) return true;
            }
            return false;
        }
    }

    public override void SetDiscardTarget(Card card)
    {
        foreach (var e in Effects)
        {
            if (e != null) e.SetDiscardTarget(card);
        }
    }

    public override CardEffect Find<T>()
    {
        foreach (var e in Effects)
        {
            if (e is T) return e as T;
            var found = e?.Find<T>();
            if (found != null) return found;
        }
        return null;
    }

    public override void ApplyEffect(Character character)
    {
        foreach (var e in Effects)
        {
            if (e != null)
            {
                e.ApplyEffect(character);
            }
        }
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        foreach (var e in Effects)
        {
            if (e != null)
            {
                e.ApplyEffect(characters);
            }
        }
    }
}
