using Godot;
using System.Collections.Generic;

public partial class CardGrid : ScrollContainer
{
    private GridContainer grid;

    public override void _Ready() => grid = GetNode<GridContainer>("%CardGrid");

    public void ShowCards(IReadOnlyList<CardSaveData> cards)
    {
        foreach (Node child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }
        foreach (CardSaveData data in cards)
        {
            // Wrap each fixed-size card in an expanding cell so the grid splits the spare
            // width/height between cells instead of packing everything into the top left.
            CenterContainer cell = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            cell.AddChild(CardUi.CreateCardView(data));
            grid.AddChild(cell);
        }
    }

    public int CardCount => grid.GetChildCount();
}