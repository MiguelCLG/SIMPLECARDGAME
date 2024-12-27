using Godot;
using Newtonsoft.Json;

public partial class Card : Button
{
    [Export]
    public string CardName { get; set; }

    [Export]
    public string Description { get; set; }

    [Export]
    public int Cost { get; set; }

    [Export]
    public string EffectString;
    public CardEffect Effect { get; set; }

    [Export]
    public int Value { get; set; }
    [Export]
    public int Amount { get; set; }
    [Export]
    public bool isTargetingSelf = false;
    [Export]
    public bool isMultipleTargets = false;

    public override void _Ready()
    {
        GetNode<Label>("ManaContainer/ManaCost").Text = Cost.ToString();
        GetNode<Label>("CardName").Text = CardName;
        GetNode<Label>("Description").Text = Description;
    }

    public void InitializeEffect()
    {
        switch (EffectString)
        {
            case "Attack":
                Effect = new AttackEffect();
                break;
            case "Defense":
                Effect = new DefenseEffect();

                break;
            case "Draw":
                Effect = new DrawEffect();
                break;
            case "Mana":
                Effect = new ManaEffect();
                break;
            case "Cleave":
                Effect = new CleaveEffect();
                break;
            case "Poison":
                Effect = new PoisonEffect();
                break;
                // Handle other effect types if needed
        }
        Effect.Value = Value;
        Effect.Amount = Amount;
        Effect.isMultipleTargets = isMultipleTargets;
        Effect.isTargetingSelf = isTargetingSelf;
    }

    public void OnButtonPressed()
    {
        EventRegistry.GetEventPublisher("OnCardClick").RaiseEvent(this);
        return;
    }
}
