using System;
using System.Collections.Generic;
using System.Linq;

public partial class Player : Character
{
    public List<Card> Deck { get; set; }
    public List<Card> Hand { get; set; }
    public List<Card> DiscardPile { get; set; }
    public int Mana { get; set; }

    public Player(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }

    public void DrawCard()
    {
        // Implement drawing card logic
        if (Deck.Count <= 0)
        {
            Deck = DiscardPile;
            DiscardPile = new();
            Shuffle();
        }
        Hand.Add(Deck.First());
        Deck.Remove(Deck.First());
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
