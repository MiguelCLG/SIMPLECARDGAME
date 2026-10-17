using System.Collections.Generic;
using Godot;

public partial class MainMenu : Control
{
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
    private Control gameContainer;
    private Control optionContainer;
    private readonly Dictionary<Button, Tween> hoverTweens = new();
    private bool isTransitioning = false;
    private Tween activeTransition;
    private Vector2 menuHomePosition;
    private Vector2 gameHomePosition;
    private Vector2 optionHomePosition;

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
        gameContainer = GetNode<Control>("%GameContainer");
        optionContainer = GetNode<Control>("%OptionContainer");

        ConnectHover(startButton);
        ConnectHover(optionsButton);
        ConnectHover(cardsButton);
        ConnectHover(quitButton);

        continueButton.Disabled = !RunManager.HasSave();

        int masterBus = AudioServer.GetBusIndex("Master");
        volumeSlider.Value = Mathf.Clamp(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus)) * 100f, 0f, 100f);
        volumeSlider.ValueChanged += OnVolumeChanged;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Remember where the layout puts each container so the transitions always animate
        // back to it. The menu is already laid out, but the game/options panels start hidden
        // so their containers have not necessarily been positioned yet.
        menuHomePosition = menuContainer.Position;
        gameHomePosition = CenteredPosition(gameContainer);
        optionHomePosition = CenteredPosition(optionContainer);
        AnimateMenuIn();
    }

    /// <summary>
    /// The position this scene's full-screen CenterContainer gives a child, mirrored from
    /// Godot's own centering math. Used for containers inside panels that are still hidden,
    /// where <see cref="Control.Position"/> may not reflect the final layout yet.
    /// </summary>
    private Vector2 CenteredPosition(Control child)
        => ((GetViewport().GetVisibleRect().Size - child.GetCombinedMinimumSize()) / 2f).Floor();

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

    private void OnStartPressed()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        TweenToGamePanel();
    }

    private void TweenToGamePanel()
    {
        if (activeTransition != null && IsInstanceValid(activeTransition))
            activeTransition.Kill();

        gamePanel.Visible = true;
        // Ensure game container starts off-screen to the right
        gameContainer.PivotOffset = gameContainer.Size / 2f;
        gameContainer.Position = new Vector2(GetViewport().GetVisibleRect().Size.X + 24f, gameHomePosition.Y);

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(menuContainer, "position", new Vector2(-menuContainer.Size.X - 48f, menuHomePosition.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(gameContainer, "position", gameHomePosition, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            isTransitioning = false;
            activeTransition = null;
        };
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

    private void OnBackToMenuPressed()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        TweenToMainFromGame();
    }

    private void TweenToMainFromGame()
    {
        if (activeTransition != null && IsInstanceValid(activeTransition))
            activeTransition.Kill();

        gamePanel.Visible = true;

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(gameContainer, "position", new Vector2(GetViewport().GetVisibleRect().Size.X + 24f, gameHomePosition.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", menuHomePosition, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            gamePanel.Visible = false;
            // Restore game container position for next time
            gameContainer.Position = gameHomePosition;
            isTransitioning = false;
            activeTransition = null;
        };
    }

    private void OnOptionsPressed()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        TweenToOptionsPanel();
    }

    private void TweenToOptionsPanel()
    {
        if (activeTransition != null && IsInstanceValid(activeTransition))
            activeTransition.Kill();

        optionsPanel.Visible = true;
        optionsPanel.Modulate = new Color(optionsPanel.Modulate.R, optionsPanel.Modulate.G, optionsPanel.Modulate.B, 0f);
        optionContainer.PivotOffset = optionContainer.Size / 2f;
        optionContainer.Position = new Vector2(optionHomePosition.X, GetViewport().GetVisibleRect().Size.Y + 48f);

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(menuContainer, "position", new Vector2(-menuContainer.Size.X - 48f, menuHomePosition.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionsPanel, "modulate:a", 1f, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionContainer, "position", optionHomePosition, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            isTransitioning = false;
            activeTransition = null;
        };
    }

    private void OnBackPressed()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        TweenToMainFromOptions();
    }

    private void TweenToMainFromOptions()
    {
        if (activeTransition != null && IsInstanceValid(activeTransition))
            activeTransition.Kill();

        optionsPanel.Visible = true;

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(optionContainer, "position", new Vector2(optionHomePosition.X, GetViewport().GetVisibleRect().Size.Y + 48f), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionsPanel, "modulate:a", 0f, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", menuHomePosition, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            optionsPanel.Visible = false;
            optionsPanel.Modulate = new Color(1f, 1f, 1f, 1f);
            optionContainer.Position = optionHomePosition;
            isTransitioning = false;
            activeTransition = null;
        };
    }

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