using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class PathMapSmokeTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public async Task PathMap_BuildsCanvasWithAllPlanetNodes()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;

            int expectedNodes = 0;
            foreach (var row in run.Path)
                expectedNodes += row.Count;

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/PathMap.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            PathCanvas canvas = sceneRoot.GetNode<PathCanvas>("%PathCanvas");
            AssertThat(canvas).IsNotNull();
            AssertThat(canvas.GetChildCount()).IsEqual(expectedNodes);
            AssertThat(canvas.CustomMinimumSize.Y > 0).IsTrue();
        }
    }
}
