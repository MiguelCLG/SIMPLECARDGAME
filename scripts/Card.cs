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
            // Handle other effect types if needed
        }
    }

    public void OnButtonPressed()
    {
        GD.Print("Button Pressed");
        EventRegistry.GetEventPublisher("OnCardClick").RaiseEvent(this);
        return;
    }
}
