using Godot;

public abstract partial class Character : Node2D
{
    [Export]
    public int Health { get; set; }

    [Export]
    public int MaxHealth { get; set; }

    [Export]
    public int Armor { get; set; }

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
}
