using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class RunDataDeckTest
    {
        [TestCase]
        public void ReturnCardsToDeck_PoolsHandAndDiscardIntoDeck()
        {
            RunData run = new()
            {
                Deck = Cards("Deck A", "Deck B"),
                Hand = Cards("Hand A", "Hand B"),
                Discard = Cards("Discard A"),
            };

            run.ReturnCardsToDeck();

            AssertThat(run.Deck.Count).IsEqual(5);
            AssertThat(run.Hand.Count).IsEqual(0);
            AssertThat(run.Discard.Count).IsEqual(0);
            AssertThat(run.Deck.Select(c => c.CardName).SequenceEqual(
                new[] { "Deck A", "Deck B", "Hand A", "Hand B", "Discard A" })).IsTrue();
        }

        [TestCase]
        public void ReturnCardsToDeck_IsIdempotent()
        {
            RunData run = new()
            {
                Deck = Cards("Deck A"),
                Hand = Cards("Hand A"),
                Discard = Cards("Discard A"),
            };

            run.ReturnCardsToDeck();
            run.ReturnCardsToDeck();

            AssertThat(run.Deck.Count).IsEqual(3);
            AssertThat(run.Hand.Count).IsEqual(0);
            AssertThat(run.Discard.Count).IsEqual(0);
        }

        [TestCase]
        public void ReturnCardsToDeck_EmptyPilesLeaveDeckUntouched()
        {
            RunData run = new() { Deck = Cards("Deck A", "Deck B") };

            run.ReturnCardsToDeck();

            AssertThat(run.Deck.Count).IsEqual(2);
        }

        [TestCase]
        public void ReturnCardsToDeck_ToleratesNullPilesFromLegacySaves()
        {
            RunData run = new() { Deck = null, Hand = null, Discard = null };

            run.ReturnCardsToDeck();

            AssertThat(run.Deck).IsNotNull();
            AssertThat(run.Deck.Count).IsEqual(0);
        }

        private static List<CardSaveData> Cards(params string[] names)
        {
            var cards = new List<CardSaveData>();
            foreach (string name in names)
            {
                cards.Add(new CardSaveData { CardName = name });
            }
            return cards;
        }
    }
}
