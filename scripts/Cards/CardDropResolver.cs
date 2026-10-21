using System.Collections.Generic;
using Godot;

/// <summary>
/// Where a dragged hand card was released. <see cref="None"/> means the drop is not
/// accepted by any drop zone and the drag should be cancelled (card returns to hand).
/// </summary>
public enum CardDropZone
{
    None = 0,
    Enemy = 1,
    Center = 2
}

/// <summary>
/// Result of <see cref="CardDropResolver.Resolve"/>: the accepted drop zone and, for
/// <see cref="CardDropZone.Enemy"/>, the index into the enemy rect list that was hit
/// (-1 for every other zone).
/// </summary>
public readonly record struct CardDropResult(CardDropZone Zone, int EnemyIndex)
{
    /// <summary>Drop rejected - the card goes back to the hand.</summary>
    public static CardDropResult Cancel => new(CardDropZone.None, -1);

    /// <summary>Drop accepted by the non-targeted center zone.</summary>
    public static CardDropResult Center => new(CardDropZone.Center, -1);

    /// <summary>Drop accepted by the enemy at <paramref name="index"/>.</summary>
    public static CardDropResult Enemy(int index) => new(CardDropZone.Enemy, index);
}

/// <summary>
/// Pure, side-effect-free classifier that decides which drop zone (if any) accepts a
/// dragged hand card, based on the card's effect and the drop position.
/// No Godot node access, scene lookup or IO happens here, so it is directly unit-testable.
/// </summary>
public static class CardDropResolver
{
    /// <summary>
    /// Resolves the drop zone for a card released at <paramref name="dropPosition"/>.
    /// </summary>
    /// <remarks>
    /// Drop-zone table (keyed by <paramref name="effectString"/>, matching
    /// <c>Card.InitializeEffect()</c>):
    /// <list type="bullet">
    /// <item><description><c>Attack</c> -&gt; enemy (needs a target).</description></item>
    /// <item><description><c>Poison</c> -&gt; enemy (needs a target).</description></item>
    /// <item><description><c>Cleave</c> -&gt; center (hits all enemies).</description></item>
    /// <item><description><c>Defense</c> -&gt; center (self).</description></item>
    /// <item><description><c>Draw</c> -&gt; center (self).</description></item>
    /// <item><description><c>Mana</c> -&gt; center (self).</description></item>
    /// <item><description>Unknown / null / empty -&gt; derived from the flags: center when
    /// <paramref name="isTargetingSelf"/> or <paramref name="effectType"/> is not
    /// <see cref="CardEffectType.Attack"/>, enemy otherwise.</description></item>
    /// </list>
    /// Enemy rects are tested before the center zone, so a small viewport (where the center
    /// zone overlaps an enemy) can never make the center zone swallow an enemy drop.
    /// <para>
    /// Hit testing uses <see cref="Rect2.HasPoint"/>, which is inclusive on the left/top edge and
    /// exclusive on the right/bottom edge. That engine behaviour is relied upon as-is (not
    /// corrected) so tests can assert it.
    /// </para>
    /// </remarks>
    /// <param name="effectString">Authoritative effect discriminator (case-sensitive).</param>
    /// <param name="effectType">Effect type, used only for the unknown-effect fallback.</param>
    /// <param name="isTargetingSelf">Self-target flag, used only for the unknown-effect fallback.</param>
    /// <param name="isMultipleTargets">
    /// Multi-target flag, used only for the unknown-effect fallback: a multi-target card behaves
    /// like <c>Cleave</c> (center zone, hits every enemy) rather than a single-target card.
    /// </param>
    /// <param name="dropPosition">Global position where the card was released.</param>
    /// <param name="centerZone">Rect of the center (non-targeted) drop zone.</param>
    /// <param name="enemyRects">Rect per enemy, in display order; may be null (treated as empty).</param>
    /// <returns>
    /// <see cref="CardDropResult.Enemy(int)"/> for the first enemy rect containing the drop,
    /// else <see cref="CardDropResult.Center"/> when the center zone contains it and the card is
    /// center-droppable, else <see cref="CardDropResult.Cancel"/>.
    /// </returns>
    public static CardDropResult Resolve(
        string effectString,
        CardEffectType effectType,
        bool isTargetingSelf,
        bool isMultipleTargets,
        Vector2 dropPosition,
        Rect2 centerZone,
        IReadOnlyList<Rect2> enemyRects)
    {
        ResolveDroppable(
            effectString,
            effectType,
            isTargetingSelf,
            isMultipleTargets,
            out bool canDropOnEnemy,
            out bool canDropOnCenter);

        if (canDropOnEnemy && enemyRects != null)
        {
            for (int i = 0; i < enemyRects.Count; i++)
            {
                if (enemyRects[i].HasPoint(dropPosition))
                {
                    return CardDropResult.Enemy(i);
                }
            }
        }

        if (canDropOnCenter && centerZone.HasPoint(dropPosition))
        {
            return CardDropResult.Center;
        }

        return CardDropResult.Cancel;
    }

    /// <summary>
    /// Classifies <paramref name="effectString"/> into the droppable zones. Kept private so the
    /// public surface stays a single pure <see cref="Resolve"/> call.
    /// </summary>
    private static void ResolveDroppable(
        string effectString,
        CardEffectType effectType,
        bool isTargetingSelf,
        bool isMultipleTargets,
        out bool canDropOnEnemy,
        out bool canDropOnCenter)
    {
        switch (effectString)
        {
            case "Attack":
            case "Poison":
                canDropOnEnemy = true;
                canDropOnCenter = false;
                return;

            case "Cleave":
            case "Defense":
            case "Draw":
            case "Mana":
                canDropOnEnemy = false;
                canDropOnCenter = true;
                return;

            default:
                // Unknown effect: fall back on the targeting flags (center for self / multi /
                // non-attack, matching the Cleave rule).
                bool centerOnly = isTargetingSelf || isMultipleTargets || effectType != CardEffectType.Attack;
                canDropOnEnemy = !centerOnly;
                canDropOnCenter = centerOnly;
                return;
        }
    }
}
