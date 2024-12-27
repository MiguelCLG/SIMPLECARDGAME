using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;
using static Utils;

public partial class Player : Character
{
    public Array<Card> Deck { get; set; }
    public Array<Card> Hand { get; set; }
    public Array<Card> DiscardPile { get; set; }

    [Export]
    public int MaxMana { get; set; }

    [Export]
    public int Mana { get; set; }

    //private ProgressBar healthBar;
    private Label healthLabel;
    private Label manaLabel;
    private Label armorLabel;
    private ProgressBar healthBar;
    private ProgressBar manaBar;
    private PackedScene cardScene = GD.Load<PackedScene>("res://Scenes/card.tscn");
    private GridContainer handContainer;

    /*public Player(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }*/

    public override void _Ready()
    {
        handContainer = GetNode<GridContainer>("%HandContainer");
        armorLabel = GetNode<Label>("%ArmorLabel");
        healthLabel = GetNode<Label>("%HealthLabel");
        manaLabel = GetNode<Label>("%ManaLabel");
        healthBar = GetNode<ProgressBar>("%HealthBar");
        manaBar = GetNode<ProgressBar>("%ManaBar");
        healthBar.MaxValue = MaxHealth;
        manaBar.MaxValue = MaxMana;
        healthBar.Value = Health;
        manaBar.Value = Mana;

        healthLabel.Text = $"{Health}/{MaxHealth}";
        manaLabel.Text = $"{Mana}/{MaxMana}";
        armorLabel.Text = Armor.ToString();

        Hand = new();
        DiscardPile = new();
        Deck = new();
    }

    public void ResetPlayer()
    {
        SetHealthToMax();
        SetManaToMax();
        Armor = 0;
        armorLabel.Text = Armor.ToString();
        foreach (var child in handContainer.GetChildren())
        {
            handContainer.RemoveChild(child);
        }
        foreach (var card in Hand)
        {
            card.GetParent()?.RemoveChild(card);
            card.QueueFree();
        }
        foreach (var card in DiscardPile)
        {
            card.GetParent()?.RemoveChild(card);
            card.QueueFree();
        }
        foreach (var card in Deck)
        {
            card.GetParent()?.RemoveChild(card);
            card.QueueFree();
        }
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
    }

    public void RefreshHand()
    {
        if (Hand.Count > 0)
        {
            foreach (Card card in Hand)
            {
                DiscardCard(card);
            }
        }
        Hand = new();
        DrawCard();
        DrawCard();
        DrawCard();
    }

    public override void DrawCard()
    {
        // Implement drawing card logic
        if (Deck.Count <= 0)
        {
            Deck = DiscardPile;
            DiscardPile = new();
            Shuffle();
        }
        if (Hand.Count >= 5)
        {
            Debug.Print("Hand Size Full");
            return;
        }
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
        Hand.Add(cardInstance);
    }

    public void DiscardCard(Card card)
    {
        // Implement discarding card logic
        DiscardPile.Add(card);
        handContainer.RemoveChild(card);
    }

    public void PlayCard(Card card, Array<Character> targets)
    {
        if (Mana >= card.Cost)
        {
            Hand.Remove(card);
            DiscardCard(card);
            if (!card.Effect.isMultipleTargets)
                card.Effect.ApplyEffect(targets.FirstOrDefault());
            else card.Effect.ApplyEffect(targets);
            Mana -= card.Cost;
            manaLabel.Text = $"{Mana}/{MaxMana}";
            manaBar.Value = Mana;
        }
        else
        {
            Debug.Print("Not enough mana!");
        }
    }

    public void Shuffle()
    {
        System.Random rng = new();
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
            EventRegistry.GetEventPublisher("OnPlayerDie").RaiseEvent(this);
        }
        healthBar.Value = Health;
        healthLabel.Text = $"{Health}/{MaxHealth}";
    }

    public override void AddArmor(int armor)
    {
        Armor += armor;
        armorLabel.Text = Armor.ToString();
    }

    public override void AddMana(int mana)
    {
        Mana += mana;
        manaLabel.Text = $"{Mana}/{MaxMana}";
        manaBar.Value = Mana;
    }

    public void SetManaToMax()
    {
        Mana = MaxMana;
        manaLabel.Text = $"{Mana}/{MaxMana}";
        manaBar.Value = Mana;
    }

    public void SetHealthToMax()
    {
        Health = MaxHealth;
        healthLabel.Text = $"{Health}/{MaxHealth}";
        healthBar.Value = MaxHealth;
    }

    public void OnEndTurnPress()
    {
        GetNode<Button>("%EndTurnButton").ReleaseFocus();
        GetNode<Button>("%EndTurnButton").Disabled = true;
        EventRegistry.GetEventPublisher("OnEndTurnPress").RaiseEvent(this);
    }
}
