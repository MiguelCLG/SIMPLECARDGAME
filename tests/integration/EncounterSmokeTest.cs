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
        public async Task FreshEncounter_ResetsCarriedOverArmor()
        {
            RunManager.NewRun();
            RunManager.CurrentRun.PlayerState.Armor = 5;

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Player player = sceneRunner.FindChild("Player") as Player;
            AssertThat(player).IsNotNull();
            AssertThat(player.Armor).IsEqual(0);

            ProgressBar armorBar = player.GetNode<ProgressBar>("%ArmorBar");
            AssertThat(armorBar.Value).IsEqual(0d);
            AssertThat(armorBar.MaxValue).IsEqual(1d); // UpdateArmorVisuals ternary: Armor == 0 → MaxValue = 1

            Label armorLabel = player.GetNode<Label>("%ArmorLabel");
            AssertThat(armorLabel.Text).IsEqual("0"); // label half of the same sync
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