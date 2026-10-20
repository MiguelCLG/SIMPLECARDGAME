using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    /// <summary>
    /// Regression tests for the animated panel transitions in <c>Scenes/MainMenu.tscn</c>.
    ///
    /// A panel must start off-screen to the right (fading in), end at its rest position, and
    /// slide back out to the right before being hidden, while the main menu container is
    /// restored to where the layout put it. Capturing a displaced position as the tween
    /// target used to leave the menu off-screen, so the menu rendered empty after Back.
    ///
    /// The panels themselves are animated rather than their contents because the
    /// CenterContainer inside each panel re-centers its child whenever the panel becomes
    /// visible, which would swallow a tween on the container's position.
    /// </summary>
    [TestSuite]
    public class MainMenuNavigationTest
    {
        // 31 frames * 20 ms = 620 ms of simulated time, comfortably above the 300 ms tweens.
        private const uint TransitionFrames = 31;
        private const uint MillisecondsPerFrame = 20;
        private const float AlphaTolerance = 0.01f;
        private static readonly Vector2 PositionTolerance = new(1.5f, 1.5f);

        [TestCase]
        [RequireGodotRuntime]
        public async Task Options_RoundTrip_SlidesPanelAndRestoresMenu()
        {
            ISceneRunner runner = ISceneRunner.Load("res://Scenes/MainMenu.tscn", autoFree: true);
            await runner.SimulateFrames(5);

            Node scene = runner.Scene();
            Control menuContainer = scene.GetNode<Control>("%MenuContainer");
            Control optionsPanel = scene.GetNode<Control>("%OptionsPanel");
            Vector2 menuLayoutPosition = menuContainer.Position;

            Press(scene, "%OptionsButton");

            // The panel must slide in from the right and fade in rather than pop:
            // it starts visible but fully transparent and off-screen.
            AssertThat(optionsPanel.Visible).IsTrue();
            AssertAlpha(optionsPanel, 0f);
            AssertOffscreenRight(optionsPanel);

            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(optionsPanel.Visible).IsTrue();
            AssertAlpha(optionsPanel, 1f);
            AssertAtRest(optionsPanel);

            Press(scene, "OptionsPanel/BackButton");

            // The fade-out starts from full opacity, the panel is only hidden once it finishes.
            AssertThat(optionsPanel.Visible).IsTrue();
            AssertAlpha(optionsPanel, 1f);
            AssertAtRest(optionsPanel);

            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(optionsPanel.Visible).IsFalse();
            AssertAlpha(optionsPanel, 1f);
            AssertAtRest(optionsPanel);
            AssertRestored(menuContainer, menuLayoutPosition);
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task GamePanel_RoundTrip_SlidesPanelAndRestoresMenu()
        {
            ISceneRunner runner = ISceneRunner.Load("res://Scenes/MainMenu.tscn", autoFree: true);
            await runner.SimulateFrames(5);

            Node scene = runner.Scene();
            Control menuContainer = scene.GetNode<Control>("%MenuContainer");
            Control gamePanel = scene.GetNode<Control>("%GamePanel");
            Vector2 menuLayoutPosition = menuContainer.Position;

            Press(scene, "%StartButton");

            // The game panel used to appear instantly; it must slide in from the right and fade.
            AssertThat(gamePanel.Visible).IsTrue();
            AssertAlpha(gamePanel, 0f);
            AssertOffscreenRight(gamePanel);

            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(gamePanel.Visible).IsTrue();
            AssertAlpha(gamePanel, 1f);
            AssertAtRest(gamePanel);

            Press(scene, "%BackToMenuButton");

            AssertThat(gamePanel.Visible).IsTrue();
            AssertAlpha(gamePanel, 1f);
            AssertAtRest(gamePanel);

            await runner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

            AssertThat(gamePanel.Visible).IsFalse();
            AssertAlpha(gamePanel, 1f);
            AssertAtRest(gamePanel);
            AssertRestored(menuContainer, menuLayoutPosition);
        }

        private static void Press(Node scene, string path)
            => scene.GetNode<Button>(path).EmitSignal(BaseButton.SignalName.Pressed);

        private static void AssertAlpha(Control control, float expected)
            => AssertThat(control.Modulate.A).IsEqualApprox(expected, AlphaTolerance);

        private static void AssertOffscreenRight(Control panel)
            => AssertThat(panel.Position.X >= panel.GetViewport().GetVisibleRect().Size.X).IsTrue();

        private static void AssertAtRest(Control panel)
            => AssertThat(panel.Position).IsEqualApprox(Vector2.Zero, PositionTolerance);

        private static void AssertRestored(Control container, Vector2 layoutPosition)
        {
            // A negative X means the menu was shifted off-screen and now renders empty.
            AssertThat(container.Position.X >= 0f).IsTrue();
            AssertThat(container.Position).IsEqualApprox(layoutPosition, PositionTolerance);
        }
    }
}
