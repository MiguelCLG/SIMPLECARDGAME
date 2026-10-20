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
                // Every card sits in an expanding cell so the grid distributes spare room.
                AssertThat(child is CenterContainer).IsTrue();
                CenterContainer cell = (CenterContainer)child;
                AssertThat(cell.SizeFlagsHorizontal).IsEqual(Control.SizeFlags.ExpandFill);
                AssertThat(cell.SizeFlagsVertical).IsEqual(Control.SizeFlags.ExpandFill);
                AssertThat(cell.GetChildCount()).IsEqual(1);
                AssertThat(cell.GetChild(0) is Card).IsTrue();

                Card card = (Card)cell.GetChild(0);
                AssertThat(card.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(card.FocusMode).IsEqual(Control.FocusModeEnum.None);
            }
        }
    }
}