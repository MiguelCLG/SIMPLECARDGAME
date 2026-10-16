using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Pure logic for picking encounter reward cards out of a <see cref="CardPoolResource"/>.
/// Kept free of Godot node dependencies so it can be unit tested without a scene.
/// </summary>
public static class RewardGenerator
{
    /// <summary>Maximum copies of the same card name allowed in the deck before it stops being offered.</summary>
    public const int MaxCopiesPerCard = 2;

    public static List<CardSaveData> PickRewardCards(RunData run, CardPoolResource pool, int count = 3)
    {
        return PickRewardCards(run, pool, count, Random.Shared);
    }

    /// <summary>
    /// Picks up to <paramref name="count"/> distinct cards from <paramref name="pool"/>.
    /// Cards already present <see cref="MaxCopiesPerCard"/> or more times in the deck are
    /// excluded, unless that leaves fewer than <paramref name="count"/> candidates, in which
    /// case the full pool is used as a fallback. Returns an empty list when no pool is set.
    /// </summary>
    public static List<CardSaveData> PickRewardCards(RunData run, CardPoolResource pool, int count, Random rng)
    {
        var result = new List<CardSaveData>();
        if (pool == null || pool.cards == null || pool.cards.Count == 0 || count <= 0)
            return result;

        rng ??= Random.Shared;

        List<CardResource> candidates = EligibleCards(run, pool.cards);
        if (candidates.Count < count)
        {
            // Small pool / deck cap reached: fall back to the full (still distinct) pool.
            candidates = DistinctCards(pool.cards);
        }

        Shuffle(candidates, rng);

        int take = Math.Min(count, candidates.Count);
        for (int i = 0; i < take; i++)
            result.Add(CardSaveData.FromResource(candidates[i]));

        return result;
    }

    private static List<CardResource> EligibleCards(RunData run, Godot.Collections.Array<CardResource> pool)
    {
        var eligible = new List<CardResource>();
        foreach (CardResource card in DistinctCards(pool))
        {
            if (CountInDeck(run, card.CardName) < MaxCopiesPerCard)
                eligible.Add(card);
        }
        return eligible;
    }

    private static List<CardResource> DistinctCards(Godot.Collections.Array<CardResource> pool)
    {
        var distinct = new List<CardResource>();
        var seen = new HashSet<string>();
        foreach (CardResource card in pool)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.CardName))
            {
                distinct.Add(card);
                continue;
            }
            if (seen.Add(card.CardName))
                distinct.Add(card);
        }
        return distinct;
    }

    private static int CountInDeck(RunData run, string cardName)
    {
        if (run?.Deck == null || string.IsNullOrEmpty(cardName)) return 0;
        int count = 0;
        foreach (CardSaveData card in run.Deck)
        {
            if (card != null && card.CardName == cardName) count++;
        }
        return count;
    }

    private static void Shuffle(List<CardResource> cards, Random rng)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
