using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class PathMap : Control
{
    [Export]
    private Array<Texture2D> nodeTextures;
    private VBoxContainer pathContainer;
    private Panel restPanel;
    private Label restHealthLabel;
    private Button quitButton;
    private Button restButton;
    private Button skipButton;
    private Label hintLabel;

    public override void _Ready()
    {
        pathContainer = GetNode<VBoxContainer>("%PathContainer");
        restPanel = GetNode<Panel>("%RestPanel");
        restHealthLabel = GetNode<Label>("%RestHealthLabel");
        quitButton = GetNode<Button>("%QuitButton");
        restButton = GetNode<Button>("%RestButton");
        skipButton = GetNode<Button>("%SkipButton");
        hintLabel = GetNode<Label>("%HintLabel");

        quitButton.Pressed += OnQuitPressed;
        restButton.Pressed += OnRestPressed;
        skipButton.Pressed += OnSkipPressed;

        if (RunManager.CurrentRun == null)
        {
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            return;
        }

        BuildPath();
    }

    private void BuildPath()
    {
        foreach (Node child in pathContainer.GetChildren())
        {
            pathContainer.RemoveChild(child);
            child.QueueFree();
        }

        RunData run = RunManager.CurrentRun;
        for (int row = 0; row < run.Path.Count; row++)
        {
            GD.Print("Refresh Textures", nodeTextures.Count);
            Array<Texture2D> rowTextures = new Array<Texture2D>();
            rowTextures.AddRange(nodeTextures);
            HBoxContainer rowBox = new();
            rowBox.Alignment = BoxContainer.AlignmentMode.Center;
            rowBox.AddThemeConstantOverride("separation", 16);
            rowBox.MouseFilter = Control.MouseFilterEnum.Ignore;
            pathContainer.AddChild(rowBox);

            List<PathNode> nodes = run.Path[row];
            for (int col = 0; col < nodes.Count; col++)
            {
                PathNode node = nodes[col];
                int random = new RandomNumberGenerator().RandiRange(0, rowTextures.Count - 1);
                Button button;
                if (node.Type == NodeType.Rest)
                {
                    Texture2D RestTexture = ResourceLoader.Load<Texture2D>("res://Images/inkscape_exports/planets/rest.png");
                    button = CreateNodeButton(node, RestTexture);
                }
                else
                {
                    Texture2D nodeTexture = rowTextures[random];
                    rowTextures.Remove(nodeTexture);
                    button = CreateNodeButton(node, nodeTexture);
                }
                bool completed = row < run.CurrentRow;
                if (completed)
                {
                    button.Disabled = true;
                    bool chosen = col == run.ChosenColumns[row];
                    button.Modulate = chosen ? Colors.Gold : new Color(1, 1, 1, 0.75f);
                }
                else if (row == run.CurrentRow)
                {
                    bool selectable = RunManager.IsSelectable(row, col);
                    button.Disabled = !selectable;
                    if (!selectable)
                        button.Modulate = new Color(1, 1, 1, 0.75f);
                }
                else
                {
                    button.Disabled = true;
                    button.Modulate = new Color(1, 1, 1, 0.75f);
                }

                int captureRow = row;
                int captureCol = col;
                button.Pressed += () => OnNodePressed(captureRow, captureCol);
                rowBox.AddChild(button);
            }
        }

        RunData.PlayerSaveState p = run.PlayerState;
        int currentRow = run.CurrentRow;
        hintLabel.Text = currentRow >= run.Path.Count ? "Map complete" : $"Step {currentRow + 1}/{run.Path.Count}  HP {p.Health}/{p.MaxHealth}";
    }

    private Button CreateNodeButton(PathNode node, Texture2D nodeTexture)
    {
        Button button = new();
        button.Icon = nodeTexture;
        button.IconAlignment = HorizontalAlignment.Center;
        button.ExpandIcon = true;
        button.CustomMinimumSize = new Vector2(56, 56);
        button.ThemeTypeVariation = "PathNodeButton";
        button.FocusMode = Control.FocusModeEnum.None;

        return button;
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