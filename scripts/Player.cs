using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Player : Character
{
    public List<Card> Deck { get; set; }
    public List<Card> Hand { get; set; }
    public List<Card> DiscardPile { get; set; }
    public int Mana { get; set; }
    private PackedScene cardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");
    private HBoxContainer handContainer;

    /*public Player(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }*/

    public override void _Ready()
    {
        handContainer = GetNode<HBoxContainer>("%HandContainer");
        Hand = new();
        DiscardPile = new();
        Deck = new();
    }

    public void StartEncounter()
    {
        if (Deck == null)
        {
            GD.PrintErr("Player has no deck");
            return;
        }
        DrawCard();
        DrawCard();
        DrawCard();
    }

    public void DrawCard()
    {
        // Implement drawing card logic
        GD.Print(Deck.Count);
        if (Deck.Count <= 0)
        {
            Deck = DiscardPile;
            DiscardPile = new();
            Shuffle();
        }
        Hand.Add(Deck.First());
        SpawnCard(Deck.First());
        Deck.Remove(Deck.First());
    }

    public void SpawnCard(Card card)
    {
        var cardInstance = cardScene.Instantiate<Card>();
        cardInstance.CardName = card.CardName;
        cardInstance.Description = card.Description;
        cardInstance.Cost = card.Cost;
        cardInstance.Effect = card.Effect;
        handContainer.AddChild(cardInstance);
    }

    public void DiscardCard(Card card)
    {
        // Implement discarding card logic
        DiscardPile.Add(card);
    }

    public void PlayCard(Card card, Enemy enemy)
    {
        card.Effect.ApplyEffect(this, enemy);
        Hand.Remove(card);
        DiscardCard(card);
    }

    public void Shuffle()
    {
        Random rng = new();
        int n = Deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (Deck[n], Deck[k]) = (Deck[k], Deck[n]);
        }
    }

    public override void TakeDamage(int damage)
    {
        int variableDamage = damage;
        if (Armor > 0)
        {
            if (Armor < damage)
            {
                variableDamage = damage - Armor;
                Armor = 0;
            }
            else
            {
                Armor -= damage;
                variableDamage = 0;
            }
        }
        Health -= variableDamage;
        if (Health <= 0)
        {
            Health = 0;
            // game over
            return;
        }
        // signal
    }

    public override void AddArmor(int armor)
    {
        Armor += armor;
    }
}
