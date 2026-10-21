using Godot;
using Godot.Collections;

public partial class SummonEffect : CardEffect
{
    public override CardEffectType Type => CardEffectType.Support;
    public string CardPath { get; set; }

    public override void ApplyEffect(Character character)
    {
        var player = character as Player;
        if (player == null)
        {
            GD.PushError("SummonEffect.ApplyEffect: target must be Player");
            return;
        }
        if (string.IsNullOrEmpty(CardPath))
        {
            GD.PushError("SummonEffect.ApplyEffect: CardPath is empty");
            return;
        }

        for (int i = 0; i < Amount; i++)
        {
            var resource = ResourceCache.Load<CardResource>(CardPath);
            if (resource == null)
            {
                GD.PushError($"SummonEffect.ApplyEffect: failed to load {CardPath}");
                continue;
            }
            var data = CardSaveData.FromResource(resource);
            // Copy summon path if present (should be on resource/card data)
            data.SummonCardPath = resource.SummonCardPath;
            player.SpawnCard(data);
        }
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        if (characters.Count > 0)
        {
            ApplyEffect(characters[0]);
        }
    }
}
