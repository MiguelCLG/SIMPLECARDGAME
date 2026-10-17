using System;
using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Godot;
using Godot.Collections;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class RewardGeneratorTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_ReturnsRequestedDistinctCards()
        {
            var pool = Pool("Strike", "Defend", "Cleave", "Focus", "Rebirth");
            var run = RunWithDeck();

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(1));

            AssertThat(rewards.Count).IsEqual(3);
            AssertThat(rewards.Select(c => c.CardName).Distinct().Count()).IsEqual(3);
            foreach (CardSaveData card in rewards)
                AssertThat(pool.cards.Any(p => p.CardName == card.CardName)).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_ExcludesCardsAtDuplicateCap()
        {
            var pool = Pool("Strike", "Defend", "Cleave", "Focus", "Rebirth");
            var run = RunWithDeck("Strike", "Strike");

            for (int seed = 0; seed < 20; seed++)
            {
                List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(seed));
                AssertThat(rewards.Any(c => c.CardName == "Strike")).IsFalse();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_AllowsUpToCapCopies()
        {
            var pool = Pool("Strike");
            var run = RunWithDeck("Strike");

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 1, new Random(1));

            AssertThat(rewards.Count).IsEqual(1);
            AssertThat(rewards[0].CardName).IsEqual("Strike");
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_CountsCopiesAcrossDrawHandAndDiscard()
        {
            // In-game RunData.Deck is only the draw pile at reward time: the other copies of a
            // card sit in Hand/Discard. The cap must count all three piles.
            var pool = Pool("Strike", "Cleave", "Focus", "Rebirth");
            var run = RunWithSplitPiles(
                deck: new[] { "Defend" },
                hand: new[] { "Strike", "Strike" },
                discard: new[] { "Strike" });

            for (int seed = 0; seed < 20; seed++)
            {
                List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(seed));
                AssertThat(rewards.Any(c => c.CardName == "Strike")).IsFalse();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_CountsCardsAtCapSplitBetweenPiles()
        {
            // Exactly at the cap (1 drawn away from the draw pile) must already be excluded.
            var pool = Pool("Strike", "Cleave", "Focus", "Rebirth");
            var run = RunWithSplitPiles(
                deck: new[] { "Strike" },
                hand: new[] { "Strike" },
                discard: System.Array.Empty<string>());

            for (int seed = 0; seed < 20; seed++)
            {
                List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(seed));
                AssertThat(rewards.Any(c => c.CardName == "Strike")).IsFalse();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_FallsBackToFullPoolWhenFilteredTooSmall()
        {
            // Every card is at the cap except one -> filtering leaves < 3 options.
            var pool = Pool("Strike", "Defend", "Cleave");
            var run = RunWithDeck("Strike", "Strike", "Defend", "Defend", "Cleave", "Cleave");

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(1));

            AssertThat(rewards.Count).IsEqual(3);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_ClampsCountToPoolSize()
        {
            var pool = Pool("Strike", "Defend");
            var run = RunWithDeck();

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(1));

            AssertThat(rewards.Count).IsEqual(2);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_DeduplicatesByCardName()
        {
            var pool = Pool("Strike", "Strike", "Defend");
            var run = RunWithDeck();

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(run, pool, 3, new Random(7));

            AssertThat(rewards.Count).IsEqual(2);
            AssertThat(rewards.Select(c => c.CardName).Distinct().Count()).IsEqual(2);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_EmptyOrNullPoolReturnsEmpty()
        {
            var run = RunWithDeck();
            var emptyPool = new CardPoolResource { cards = new Array<CardResource>() };

            AssertThat(RewardGenerator.PickRewardCards(run, emptyPool, 3, new Random(1)).Count).IsEqual(0);
            AssertThat(RewardGenerator.PickRewardCards(run, null, 3, new Random(1)).Count).IsEqual(0);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void PickRewardCards_CarriesCardStatsFromResource()
        {
            var pool = new CardPoolResource { cards = new Array<CardResource>() };
            pool.cards.Add(new CardResource
            {
                CardName = "Cleave",
                Description = "Deal 4 Damage to all enemies",
                Cost = 1,
                Value = 4,
                Amount = 1,
                EffectString = "Cleave",
                isMultipleTargets = true
            });

            List<CardSaveData> rewards = RewardGenerator.PickRewardCards(RunWithDeck(), pool, 1, new Random(1));

            AssertThat(rewards.Count).IsEqual(1);
            AssertThat(rewards[0].CardName).IsEqual("Cleave");
            AssertThat(rewards[0].Cost).IsEqual(1);
            AssertThat(rewards[0].Value).IsEqual(4);
            AssertThat(rewards[0].EffectString).IsEqual("Cleave");
            AssertThat(rewards[0].isMultipleTargets).IsTrue();
        }

        private static CardPoolResource Pool(params string[] names)
        {
            var pool = new CardPoolResource { cards = new Array<CardResource>() };
            foreach (string name in names)
                pool.cards.Add(new CardResource { CardName = name });
            return pool;
        }

        private static RunData RunWithDeck(params string[] cardNames)
        {
            var run = new RunData();
            foreach (string name in cardNames)
                run.Deck.Add(new CardSaveData { CardName = name });
            return run;
        }

        private static RunData RunWithSplitPiles(string[] deck, string[] hand, string[] discard)
        {
            var run = new RunData();
            foreach (string name in deck)
                run.Deck.Add(new CardSaveData { CardName = name });
            foreach (string name in hand)
                run.Hand.Add(new CardSaveData { CardName = name });
            foreach (string name in discard)
                run.Discard.Add(new CardSaveData { CardName = name });
            return run;
        }
    }
}
