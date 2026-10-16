using Godot.Collections;
using Godot;

public partial class DiscardEffect : CardEffect
{
    public Card PendingTarget { get; private set; }
    public override CardEffectType Type => CardEffectType.Support;
    public override bool RequiresHandTarget => true;

    public override void SetDiscardTarget(Card card)
    {
        PendingTarget = card;
    }

    public override void ApplyEffect(Character character)
    {
        var player = character as Player;
        if (player == null)
        {
            GD.PushError("DiscardEffect.ApplyEffect: target must be Player");
            return;
        }
        if (PendingTarget == null)
        {
            // No target chosen - silently no-op as per assumptions
            return;
        }
        player.DiscardCard(PendingTarget);
    }

    public override void ApplyEffect(Array<Character> characters)
    {
        // Discard targets a single card from hand, not multiple characters
        if (characters.Count > 0)
        {
            ApplyEffect(characters[0]);
        }
    }
}
