using Godot;

public class CardSaveData
{
    public string CardName;
    public string CardImage;
    public string Description;
    public int Cost;
    public string EffectString;
    public int Value;
    public int Amount;
    public bool isTargetingSelf;
    public bool isMultipleTargets;

    public static CardSaveData FromCard(Card card)
    {
        string effectString = card.EffectString;
        if (string.IsNullOrEmpty(effectString) && card.Effect != null)
            effectString = card.Effect.GetType().Name.Replace("Effect", "");
        GD.Print(card.CardImage);
        GD.Print(card.CardName);
        GD.Print(card.Value);
        return new CardSaveData
        {
            CardName = card.CardName,
            CardImage = card.CardImage.ResourcePath,
            Description = card.Description,
            Cost = card.Cost,
            EffectString = effectString,
            Value = card.Value,
            Amount = card.Amount,
            isTargetingSelf = card.isTargetingSelf,
            isMultipleTargets = card.isMultipleTargets
        };
    }

    public static CardSaveData FromResource(CardResource resource)
    {
        return new CardSaveData
        {
            CardName = resource.CardName,
            CardImage = resource.CardImage.ResourcePath,
            Description = resource.Description,
            Cost = resource.Cost,
            EffectString = resource.EffectString,
            Value = resource.Value,
            Amount = resource.Amount,
            isTargetingSelf = resource.isTargetingSelf,
            isMultipleTargets = resource.isMultipleTargets
        };
    }

    public Card ToCard()
    {
        Texture2D cardImage = ResourceLoader.Load<Texture2D>(CardImage);
        Card card = new();
        card.CardName = CardName;
        card.Description = Description;
        card.CardImage = cardImage;
        card.Cost = Cost;
        card.EffectString = EffectString;
        card.Value = Value;
        card.Amount = Amount;
        card.isTargetingSelf = isTargetingSelf;
        card.isMultipleTargets = isMultipleTargets;
        card.InitializeEffect();
        return card;
    }
}
