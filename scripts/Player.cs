using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;
using static Utils;

public partial class Player : Character
{
    public List<CardSaveData> Deck { get; set; }
    public Array<Card> Hand { get; set; }
    public List<CardSaveData> DiscardPile { get; set; }

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

    public void ClearCards()
    {
        Hand = new();
        DiscardPile = new();
        Deck = new();
    }

    public override void _ExitTree()
    {
        ClearCards();
    }

    public RunData.PlayerSaveState CollectState()
    {
        return new RunData.PlayerSaveState
        {
            Health = Health,
            MaxHealth = MaxHealth,
            Armor = Armor,
            Mana = Mana,
            MaxMana = MaxMana
        };
    }

    public void ApplyState(RunData.PlayerSaveState state)
    {
        if (state == null) return;
        Health = state.Health;
        MaxHealth = state.MaxHealth;
        Armor = state.Armor;
        Mana = state.Mana;
        MaxMana = state.MaxMana;
        healthBar.MaxValue = MaxHealth;
        manaBar.MaxValue = MaxMana;
        healthBar.Value = Health;
        manaBar.Value = Mana;
        healthLabel.Text = $"{Health}/{MaxHealth}";
        manaLabel.Text = $"{Mana}/{MaxMana}";
        armorLabel.Text = Armor.ToString();
    }

    public void CollectCards(out List<CardSaveData> deck, out List<CardSaveData> hand, out List<CardSaveData> discard)
    {
        deck = new List<CardSaveData>(Deck);
        hand = Hand.Select(CardSaveData.FromCard).ToList();
        discard = new List<CardSaveData>(DiscardPile);
    }

    public void SetDeck(List<CardSaveData> deckData)
    {
        ClearCards();
        Deck = deckData == null ? new() : new List<CardSaveData>(deckData);
    }

    public void SetHand(List<CardSaveData> handData)
    {
        if (handData == null) return;
        foreach (CardSaveData data in handData)
        {
            SpawnCard(data);
        }
    }

    public void SetDiscard(List<CardSaveData> discardData)
    {
        DiscardPile = discardData == null ? new() : new List<CardSaveData>(discardData);
    }

    public void StartEncounter()
    {
        if (Deck == null)
        {
            GD.PrintErr("Player has no deck");
            return;
        }
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
            Deck.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle();
        }
        if (Hand.Count >= 5)
        {
            Debug.Print("Hand Size Full");
            return;
        }
        CardSaveData data = Deck[0];
        Deck.RemoveAt(0);
        SpawnCard(data);
    }

    public void SpawnCard(CardSaveData data)
    {
        var cardInstance = cardScene.Instantiate<Card>();
        cardInstance.CardName = data.CardName;
        cardInstance.Description = data.Description;
        cardInstance.Cost = data.Cost;
        cardInstance.EffectString = data.EffectString;
        cardInstance.Value = data.Value;
        cardInstance.Amount = data.Amount;
        cardInstance.isTargetingSelf = data.isTargetingSelf;
        cardInstance.isMultipleTargets = data.isMultipleTargets;
        cardInstance.InitializeEffect();
        handContainer.AddChild(cardInstance);
        Hand.Add(cardInstance);
    }

    public void DiscardCard(Card card)
    {
        // Implement discarding card logic
        DiscardPile.Add(CardSaveData.FromCard(card));
        handContainer.RemoveChild(card);
        card.QueueFree();
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
            var stats = RunManager.CurrentRun?.Stats;
            if (stats != null) stats.CardsPlayed++;
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
        var stats = RunManager.CurrentRun?.Stats;
        if (stats != null) stats.DamageTaken += variableDamage;
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
