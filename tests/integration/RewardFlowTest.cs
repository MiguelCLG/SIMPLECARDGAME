using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        /// <summary>
        /// Regression (quit mid-reward restarts the fight): at victory the offered cards
        /// and the awaiting-reward flag must be on disk BEFORE any choice is made.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task EncounterWin_PersistsPendingRewardBeforeChoice()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
            var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
            var rewardContainer = sceneRoot.GetNode<Node>("%RewardCardContainer");

            int rowBefore = RunManager.CurrentRun.CurrentRow;
            AssertThat(RunManager.CurrentRun.ActiveEncounter).IsTrue();

            var enemies = new List<Node>();
            foreach (Node child in enemySpawn.GetChildren())
                enemies.Add(child);
            AssertThat(enemies.Count >= 1).IsTrue();

            var publisher = EventRegistry.GetEventPublisher("OnEnemyDie");
            foreach (Node enemy in enemies)
                publisher.RaiseEvent(enemy);
            await sceneRunner.SimulateFrames(2);

            AssertThat(rewardPanel.Visible).IsTrue();

            // The exact victory snapshot must already be on disk — a quit right here
            // must resume into this reward, not a regenerated fight.
            RunData disk = SaveManager.LoadRun();
            AssertThat(disk).IsNotNull();
            AssertThat(disk.PendingReward).IsTrue();
            AssertThat(disk.PendingRewardCards.Count).IsEqual(3);
            AssertThat(disk.Enemies.Count).IsEqual(0);
            AssertThat(disk.ActiveEncounter).IsTrue();
            AssertThat(disk.CurrentRow).IsEqual(rowBefore);
            AssertThat(disk.HasPendingReward()).IsTrue();

            // The persisted offer must be the exact set of cards the player is looking at:
            // re-offering a *different* list on resume would be a silent cheat.
            List<string> shownNames = rewardContainer.GetChildren().OfType<Card>()
                .Select(c => c.CardName).OrderBy(n => n).ToList();
            List<string> persistedNames = disk.PendingRewardCards
                .Select(c => c.CardName).OrderBy(n => n).ToList();
            AssertThat(shownNames.Count).IsEqual(3);
            AssertThat(string.Join("|", persistedNames)).IsEqual(string.Join("|", shownNames));
        }

        /// <summary>
        /// Regression (resume regenerates the fight): a victory-shaped save with a pending
        /// reward must re-offer the exact persisted cards on Encounter load — no enemies
        /// spawned, no row advance — and picking one completes the node and clears the flag.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task ResumeWithPendingReward_ReoffersRewardInsteadOfRestartingFight()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;

            // Shape the save exactly like a victory snapshot.
            run.ActiveEncounter = true;
            run.Enemies.Clear();
            run.PendingReward = true;
            run.PendingRewardCards = new List<CardSaveData>
            {
                new CardSaveData { CardName = "Reward A", Description = "A", Cost = 1, EffectString = "Attack", Value = 6, Amount = 0 },
                new CardSaveData { CardName = "Reward B", Description = "B", Cost = 1, EffectString = "Defense", Value = 5, Amount = 0 },
                new CardSaveData { CardName = "Reward C", Description = "C", Cost = 2, EffectString = "Cleave", Value = 8, Amount = 0 }
            };
            RunManager.SaveRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
            var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
            var rewardContainer = sceneRoot.GetNode<Node>("%RewardCardContainer");

            int rowBefore = run.CurrentRow;
            int deckBefore = run.Deck.Count;

            // Reward re-offered, fight NOT restarted.
            AssertThat(rewardPanel.Visible).IsTrue();
            AssertThat(rewardContainer.GetChildCount()).IsEqual(3);
            List<string> offeredNames = rewardContainer.GetChildren().OfType<Card>()
                .Select(c => c.CardName).OrderBy(n => n).ToList();
            AssertThat(string.Join("|", offeredNames)).IsEqual("Reward A|Reward B|Reward C");
            AssertThat(enemySpawn.GetChildCount()).IsEqual(0);
            AssertThat(run.CurrentRow).IsEqual(rowBefore);

            // Picking a card resolves the reward: row advances, deck grows, flag cleared on disk.
            var rewardCard = rewardContainer.GetChild(0) as Card;
            AssertThat(rewardCard).IsNotNull();
            rewardCard.EmitSignal(BaseButton.SignalName.Pressed);

            AssertThat(rewardPanel.Visible).IsFalse();
            AssertThat(run.CurrentRow).IsEqual(rowBefore + 1);
            AssertThat(run.Deck.Count).IsEqual(deckBefore + 1);

            RunData disk = SaveManager.LoadRun();
            AssertThat(disk).IsNotNull();
            AssertThat(disk.PendingReward).IsFalse();
            AssertThat(disk.PendingRewardCards.Count).IsEqual(0);
            AssertThat(disk.HasPendingReward()).IsFalse();
        }

        /// <summary>
        /// Skipping a re-offered pending reward must resolve the node exactly like the
        /// in-session skip: no card added to the deck, row advanced, and the flag cleared
        /// on disk — so the player can never get soft-locked behind the reward.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task ResumeWithPendingReward_SkipCompletesNodeWithoutAddingCard()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;

            // Same victory-shaped save as above, but the player skips the re-offered reward.
            run.ActiveEncounter = true;
            run.Enemies.Clear();
            run.PendingReward = true;
            run.PendingRewardCards = new List<CardSaveData>
            {
                new CardSaveData { CardName = "Reward A", Description = "A", Cost = 1, EffectString = "Attack", Value = 6, Amount = 0 },
                new CardSaveData { CardName = "Reward B", Description = "B", Cost = 1, EffectString = "Defense", Value = 5, Amount = 0 },
                new CardSaveData { CardName = "Reward C", Description = "C", Cost = 2, EffectString = "Cleave", Value = 8, Amount = 0 }
            };
            RunManager.SaveRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
            var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
            var rewardContainer = sceneRoot.GetNode<Node>("%RewardCardContainer");
            var skipButton = rewardPanel.GetNode<Button>("%RewardSkipButton");

            int deckBefore = run.Deck.Count;
            int rowBefore = run.CurrentRow;
            int stepsBefore = run.Stats.StepsCompleted;

            AssertThat(rewardPanel.Visible).IsTrue();
            AssertThat(rewardContainer.GetChildCount()).IsEqual(3);
            AssertThat(enemySpawn.GetChildCount()).IsEqual(0);

            skipButton.EmitSignal(BaseButton.SignalName.Pressed);

            // Skip = no card gained, but the node still completes.
            AssertThat(rewardPanel.Visible).IsFalse();
            AssertThat(run.Deck.Count).IsEqual(deckBefore);
            AssertThat(run.CurrentRow).IsEqual(rowBefore + 1);
            AssertThat(run.Stats.StepsCompleted).IsEqual(stepsBefore + 1);

            RunData disk = SaveManager.LoadRun();
            AssertThat(disk).IsNotNull();
            AssertThat(disk.Deck.Count).IsEqual(deckBefore);
            AssertThat(disk.CurrentRow).IsEqual(rowBefore + 1);
            AssertThat(disk.PendingReward).IsFalse();
            AssertThat(disk.PendingRewardCards.Count).IsEqual(0);
            AssertThat(disk.HasPendingReward()).IsFalse();
        }

        /// <summary>
        /// A save written before the fix existed (neither field present in the JSON) must
        /// still resume as a plain mid-combat fight through the full SetupRun path: no
        /// reward panel, the saved enemy re-spawned from its saved state, and the live
        /// fight persisted back without inventing a pending reward.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task ResumeLegacySaveWithoutPendingReward_ResumesMidCombatFight()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;

            run.ActiveEncounter = true;
            run.Enemies = new List<EnemySaveData>
            {
                new EnemySaveData
                {
                    EnemyName = "Wolf",
                    TexturePath = "res://Images/Enemies/Wolf/wolf_frame_1.png",
                    // Health deliberately != MaxHealth: a regenerated enemy would come
                    // back at full health, so this proves the saved state was hydrated.
                    Health = 20,
                    MaxHealth = 40,
                    Armor = 0,
                    AttackMin = 4,
                    AttackMax = 8,
                    DefendMin = 2,
                    DefendMax = 4,
                    IntentType = Intent.Attack,
                    IntentValue = 5
                }
            };
            run.PendingReward = false;
            run.PendingRewardCards = new List<CardSaveData>();

            // Emulate a pre-fix save: same payload minus the two new fields.
            JObject legacy = JObject.Parse(JsonConvert.SerializeObject(run, Formatting.Indented));
            legacy.Remove("PendingReward");
            legacy.Remove("PendingRewardCards");
            File.WriteAllText(LegacyAutoSavePath(), legacy.ToString(Formatting.Indented));

            try
            {
                AssertThat(RunManager.LoadSave()).IsTrue();

                ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
                await sceneRunner.SimulateFrames(30);

                Node sceneRoot = sceneRunner.Scene();
                var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
                var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
                var rewardContainer = sceneRoot.GetNode<Node>("%RewardCardContainer");

                // Resumes as a fight, never as a reward.
                AssertThat(RunManager.CurrentRun.HasPendingReward()).IsFalse();
                AssertThat(rewardPanel.Visible).IsFalse();
                AssertThat(rewardContainer.GetChildCount()).IsEqual(0);

                AssertThat(enemySpawn.GetChildCount()).IsEqual(1);
                var resumedEnemy = enemySpawn.GetChild(0) as Enemy;
                AssertThat(resumedEnemy).IsNotNull();
                AssertThat(resumedEnemy.Health).IsEqual(20);
                AssertThat(resumedEnemy.MaxHealth).IsEqual(40);

                // The resumed fight is written back, still with no pending reward.
                RunData disk = SaveManager.LoadRun();
                AssertThat(disk).IsNotNull();
                AssertThat(disk.Enemies.Count).IsEqual(1);
                AssertThat(disk.Enemies[0].Health).IsEqual(20);
                AssertThat(disk.PendingReward).IsFalse();
                AssertThat(disk.PendingRewardCards.Count).IsEqual(0);
            }
            finally
            {
                SaveManager.DeleteRun();
            }
        }

        /// <summary>
        /// Winning the boss row ends the run instead of offering a reward: no panel, the
        /// auto-save is consumed, and the victory lands in the run history.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task BossVictory_EndsRunWithoutOfferingReward()
        {
            string runsHistoryPath = System.IO.Path.Combine(OS.GetUserDataDir(), "runs.json");
            string historyBackup = File.Exists(runsHistoryPath) ? File.ReadAllText(runsHistoryPath) : null;

            try
            {
                RunManager.NewRun();
                RunData run = RunManager.CurrentRun;
                run.IsBossNode = true;
                run.ActiveEncounter = true;
                RunManager.SaveRun();

                ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
                await sceneRunner.SimulateFrames(30);

                Node sceneRoot = sceneRunner.Scene();
                var enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
                var rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");

                // Boss rows spawn a single enemy.
                AssertThat(enemySpawn.GetChildCount()).IsEqual(1);

                var publisher = EventRegistry.GetEventPublisher("OnEnemyDie");
                publisher.RaiseEvent(enemySpawn.GetChild(0));
                await sceneRunner.SimulateFrames(2);

                // No reward offer, no leftover pending reward, run over.
                AssertThat(rewardPanel.Visible).IsFalse();
                AssertThat(RunManager.CurrentRun).IsNull();
                AssertThat(SaveManager.HasSave()).IsFalse();

                List<RunStats> history =
                    JsonConvert.DeserializeObject<List<RunStats>>(File.ReadAllText(runsHistoryPath));
                AssertThat(history).IsNotNull();
                AssertThat(history.Count >= 1).IsTrue();
                AssertThat(history[^1].Success).IsTrue();
                AssertThat(history[^1].BossDefeated).IsTrue();
            }
            finally
            {
                if (historyBackup == null)
                {
                    if (File.Exists(runsHistoryPath)) File.Delete(runsHistoryPath);
                }
                else
                {
                    File.WriteAllText(runsHistoryPath, historyBackup);
                }
            }
        }

        private static string LegacyAutoSavePath() =>
            System.IO.Path.Combine(OS.GetUserDataDir(), "savegame.json");
    }
}
