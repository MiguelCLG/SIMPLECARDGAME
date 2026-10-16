using Godot;
using Newtonsoft.Json;

public partial class Card : Button
{
    [Export]
    public string CardName { get; set; }

    [Export]
    public Texture2D CardImage { get; set; }

    [Export]
    public string Description { get; set; }

    [Export]
    public int Cost { get; set; }

    [Export]
    public string EffectString;
    public CardEffect Effect { get; set; }

    [Export]
    public string OnDiscardEffectString;
    public CardEffect OnDiscardEffect { get; set; }

    [Export]
    public int Value { get; set; }
    [Export]
    public int Amount { get; set; }
    [Export]
    public bool isTargetingSelf = false;
    [Export]
    public bool isMultipleTargets = false;
    [Export]
    public string SummonCardPath;

    public override void _Ready()
    {
        GetNode<Label>("ManaCost").Text = Cost.ToString();
        GetNode<TextureRect>("%CardImage").Texture = CardImage;
        GetNode<Label>("%CardName").Text = CardName;
        GetNode<Label>("%Description").Text = Description;
    }

    public void InitializeEffect()
    {
        Effect = CardEffectFactory.Create(EffectString);
        if (Effect == null)
        {
            GD.PushError($"Card.InitializeEffect: unknown EffectString '{EffectString}' for card '{CardName}'");
            return;
        }
        Effect.Value = Value;
        Effect.Amount = Amount;
        Effect.isMultipleTargets = isMultipleTargets;
        Effect.isTargetingSelf = isTargetingSelf;
        if (Effect is SummonEffect summonEffect && !string.IsNullOrEmpty(SummonCardPath))
        {
            summonEffect.CardPath = SummonCardPath;
        }
        InitializeOnDiscardEffect();
    }

    public void InitializeOnDiscardEffect()
    {
        if (string.IsNullOrEmpty(OnDiscardEffectString))
        {
            OnDiscardEffect = null;
            return;
        }
        OnDiscardEffect = CardEffectFactory.Create(OnDiscardEffectString);
        if (OnDiscardEffect != null)
        {
            OnDiscardEffect.Value = Value;
            OnDiscardEffect.Amount = Amount;
            OnDiscardEffect.isMultipleTargets = false;
            OnDiscardEffect.isTargetingSelf = true;
        }
    }

    public void OnButtonPressed()
    {
        EventRegistry.GetEventPublisher("OnCardClick").RaiseEvent(this);
        return;
    }
}
