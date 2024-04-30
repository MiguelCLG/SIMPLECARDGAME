using System;

public enum Intent
{
    Attack,
    Defend
};

public partial class Enemy : Character
{
    private Intent intentType = Intent.Attack;
    private int intentValue = 0;

    public Enemy(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }

    public void SetIntent(Intent intent, int value)
    {
        intentType = intent;
        intentValue = value;
    }

    public override void AddArmor(int armor)
    {
        Armor += armor;
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
            // Dies
            return;
        }
    }

    public void PlayTurn(Player player)
    {
        Random rng = new();

        if (intentType == Intent.Attack)
        {
            AttackPlayer(player);
        }
        else if (intentType == Intent.Defend)
            AddArmor(rng.Next(5));
    }

    private void AttackPlayer(Player player)
    {
        player.TakeDamage(intentValue);
    }

    // Add other methods as needed
}
