using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    /// <summary>
    /// Guards <c>Themes/Theme.tres</c> against malformed text resources. When the theme fails
    /// to parse, Godot only logs an error and the scene loads with a null theme, so scene tests
    /// keep passing and the styling regression is not noticed until the editor is opened.
    /// </summary>
    [TestSuite]
    public class ThemeResourceTest
    {
        private const string ThemePath = "res://Themes/Theme.tres";

        [TestCase]
        [RequireGodotRuntime]
        public void Theme_LoadsWithExpectedTypeVariations()
        {
            Theme theme = ResourceLoader.Load<Theme>(ThemePath);

            AssertThat(theme).IsNotNull();
            AssertThat(theme.GetStylebox("panel", "TransparentPanel") is StyleBoxEmpty).IsTrue();
            AssertThat(theme.GetStylebox("panel", "OverlayPanel") is StyleBoxFlat).IsTrue();
            AssertThat(theme.GetStylebox("normal", "MainMenuButton")).IsNotNull();
        }
    }
}
