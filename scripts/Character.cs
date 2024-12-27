using System.Collections.Generic;
using Godot;

public abstract partial class Character : Control
{
    [Export]
    public int Health { get; set; }

    [Export]
    public int MaxHealth { get; set; }

    [Export]
    public int Armor { get; set; }
    public List<CardEffect> TurnEffects { get; set; }

    // Constructor
    /*protected Character(int health, int maxHealth, int armor = 0)
    {
        Health = health;
        MaxHealth = maxHealth;
        Armor = armor;
    }*/

    // Method to take damage
    public abstract void TakeDamage(int damage);
    public abstract void AddArmor(int armor);
    public abstract void AddMana(int mana);
    public abstract void DrawCard();

}
