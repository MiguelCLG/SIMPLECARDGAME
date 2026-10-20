using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class EncounterSmokeTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public async Task Encounter_HydratesRunAndSpawnsEnemies()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Player player = sceneRunner.FindChild("Player") as Player;
            AssertThat(player).IsNotNull();

            int handSize = player.Hand.Count;
            AssertThat(handSize >= 3).IsTrue();
            AssertThat(handSize <= 5).IsTrue();

            Node sceneRoot = sceneRunner.Scene();
            Node enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");
            int enemyCount = enemySpawn.GetChildCount();
            AssertThat(enemyCount >= 1).IsTrue();
            AssertThat(enemyCount <= 4).IsTrue();

            AssertThat(player.Mana >= 0).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task Encounter_KeepsSceneExportedResourcesAlive()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            // These are exported on Encounter.tscn, so the engine resource cache owns them.
            // GameManager must hold their managed wrappers, otherwise the cache can abort
            // the game when it resurrects a collected wrapper on the next scene load
            // (godotengine/godot#83762). Reloading them here returns the cached wrappers.
            CardPoolResource pool = ResourceLoader.Load<CardPoolResource>("res://Resources/CardPools/DefaultCardPool.tres");
            AssertThat(pool).IsNotNull();
            AssertThat(ResourceCache.IsKeptAlive(pool)).IsTrue();
            foreach (CardResource card in pool.cards)
                AssertThat(ResourceCache.IsKeptAlive(card)).IsTrue();

            EnemyResource enemy = ResourceLoader.Load<EnemyResource>("res://Resources/Enemy/Eye.tres");
            AssertThat(enemy).IsNotNull();
            AssertThat(ResourceCache.IsKeptAlive(enemy)).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task PlayCard_CardNotInHand_IsIgnored()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            player.SetManaToMax();

            int manaBefore = player.Mana;
            int armorBefore = player.Armor;
            int handBefore = player.Hand.Count;
            int discardBefore = player.DiscardPile.Count;

            // A card that is NOT in the player's hand (added to the tree so the
            // unguarded code path would run to completion instead of crashing).
            Card card = CardUi.CreateCardView(new CardSaveData
            {
                CardName = "Guard Test Card",
                Description = "Not in hand",
                Cost = 1,
                EffectString = "Defense",
                Value = 5
            });
            sceneRoot.AddChild(card);

            Godot.Collections.Array<Character> targets = new() { player };
            player.PlayCard(card, targets);

            AssertThat(player.Mana).IsEqual(manaBefore);
            AssertThat(player.Armor).IsEqual(armorBefore);
            AssertThat(player.Hand.Count).IsEqual(handBefore);
            AssertThat(player.DiscardPile.Count).IsEqual(discardBefore);
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task PlayCard_CardInHand_IsPlayed()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            player.SetManaToMax();

            // Deterministic in-hand card: Defend (Defense, cost 1, value 5).
            // Added via SpawnCard so the Hand.Contains guard sees a legitimately
            // drawn card regardless of shuffle order.
            CardResource defend = ResourceCache.Load<CardResource>("res://Resources/Cards/Defend.tres");
            AssertThat(defend).IsNotNull();
            player.SpawnCard(CardSaveData.FromResource(defend));
            Card card = player.Hand[player.Hand.Count - 1];
            AssertThat(player.Hand.Contains(card)).IsTrue();

            int manaBefore = player.Mana;
            int armorBefore = player.Armor;
            int handBefore = player.Hand.Count;
            int discardBefore = player.DiscardPile.Count;

            Godot.Collections.Array<Character> targets = new() { player };
            player.PlayCard(card, targets);

            // The guard must not overblock: an affordable in-hand card is played,
            // mana spent, effect applied, and the card moved to the discard pile.
            AssertThat(player.Mana).IsEqual(manaBefore - defend.Cost);
            AssertThat(player.Armor).IsEqual(armorBefore + defend.Value);
            AssertThat(player.Hand.Count).IsEqual(handBefore - 1);
            AssertThat(player.DiscardPile.Count).IsEqual(discardBefore + 1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task FreshEncounter_ResetsCarriedOverArmor()
        {
            RunManager.NewRun();
            RunManager.CurrentRun.PlayerState.Armor = 5;

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Player player = sceneRunner.FindChild("Player") as Player;
            AssertThat(player).IsNotNull();
            AssertThat(player.Armor).IsEqual(0);
        }

        /// <summary>
        /// Regression (issue 12): the deck overlay is a full-screen STOP panel and must
        /// never stay open when the encounter is won, otherwise the reward panel appears
        /// underneath it. Reachable in-game by ending the turn with the overlay open so
        /// end-of-turn effects (e.g. poison) kill the last enemy.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task EncounterWin_WithDeckOverlayOpen_HidesDeckOverlay()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            Panel overlay = sceneRoot.GetNode<Panel>("%DeckOverlay");
            RewardPanel rewardPanel = sceneRoot.GetNode<RewardPanel>("%RewardPanel");
            Node enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");

            AssertThat(overlay.Visible).IsFalse();

            // Open the overlay the way the in-game Deck button does.
            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(2);
            AssertThat(overlay.Visible).IsTrue();

            // Kill every enemy through the real win path (OnEnemyDie -> ShowRewardOrComplete).
            var enemies = new System.Collections.Generic.List<Node>();
            foreach (Node child in enemySpawn.GetChildren())
                enemies.Add(child);
            AssertThat(enemies.Count >= 1).IsTrue();

            EventPublisher<object> publisher = EventRegistry.GetEventPublisher("OnEnemyDie");
            foreach (Node enemy in enemies)
                publisher.RaiseEvent(enemy);

            // Flush the deferred QueueFree() calls; no timers involved in showing the reward.
            await sceneRunner.SimulateFrames(2);

            AssertThat(overlay.Visible).IsFalse();
            AssertThat(rewardPanel.Visible).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task ResumedEncounter_KeepsArmor()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;
            run.ActiveEncounter = true;
            run.PlayerState.Armor = 5;
            run.Enemies.Add(
                new EnemySaveData
                {
                    EnemyName = "TestEnemy",
                    TexturePath = "res://Images/Icons/sword.png",
                    Health = 10,
                    MaxHealth = 10,
                    Armor = 0,
                    AttackMin = 1,
                    AttackMax = 2,
                    DefendMin = 1,
                    DefendMax = 2,
                    IntentType = Intent.Attack,
                    IntentValue = 1
                }
            );
            RunManager.SaveRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Player player = sceneRunner.FindChild("Player") as Player;
            AssertThat(player).IsNotNull();
            AssertThat(player.Armor).IsEqual(5);
        }
    }
}
