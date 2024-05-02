using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public partial class Player : Character
{
    public List<Card> Deck { get; set; }
    public List<Card> Hand { get; set; }
    public List<Card> DiscardPile { get; set; }

    [Export]
    public int MaxMana { get; set; }

    [Export]
    public int Mana { get; set; }

    private ProgressBar healthBar;
    private ProgressBar manaBar;
    private Label armorLabel;
    private PackedScene cardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");
    private HBoxContainer handContainer;

    /*public Player(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }*/

    public override void _Ready()
    {
        handContainer = GetNode<HBoxContainer>("%HandContainer");
        healthBar = GetNode<ProgressBar>("PlayerUI/HealthBar");
        manaBar = GetNode<ProgressBar>("PlayerUI/ManaBar");
        armorLabel = GetNode<Label>("%ArmorLabel");

        healthBar.Value = Utils.ConvertToPercentage(Health, MaxHealth);
        manaBar.Value = Mana;
        manaBar.MaxValue = MaxMana;
        armorLabel.Text = Armor.ToString();

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
        Shuffle();
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
        handContainer.RemoveChild(card);
    }

    public void PlayCard(Card card, Enemy enemy)
    {
        if (Mana >= card.Cost)
        {
            card.Effect.ApplyEffect(this, enemy);
            Hand.Remove(card);
            DiscardCard(card);
            Mana -= card.Cost;
            manaBar.Value = Mana;
        }
        else
        {
            Debug.Print("Not enough mana!");
        }
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
            armorLabel.Text = Armor.ToString();
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
        armorLabel.Text = Armor.ToString();
    }

    public void SetManaToMax()
    {
        Mana = MaxMana;
        manaBar.Value = Mana;
    }

    public void OnEndTurnPress()
    {
        EventRegistry.GetEventPublisher("OnEndTurnPress").RaiseEvent(this);
    }
}
