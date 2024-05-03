using System;
using Godot;

public enum Intent
{
    Attack,
    Defend
};

public partial class Enemy : Character
{
    private Intent intentType = Intent.Attack;
    private int intentValue = 0;

    [Export]
    private string EnemyName = "";

    private ProgressBar healthBar;
    private Label healthLabel;
    private Label armorLabel;

    /*public Enemy(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }*/

    public override void _Ready()
    {
        healthBar = GetNode<ProgressBar>("HealthBar");
        healthBar.Value = Utils.ConvertToPercentage(Health, MaxHealth);
        healthLabel = GetNode<Label>("HealthBar/HealthLabel");
        healthLabel.Text = $"{Health}/{MaxHealth}";
        armorLabel = GetNode<Label>("ArmorLabel");
        armorLabel.Text = Armor.ToString();
    }

    public void SetIntent(Intent intent, int value)
    {
        intentType = intent;
        intentValue = value;
        GetNode<Label>("%IntentValue").Text = value.ToString();
    }

    public override void AddArmor(int armor)
    {
        Armor += armor;
        armorLabel.Text = Armor.ToString();
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
        healthBar.Value = Mathf.RoundToInt(Health * 100 / MaxHealth);
        if (Health <= 0)
        {
            Health = 0;
            // Dies
            EventRegistry.GetEventPublisher("OnEnemyDie").RaiseEvent(this);
            return;
        }
        healthLabel.Text = $"{Health}/{MaxHealth}";
    }

    public void PlayTurn(Player player)
    {
        if (player.Health <= 0)
            return;
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

    public void OnEnemyClick()
    {
        EventRegistry.GetEventPublisher("OnEnemyClick").RaiseEvent(this);
    }

    // Add other methods as needed
}
