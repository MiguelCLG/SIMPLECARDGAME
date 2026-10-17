using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    /// <summary>
    /// Regression tests for the animated panel transitions in <c>Scenes/MainMenu.tscn</c>.
    ///
    /// The menu/game/options containers are laid out by a <see cref="CenterContainer"/>
    /// and then slid around by tweens. Opening a panel must end with its content centered,
    /// and returning to a previous panel must put the menu container back where the layout
    /// put it. Capturing the container's *current* (already displaced) position as the tween
    /// target leaves the main menu shifted off-screen, so the menu renders empty after Back.
    /// </summary>
    [TestSuite]
    public class MainMenuNavigationTest
    {
        // 31 frames * 20 ms = 620 ms of simulated time, comfortably above the 200 ms tweens.
        private const uint TransitionFrames = 31;
        private const uint MillisecondsPerFrame = 20;
        private static readonly Vector2 PositionTolerance = new(1.5f, 1.5f);
        private static readonly Vector2 CenteredTolerance = new(2f, 2f);

        [TestCase]
        [RequireGodotRuntime]
        public async Task Options_RoundTrip_CentersOptionsAndRestoresMenu()
        {
            ISceneRunner runner = ISceneRunner.Load("res://Scenes/MainMenu.tscn", autoFree: true);
            await runner.SimulateFrames(5);

            Node scene = runner.Scene();
            Control menuContainer = scene.GetNode<Control>("%MenuContainer");
            Control optionsPanel = scene.GetNode<Control>("%OptionsPanel");
            Control optionContainer = scene.GetNode<Control>("%OptionContainer");
            Vector2 menuLayoutPosition = menuContainer.Position;

            Press(scene, "%OptionsButton");
            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(optionsPanel.Visible).IsTrue();
            AssertCentered(optionContainer);

            Press(scene, "OptionsPanel/BackButton");
            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(optionsPanel.Visible).IsFalse();
            AssertRestored(menuContainer, menuLayoutPosition);
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task GamePanel_RoundTrip_CentersGameContainerAndRestoresMenu()
        {
            ISceneRunner runner = ISceneRunner.Load("res://Scenes/MainMenu.tscn", autoFree: true);
            await runner.SimulateFrames(5);

            Node scene = runner.Scene();
            Control menuContainer = scene.GetNode<Control>("%MenuContainer");
            Control gamePanel = scene.GetNode<Control>("%GamePanel");
            Control gameContainer = scene.GetNode<Control>("%GameContainer");
            Vector2 menuLayoutPosition = menuContainer.Position;

            Press(scene, "%StartButton");
            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(gamePanel.Visible).IsTrue();
            AssertCentered(gameContainer);

            Press(scene, "%BackToMenuButton");
            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(gamePanel.Visible).IsFalse();
            AssertRestored(menuContainer, menuLayoutPosition);
        }

        private static void Press(Node scene, string path)
            => scene.GetNode<Button>(path).EmitSignal(BaseButton.SignalName.Pressed);

        private static void AssertCentered(Control container)
        {
            // The container's parent CenterContainer fills the viewport, so a correctly
            // centered child has its center at the middle of the viewport.
            Vector2 viewportCenter = container.GetViewport().GetVisibleRect().Size / 2f;
            Vector2 contentCenter = container.Position + container.Size / 2f;
            AssertThat(contentCenter).IsEqualApprox(viewportCenter, CenteredTolerance);
        }

        private static void AssertRestored(Control container, Vector2 layoutPosition)
        {
            // A negative X means the menu was shifted off-screen and now renders empty.
            AssertThat(container.Position.X >= 0f).IsTrue();
            AssertThat(container.Position).IsEqualApprox(layoutPosition, PositionTolerance);
        }
    }
}
