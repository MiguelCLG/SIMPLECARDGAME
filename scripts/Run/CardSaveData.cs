public class CardSaveData
{
    public string CardName;
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

        return new CardSaveData
        {
            CardName = card.CardName,
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
        Card card = new();
        card.CardName = CardName;
        card.Description = Description;
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
