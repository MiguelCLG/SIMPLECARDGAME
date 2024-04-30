using Godot;

public partial class Card : CanvasLayer
{
    [Export]
    public string CardName { get; set; }

    [Export]
    public string Description { get; set; }

    [Export]
    public int Cost { get; set; }
    public CardEffect Effect { get; set; }
}
