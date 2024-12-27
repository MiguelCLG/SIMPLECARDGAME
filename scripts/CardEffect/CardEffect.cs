using System;
using Godot.Collections;

public enum CardEffectType
{
    Attack,
    Defense,
    Support
}

public abstract class CardEffect
{
    public abstract CardEffectType Type { get; }
    public int Value;
    public int Amount = 1;
    public bool isMultipleTargets = false;
    public bool isTargetingSelf = false;
    public abstract void ApplyEffect(Array<Character> character);
    public abstract void ApplyEffect(Character character);
}
