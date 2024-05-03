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

    [JsonProperty("Effect")]
    public string EffectString;

    [JsonIgnore]
    public CardEffect Effect { get; set; }

    public int Value { get; set; }

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
                Effect.Value = Value;
                break;
            case "Defense":
                Effect = new DefenseEffect();
                Effect.Value = Value;
                break;
            case "Draw":
                Effect = new DrawEffect();
                Effect.Value = Value;
                break;
            // Handle other effect types if needed
        }
    }

    public void OnButtonPressed()
    {
        EventRegistry.GetEventPublisher("OnCardClick").RaiseEvent(this);
        return;
    }
}
