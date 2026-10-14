using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Renders the run's path graph: positions the planet buttons itself and draws the
/// connectors between each planet and the paths it can lead to.
///
/// This is a plain <see cref="Control"/> (not a container) so that <c>_Draw</c> shares the
/// same coordinate space as the buttons and renders behind them, and so the whole graph
/// scrolls together inside the parent <see cref="ScrollContainer"/>.
/// </summary>
public partial class PathCanvas : Control
{
    private const string RestTexturePath = "res://Images/inkscape_exports/planets/rest.png";

    [Export] public Color EdgeDefault = new Color(1, 1, 1, 0.16f);
    [Export] public Color EdgeReachable = new Color(0.55f, 0.85f, 1.0f, 0.9f);
    [Export] public Color EdgeTravelled = new Color(1.0f, 0.84f, 0.0f, 1.0f);

    [Export] public float NodeSize = 56f;
    [Export] public float HSpacing = 20f;
    [Export] public float VSpacing = 24f;

    private List<List<PathNode>> path = new();
    private readonly List<List<Button>> buttons = new();
    private int currentRow;
    private List<int> chosenColumns = new();

    /// <summary>Rebuilds the planet buttons for the given run and lays out the graph.</summary>
    public void Render(RunData run, Action<int, int> onPressed)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        buttons.Clear();

        path = run.Path;
        currentRow = run.CurrentRow;
        chosenColumns = run.ChosenColumns;

        for (int r = 0; r < path.Count; r++)
        {
            List<Button> row = new();
            for (int c = 0; c < path[r].Count; c++)
            {
                Button button = CreateNodeButton(path[r][c]);
                int captureRow = r;
                int captureCol = c;
                button.Pressed += () => onPressed(captureRow, captureCol);
                ApplyState(button, r, c, run);
                AddChild(button);
                row.Add(button);
            }
            buttons.Add(row);
        }

        int rows = path.Count;
        CustomMinimumSize = new Vector2(0, rows * NodeSize + Math.Max(0, rows - 1) * VSpacing);
        Relayout();
        // The ScrollContainer may not have resized us yet on the first frame.
        Callable.From(Relayout).CallDeferred();
    }

    public override void _Notification(int what)
    {
        if (what == (int)NotificationResized)
            Relayout();
    }

    private void Relayout()
    {
        if (buttons.Count == 0)
            return;

        float width = Size.X;
        if (width <= 0f && GetParent() is Control parent)
            width = parent.Size.X;
        float centerX = width * 0.5f;
        float rowHeight = NodeSize + VSpacing;
        float colSpacing = NodeSize + HSpacing;

        for (int r = 0; r < buttons.Count; r++)
        {
            int count = buttons[r].Count;
            for (int c = 0; c < count; c++)
            {
                float x = centerX + (c - (count - 1) * 0.5f) * colSpacing;
                float y = r * rowHeight;
                Button button = buttons[r][c];
                button.Position = new Vector2(x - NodeSize * 0.5f, y);
                button.Size = new Vector2(NodeSize, NodeSize);
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (path == null || buttons.Count == 0)
            return;

        // Pass 1: every possible connection, faint.
        for (int r = 0; r < path.Count - 1; r++)
        {
            for (int c = 0; c < path[r].Count; c++)
            {
                List<int> connections = path[r][c].Connections;
                if (connections == null)
                    continue;
                foreach (int nextCol in connections)
                {
                    if (nextCol < 0 || nextCol >= path[r + 1].Count)
                        continue;
                    DrawLine(Center(r, c), Center(r + 1, nextCol), EdgeDefault, 2f, true);
                }
            }
        }

        // Pass 2: the route already travelled, in gold.
        for (int r = 0; r < currentRow - 1 && r + 1 < chosenColumns.Count; r++)
        {
            int from = chosenColumns[r];
            int to = chosenColumns[r + 1];
            if (from < 0 || to < 0)
                continue;
            if (from >= buttons[r].Count || to >= buttons[r + 1].Count)
                continue;
            DrawLine(Center(r, from), Center(r + 1, to), EdgeTravelled, 3f, true);
        }

        // Pass 2b: extend the gold route from the last completed planet into the row the player
        // is currently choosing from, so the trail connects all the way to the current choice.
        int pendingFromRow = currentRow - 1;
        if (pendingFromRow >= 0 && pendingFromRow < path.Count - 1 && pendingFromRow < chosenColumns.Count)
        {
            int from = chosenColumns[pendingFromRow];
            if (from >= 0 && from < buttons[pendingFromRow].Count)
            {
                for (int to = 0; to < path[currentRow].Count; to++)
                {
                    if (!RunManager.IsSelectable(currentRow, to))
                        continue;
                    DrawLine(Center(pendingFromRow, from), Center(currentRow, to), EdgeTravelled, 3f, true);
                }
            }
        }

        // Pass 3: where the currently selectable planets can lead, emphasized.
        if (currentRow >= 0 && currentRow < path.Count - 1)
        {
            for (int c = 0; c < path[currentRow].Count; c++)
            {
                if (!RunManager.IsSelectable(currentRow, c))
                    continue;
                List<int> connections = path[currentRow][c].Connections;
                if (connections == null)
                    continue;
                foreach (int nextCol in connections)
                {
                    if (nextCol < 0 || nextCol >= path[currentRow + 1].Count)
                        continue;
                    DrawLine(Center(currentRow, c), Center(currentRow + 1, nextCol), EdgeReachable, 3f, true);
                }
            }
        }
    }

    private Vector2 Center(int row, int col)
    {
        Button button = buttons[row][col];
        return button.Position + button.Size * 0.5f;
    }

    private Button CreateNodeButton(PathNode node)
    {
        Button button = new();
        button.Icon = GetNodeTexture(node);
        button.IconAlignment = HorizontalAlignment.Center;
        button.ExpandIcon = true;
        button.CustomMinimumSize = new Vector2(NodeSize, NodeSize);
        button.Size = new Vector2(NodeSize, NodeSize);
        button.ThemeTypeVariation = "PathNodeButton";
        button.FocusMode = Control.FocusModeEnum.None;
        return button;
    }

    private static Texture2D GetNodeTexture(PathNode node)
    {
        if (!string.IsNullOrEmpty(node.DistributionPath))
        {
            var dist = ResourceCache.Load<PathDistribution>(node.DistributionPath);
            if (dist != null && dist.Planet != null)
                return dist.Planet;
        }
        return ResourceLoader.Load<Texture2D>(RestTexturePath);
    }

    private static void ApplyState(Button button, int row, int col, RunData run)
    {
        if (row < run.CurrentRow)
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
    }
}
