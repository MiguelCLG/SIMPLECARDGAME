using System.Collections.Generic;
using Godot;

public partial class MainMenu : Control
{
    private Button startButton;
    private Button optionsButton;
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

    public override async void _Ready()
    {
        startButton = GetNode<Button>("%StartButton");
        optionsButton = GetNode<Button>("%OptionsButton");
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
        ConnectHover(quitButton);

        continueButton.Disabled = !RunManager.HasSave();

        int masterBus = AudioServer.GetBusIndex("Master");
        volumeSlider.Value = Mathf.Clamp(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus)) * 100f, 0f, 100f);
        volumeSlider.ValueChanged += OnVolumeChanged;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        AnimateMenuIn();
    }

    private void AnimateMenuIn()
    {
        Button[] buttons = { startButton, optionsButton, quitButton };
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
        Vector2 gameTarget = gameContainer.Position;
        gameContainer.Position = new Vector2(GetViewport().GetVisibleRect().Size.X + 24f, gameTarget.Y);

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(menuContainer, "position", new Vector2(-menuContainer.Size.X - 48f, menuContainer.Position.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(gameContainer, "position", gameTarget, 0.2f)
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
        Vector2 gameTarget = gameContainer.Position;
        Vector2 menuTarget = menuContainer.Position;

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(gameContainer, "position", new Vector2(GetViewport().GetVisibleRect().Size.X + 24f, gameContainer.Position.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", menuTarget, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            gamePanel.Visible = false;
            // Restore game container position for next time
            gameContainer.Position = gameTarget;
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
        Vector2 optionTarget = optionContainer.Position;
        optionContainer.Position = new Vector2(optionTarget.X, GetViewport().GetVisibleRect().Size.Y + 48f);

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(menuContainer, "position", new Vector2(-menuContainer.Size.X - 48f, menuContainer.Position.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionsPanel, "modulate:a", 1f, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionContainer, "position", optionTarget, 0.2f)
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
        Vector2 optionTarget = optionContainer.Position;
        Vector2 menuTarget = menuContainer.Position;

        activeTransition = CreateTween();
        activeTransition.Parallel().TweenProperty(optionContainer, "position", new Vector2(optionTarget.X, GetViewport().GetVisibleRect().Size.Y + 48f), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(optionsPanel, "modulate:a", 0f, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Parallel().TweenProperty(menuContainer, "position", menuTarget, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        activeTransition.Finished += () =>
        {
            optionsPanel.Visible = false;
            optionsPanel.Modulate = new Color(1f, 1f, 1f, 1f);
            optionContainer.Position = optionTarget;
            isTransitioning = false;
            activeTransition = null;
        };
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