using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class CardCompendiumSmokeTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public async Task Compendium_LoadsAndFillsGridWithEveryCatalogCard()
        {
            int expected = CardCatalog.All().Count;

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/CardCompendium.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            CardGrid grid = sceneRoot.GetNode<CardGrid>("%CardGrid");
            AssertThat(grid).IsNotNull();
            AssertThat(grid.CardCount).IsEqual(expected);
            AssertThat(expected > 0).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task Compendium_CardsAreNonInteractive()
        {
            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/CardCompendium.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            CardGrid grid = sceneRoot.GetNode<CardGrid>("%CardGrid");
            GridContainer container = grid.GetChild<GridContainer>(0);
            foreach (Node child in container.GetChildren())
            {
                AssertThat(child is Card).IsTrue();
                AssertThat(((Card)child).MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(((Card)child).FocusMode).IsEqual(Control.FocusModeEnum.None);
            }
        }
    }
}