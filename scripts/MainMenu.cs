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
    private readonly Dictionary<Button, Tween> hoverTweens = new();

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
        gamePanel.Visible = true;
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
        gamePanel.Visible = false;
    }

    private void OnOptionsPressed()
    {
        optionsPanel.Visible = true;
    }

    private void OnBackPressed()
    {
        optionsPanel.Visible = false;
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