using System.Collections.Generic;
using Godot;

/// <summary>
/// Shared full-screen deck viewer (combat + PathMap). Resolves its own unique-name
/// children in _Ready (CardGrid.cs pattern), wires the close button itself, and is
/// driven only through the public API — consumers never % -lookup its internals.
/// Card order when rendering: Deck, Discard, Hand (matches the original inline
/// overlay in Encounter.tscn).
/// </summary>
public partial class DeckOverlay : Panel
{
    private CardGrid grid;
    private Label deckCountLabel;
    private Button closeButton;

    public bool IsOpen => Visible;

    public CardGrid Grid => grid;

    public Button CloseButton => closeButton;

    public override void _Ready()
    {
        grid = GetNode<CardGrid>("%CardGrid");
        deckCountLabel = GetNode<Label>("%DeckCountLabel");
        closeButton = GetNode<Button>("%DeckCloseButton");
        closeButton.Pressed += Hide;
    }

    /// <summary>
    /// Renders the three piles read-only and shows the overlay.
    /// </summary>
    public void ShowDeck(List<CardSaveData> draw, List<CardSaveData> hand, List<CardSaveData> discard)
    {
        int drawCount = draw?.Count ?? 0;
        int handCount = hand?.Count ?? 0;
        int discardCount = discard?.Count ?? 0;

        List<CardSaveData> all = new();
        if (draw != null) all.AddRange(draw);
        if (discard != null) all.AddRange(discard);
        if (hand != null) all.AddRange(hand);

        deckCountLabel.Text = $"Draw {drawCount} · Hand {handCount} · Discard {discardCount}";
        grid.ShowCards(all);
        Visible = true;
    }

    /// <summary>
    /// Hides the overlay when open, otherwise renders the piles and shows it.
    /// </summary>
    public void ToggleDeck(List<CardSaveData> draw, List<CardSaveData> hand, List<CardSaveData> discard)
    {
        if (Visible)
        {
            Hide();
            return;
        }
        ShowDeck(draw, hand, discard);
    }

    // Hide() comes from CanvasItem and is exactly Visible = false — it is part of the
    // public API by inheritance (declaring it again would shadow it, CS0108).
}
