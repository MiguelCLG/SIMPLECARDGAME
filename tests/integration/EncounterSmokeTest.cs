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

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn");
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
    }
}