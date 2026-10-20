using System.Collections.Generic;
using Godot;

public partial class MainMenu : Control
{
    private const float TransitionDuration = 0.3f;

    private Button startButton;
    private Button optionsButton;
    private Button cardsButton;
    private Button quitButton;
    private Panel optionsPanel;
    private HSlider volumeSlider;
    private Panel gamePanel;
    private Button newGameButton;
    private Button continueButton;
    private Button backToMenuButton;
    private Control menuContainer;
    private readonly Dictionary<Button, Tween> hoverTweens = new();
    private bool isTransitioning = false;
    private Tween activeTransition;
    private Vector2 menuHomePosition;

    public override async void _Ready()
    {
        startButton = GetNode<Button>("%StartButton");
        optionsButton = GetNode<Button>("%OptionsButton");
        cardsButton = GetNode<Button>("%CardsButton");
        quitButton = GetNode<Button>("%QuitButton");
        optionsPanel = GetNode<Panel>("%OptionsPanel");
        volumeSlider = GetNode<HSlider>("%VolumeSlider");
        gamePanel = GetNode<Panel>("%GamePanel");
        newGameButton = GetNode<Button>("%NewGameButton");
        continueButton = GetNode<Button>("%ContinueButton");
        backToMenuButton = GetNode<Button>("%BackToMenuButton");
        menuContainer = GetNode<Control>("%MenuContainer");

        ConnectHover(startButton);
        ConnectHover(optionsButton);
        ConnectHover(cardsButton);
        ConnectHover(quitButton);

        continueButton.Disabled = !RunManager.HasSave();

        int masterBus = AudioServer.GetBusIndex("Master");
        volumeSlider.Value = Mathf.Clamp(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus)) * 100f, 0f, 100f);
        volumeSlider.ValueChanged += OnVolumeChanged;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Remember where the layout puts the menu so the transitions always animate back to it.
        menuHomePosition = menuContainer.Position;
        AnimateMenuIn();
    }

    private void AnimateMenuIn()
    {
        Button[] buttons = { startButton, optionsButton, cardsButton, quitButton };
        Tween tween = CreateTween();
        foreach (Button button in buttons)
        {
            Vector2 target = button.Position;
            button.PivotOffset = button.Size / 2f;
            button.Position = new Vector2(-button.Size.X - 24f, target.Y);
            tween
                .TweenProperty(button, "position", target, 0.2f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
        }
    }

    private void ConnectHover(Button button)
    {
        button.MouseEntered += () => ScaleButton(button, Vector2.One * 1.2f);
        button.MouseExited += () => ScaleButton(button, Vector2.One);
    }

    private void ScaleButton(Button button, Vector2 targetScale)
    {
        if (hoverTweens.TryGetValue(button, out Tween previous) && IsInstanceValid(previous))
            previous.Kill();
        Tween tween = CreateTween();
        hoverTweens[button] = tween;
        tween.Finished += () =>
        {
            if (hoverTweens.TryGetValue(button, out Tween current) && current == tween)
                hoverTweens.Remove(button);
        };
        tween
            .TweenProperty(button, "scale", targetScale, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private void OnVolumeChanged(double value)
    {
        int masterBus = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusVolumeDb(masterBus, (float)Mathf.LinearToDb(value / 100.0));
    }

    private void OnStartPressed() => OpenPanel(gamePanel);

    private void OnOptionsPressed() => OpenPanel(optionsPanel);

    private void OnBackToMenuPressed() => ClosePanel(gamePanel);

    private void OnBackPressed() => ClosePanel(optionsPanel);

    /// <summary>Fully off-screen position to the right of the viewport.</summary>
    private Vector2 OffscreenRight()
        => new(GetViewport().GetVisibleRect().Size.X + 24f, 0f);

    private Vector2 HiddenMenuPosition()
        => new(-menuContainer.Size.X - 48f, menuHomePosition.Y);

    /// <summary>
    /// Slides and fades a panel in from the right while the main menu slides out to the
    /// left, so panels never pop into place. The panel itself is animated (not its
    /// contents) because the CenterContainer inside it re-centers its child whenever the
    /// panel becomes visible, which would swallow a tween on the container's position.
    /// </summary>
    private void OpenPanel(Panel panel)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        KillActiveTransition();

        panel.Visible = true;
        SetAlpha(panel, 0f);
        panel.Position = OffscreenRight();

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(panel, "position", Vector2.Zero, TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(panel, "modulate:a", 1f, TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", HiddenMenuPosition(), TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += EndTransition;
    }

    private void OnNewGamePressed()
    {
        RunManager.NewRun();
        GetTree().ChangeSceneToFile("res://Scenes/PathMap.tscn");
    }

    private void OnContinuePressed()
    {
        if (!RunManager.LoadSave()) return;
        if (RunManager.CurrentRun.ActiveEncounter)
            GetTree().ChangeSceneToFile("res://Scenes/Encounter.tscn");
        else
            GetTree().ChangeSceneToFile("res://Scenes/PathMap.tscn");
    }

    /// <summary>
    /// Fades a panel out and slides it back out to the right while the main menu returns
    /// from the left. Restores every tweened property on finish.
    /// </summary>
    private void ClosePanel(Panel panel)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        KillActiveTransition();

        panel.Visible = true;

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(panel, "position", OffscreenRight(), TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(panel, "modulate:a", 0f, TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", menuHomePosition, TransitionDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            panel.Visible = false;
            panel.Position = Vector2.Zero;
            SetAlpha(panel, 1f);
            menuContainer.Position = menuHomePosition;
            EndTransition();
        };
    }

    private void KillActiveTransition()
    {
        if (activeTransition != null && IsInstanceValid(activeTransition))
            activeTransition.Kill();
    }

    private void EndTransition()
    {
        isTransitioning = false;
        activeTransition = null;
    }

    private static void SetAlpha(CanvasItem item, float alpha)
        => item.Modulate = new Color(item.Modulate.R, item.Modulate.G, item.Modulate.B, alpha);

    private void OnCardsPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardCompendium.tscn");
    }

    private void OnQuitPressed()
    {
        if (OS.GetName() == "Web")
        {
            GD.Print("Quit is not supported on web");
            return;
        }
        GetTree().Quit();
    }
}