using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    /// <summary>
    /// Covers the post-encounter reward flow end to end: the panel appears after a win,
    /// picking a card adds it to the persisted deck, and reward cards never run the
    /// global play-card flow (watch-out from the issue-13 plan review).
    /// </summary>
    [TestSuite]
    public class RewardFlowTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public void RewardCard_DoesNotRaiseOnCardClick_WhenGuardDisabled()
        {
            EventRegistry.RegisterEvent("OnCardClick");
            int clicks = 0;
            EventHandler<object> handler = (sender, args) => clicks++;
            EventSubscriber.SubscribeToEvent("OnCardClick", handler);
            try
            {
                var cardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");

                Card rewardCard = AutoFree(cardScene.Instantiate<Card>());
                rewardCard.RaiseCardClick = false;
                rewardCard.EmitSignal(BaseButton.SignalName.Pressed);
                AssertThat(clicks).IsEqual(0);

                Card handCard = AutoFree(cardScene.Instantiate<Card>());
                handCard.RaiseCardClick = true;
                handCard.EmitSignal(BaseButton.SignalName.Pressed);
                AssertThat(clicks).IsEqual(1);
            }
            finally
            {
                EventSubscriber.UnsubscribeFromEvent("OnCardClick", handler);
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task EncounterWin_ShowsRewardPanel_AndChoiceAddsCardToDeck()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
            var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
            var rewardContainer = sceneRoot.GetNode<Node>("%RewardCardContainer");
            var player = sceneRoot.GetNode<Player>("%Player");

            AssertThat(rewardPanel.Visible).IsFalse();

            int deckBefore = RunManager.CurrentRun.Deck.Count;
            int rowBefore = RunManager.CurrentRun.CurrentRow;
            int manaBefore = player.Mana;
            int cardsPlayedBefore = RunManager.CurrentRun.Stats.CardsPlayed;

            var enemies = new List<Node>();
            foreach (Node child in enemySpawn.GetChildren())
                enemies.Add(child);
            AssertThat(enemies.Count >= 1).IsTrue();

            var publisher = EventRegistry.GetEventPublisher("OnEnemyDie");
            foreach (Node enemy in enemies)
                publisher.RaiseEvent(enemy);

            // Let the deferred enemy QueueFree() calls flush before checking orphan-free cleanup.
            await sceneRunner.SimulateFrames(2);

            // No scene transition until a choice is made; the panel offers 3 cards.
            AssertThat(rewardPanel.Visible).IsTrue();
            AssertThat(rewardContainer.GetChildCount()).IsEqual(3);

            var rewardCard = rewardContainer.GetChild(0) as Card;
            AssertThat(rewardCard).IsNotNull();
            AssertThat(rewardCard.RaiseCardClick).IsFalse();
            string chosenName = rewardCard.CardName;

            rewardCard.EmitSignal(BaseButton.SignalName.Pressed);

            AssertThat(rewardPanel.Visible).IsFalse();
            AssertThat(RunManager.CurrentRun.Deck.Count).IsEqual(deckBefore + 1);
            AssertThat(RunManager.CurrentRun.Deck.Any(c => c.CardName == chosenName)).IsTrue();
            AssertThat(RunManager.CurrentRun.CurrentRow).IsEqual(rowBefore + 1);

            // Clicking a reward card must not spend mana or count as a played card.
            AssertThat(player.Mana).IsEqual(manaBefore);
            AssertThat(RunManager.CurrentRun.Stats.CardsPlayed).IsEqual(cardsPlayedBefore);
        }
    }
}
