using Godot;

public static class CardUi
{
    private static readonly PackedScene CardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");

    public static Card CreateCardView(CardSaveData data)
    {
        Card card = CardScene.Instantiate<Card>();
        Texture2D image = string.IsNullOrEmpty(data.CardImage)
            ? null
            : ResourceCache.Load<Texture2D>(data.CardImage);
        card.CardName = data.CardName;
        card.CardImage = image;
        card.Description = data.Description;
        card.Cost = data.Cost;
        card.EffectString = data.EffectString;
        card.Value = data.Value;
        card.Amount = data.Amount;
        card.isTargetingSelf = data.isTargetingSelf;
        card.isMultipleTargets = data.isMultipleTargets;
        card.InitializeEffect();
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        card.FocusMode = Control.FocusModeEnum.None;
        return card;
    }
}