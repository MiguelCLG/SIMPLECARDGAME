using Godot;

public class CardSaveData
{
    public string CardName;
    public string CardImage;
    public string Description;
    public int Cost;
    public string EffectString;
    public string OnDiscardEffectString;
    public int Value;
    public int Amount;
    public bool isTargetingSelf;
    public bool isMultipleTargets;
    public string SummonCardPath;

    public static CardSaveData FromCard(Card card)
    {
        string effectString = card.EffectString;
        if (string.IsNullOrEmpty(effectString) && card.Effect != null)
            effectString = card.Effect.GetType().Name.Replace("Effect", "");
        
        string onDiscard = card.OnDiscardEffectString;
        if (string.IsNullOrEmpty(onDiscard) && card.OnDiscardEffect != null)
            onDiscard = card.OnDiscardEffect.GetType().Name.Replace("Effect", "");
        
        return new CardSaveData
        {
            CardName = card.CardName,
            CardImage = card.CardImage?.ResourcePath,
            Description = card.Description,
            Cost = card.Cost,
            EffectString = effectString,
            OnDiscardEffectString = onDiscard,
            Value = card.Value,
            Amount = card.Amount,
            isTargetingSelf = card.isTargetingSelf,
            isMultipleTargets = card.isMultipleTargets,
            SummonCardPath = card.SummonCardPath
        };
    }

    public static CardSaveData FromResource(CardResource resource)
    {
        return new CardSaveData
        {
            CardName = resource.CardName,
            CardImage = resource.CardImage?.ResourcePath,
            Description = resource.Description,
            Cost = resource.Cost,
            EffectString = resource.EffectString,
            OnDiscardEffectString = resource.OnDiscardEffectString,
            Value = resource.Value,
            Amount = resource.Amount,
            isTargetingSelf = resource.isTargetingSelf,
            isMultipleTargets = resource.isMultipleTargets,
            SummonCardPath = resource.SummonCardPath
        };
    }

    public Card ToCard()
    {
        Texture2D cardImage = string.IsNullOrEmpty(CardImage)
            ? null
            : ResourceCache.Load<Texture2D>(CardImage);
        Card card = new();
        card.CardName = CardName;
        card.Description = Description;
        card.CardImage = cardImage;
        card.Cost = Cost;
        card.EffectString = EffectString;
        card.OnDiscardEffectString = OnDiscardEffectString;
        card.Value = Value;
        card.Amount = Amount;
        card.isTargetingSelf = isTargetingSelf;
        card.isMultipleTargets = isMultipleTargets;
        card.SummonCardPath = SummonCardPath;
        card.InitializeEffect();
        return card;
    }
}
