using Godot;
using System.Collections.Generic;

public partial class CardGrid : ScrollContainer
{
    private GridContainer grid;

    public override void _Ready() => grid = GetNode<GridContainer>("%CardGrid");

    public void Show(IReadOnlyList<CardSaveData> cards)
    {
        foreach (Node child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }
        foreach (CardSaveData data in cards)
        {
            grid.AddChild(CardUi.CreateCardView(data));
        }
    }

    public int CardCount => grid.GetChildCount();
}