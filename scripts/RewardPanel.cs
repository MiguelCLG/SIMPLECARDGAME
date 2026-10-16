using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Full-screen overlay shown after winning a normal encounter. Offers the generated
/// reward cards plus a Skip button. Lives in the Encounter scene (mirrors PathMap's
/// RestPanel pattern) so the map row is only advanced once a choice is made.
/// </summary>
public partial class RewardPanel : Panel
{
    private HBoxContainer rewardCardContainer;
    private Button rewardSkipButton;
    private PackedScene cardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");

    private readonly List<Card> spawnedCards = new();
    private Action<CardSaveData> onChosen;
    private bool resolved;

    public override void _Ready()
    {
        rewardCardContainer = GetNode<HBoxContainer>("%RewardCardContainer");
        rewardSkipButton = GetNode<Button>("%RewardSkipButton");
        rewardSkipButton.Pressed += OnSkipPressed;
        Visible = false;
    }

    /// <summary>
    /// Shows the offered reward cards. <paramref name="chosen"/> is invoked once with the
    /// selected card, or with <c>null</c> when the player skips.
    /// </summary>
    public void Present(List<CardSaveData> rewards, Action<CardSaveData> chosen)
    {
        onChosen = chosen;
        resolved = false;
        ClearCards();

        if (rewards != null)
        {
            foreach (CardSaveData data in rewards)
            {
                if (data == null) continue;
                Card card = cardScene.Instantiate<Card>();
                // Reward cards must not run the global play-card flow (OnCardClick).
                card.RaiseCardClick = false;
                ApplyCardData(card, data);
                CardSaveData selected = data;
                card.Pressed += () => OnRewardCardPressed(selected);
                rewardCardContainer.AddChild(card);
                spawnedCards.Add(card);
            }
        }

        Visible = true;
    }

    private void OnRewardCardPressed(CardSaveData card)
    {
        if (resolved) return;
        resolved = true;
        Visible = false;
        onChosen?.Invoke(card);
    }

    private void OnSkipPressed()
    {
        if (resolved) return;
        resolved = true;
        Visible = false;
        onChosen?.Invoke(null);
    }

    private void ClearCards()
    {
        foreach (Card card in spawnedCards)
        {
            if (IsInstanceValid(card)) card.QueueFree();
        }
        spawnedCards.Clear();
        if (rewardCardContainer != null)
        {
            foreach (Node child in rewardCardContainer.GetChildren())
                rewardCardContainer.RemoveChild(child);
        }
    }

    private static void ApplyCardData(Card card, CardSaveData data)
    {
        Texture2D cardImage = string.IsNullOrEmpty(data.CardImage)
            ? null
            : ResourceLoader.Load<Texture2D>(data.CardImage);
        card.CardName = data.CardName;
        card.CardImage = cardImage;
        card.Description = data.Description;
        card.Cost = data.Cost;
        card.EffectString = data.EffectString;
        card.Value = data.Value;
        card.Amount = data.Amount;
        card.isTargetingSelf = data.isTargetingSelf;
        card.isMultipleTargets = data.isMultipleTargets;
        card.InitializeEffect();
    }
}
