using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class PathMap : Control
{
    private PathCanvas pathCanvas;
    private Panel restPanel;
    private Label restHealthLabel;
    private Button quitButton;
    private Button deckButton;
    private DeckOverlay deckOverlay;
    private Button restButton;
    private Button skipButton;
    private Label hintLabel;

    public override void _Ready()
    {
        pathCanvas = GetNode<PathCanvas>("%PathCanvas");
        restPanel = GetNode<Panel>("%RestPanel");
        restHealthLabel = GetNode<Label>("%RestHealthLabel");
        quitButton = GetNode<Button>("%QuitButton");
        deckButton = GetNode<Button>("%DeckButton");
        deckOverlay = GetNode<DeckOverlay>("%DeckOverlay");
        restButton = GetNode<Button>("%RestButton");
        skipButton = GetNode<Button>("%SkipButton");
        hintLabel = GetNode<Label>("%HintLabel");

        quitButton.Pressed += OnQuitPressed;
        deckButton.Pressed += OnDeckPressed;
        restButton.Pressed += OnRestPressed;
        skipButton.Pressed += OnSkipPressed;

        if (RunManager.CurrentRun == null)
        {
            deckButton.Disabled = true;
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            return;
        }

        BuildPath();
    }

    private void BuildPath()
    {
        RunData run = RunManager.CurrentRun;
        pathCanvas.Render(run, OnNodePressed);

        RunData.PlayerSaveState p = run.PlayerState;
        int currentRow = run.CurrentRow;
        hintLabel.Text = currentRow >= run.Path.Count ? "Map complete" : $"Step {currentRow + 1}/{run.Path.Count}  HP {p.Health}/{p.MaxHealth}";
    }

    /// <summary>
    /// Opens the shared deck viewer for the run piles. No event bus: OnDeckViewPress
    /// only exists inside the Encounter lifecycle. PendingRewardCards are excluded —
    /// they are not owned by the player until the reward is picked.
    /// </summary>
    private void OnDeckPressed()
    {
        RunData run = RunManager.CurrentRun;
        if (run == null)
        {
            deckButton.Disabled = true;
            return;
        }
        deckOverlay.ToggleDeck(run.Deck, run.Hand, run.Discard);
    }

    private void OnNodePressed(int row, int col)
    {
        NodeType type = RunManager.CurrentRun.Path[row][col].Type;
        if (!RunManager.ChooseNode(row, col)) return;

        if (type == NodeType.Rest)
        {
            ShowRestPanel();
        }
        else
        {
            GetTree().ChangeSceneToFile("res://Scenes/Encounter.tscn");
        }
    }

    private void ShowRestPanel()
    {
        RunData.PlayerSaveState p = RunManager.CurrentRun.PlayerState;
        int healAmount = p.MaxHealth * 25 / 100;
        restHealthLabel.Text = $"HP {p.Health}/{p.MaxHealth}\nRest will heal {healAmount} HP (25%)";
        restPanel.Visible = true;
    }

    private void OnRestPressed()
    {
        RunManager.HealPlayer(25);
        CompleteAndRebuild();
    }

    private void OnSkipPressed()
    {
        CompleteAndRebuild();
    }

    private void CompleteAndRebuild()
    {
        restPanel.Visible = false;
        RunManager.CompleteCurrentNode();
        if (RunManager.CurrentRun == null)
        {
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            return;
        }
        BuildPath();
    }

    private void OnQuitPressed()
    {
        RunManager.SaveRun();
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}