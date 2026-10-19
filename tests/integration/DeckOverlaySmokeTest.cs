using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class DeckOverlaySmokeTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckButton_TogglesOverlayAndListsEveryOwnedCard()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            Panel overlay = sceneRoot.GetNode<Panel>("%DeckOverlay");
            AssertThat(overlay.Visible).IsFalse();

            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(2);

            AssertThat(overlay.Visible).IsTrue();
            CardGrid grid = sceneRoot.GetNode<CardGrid>("%CardGrid");
            int owned = player.Deck.Count + player.Hand.Count + player.DiscardPile.Count;
            AssertThat(grid.CardCount).IsEqual(owned);

            GridContainer container = grid.GetChild<GridContainer>(0);
            AssertThat(container.GetChildCount()).IsEqual(owned);
            foreach (Node child in container.GetChildren())
            {
                AssertThat(child is Card).IsTrue();
                AssertThat(((Card)child).FocusMode).IsEqual(Control.FocusModeEnum.None);
            }

            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(2);
            AssertThat(overlay.Visible).IsFalse();
        }
    }
}