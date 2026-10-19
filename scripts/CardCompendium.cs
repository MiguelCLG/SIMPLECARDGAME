using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CardCompendium : Control
{
    private CardGrid cardGrid;

    public override void _Ready()
    {
        cardGrid = GetNode<CardGrid>("%CardGrid");

        List<CardSaveData> cards = CardCatalog.All()
            .Select(CardSaveData.FromResource)
            .ToList();
        cardGrid.ShowCards(cards);
    }

    private void OnBackPressed() => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
}