using System;
using Godot;

public enum Intent
{
    Attack,
    Defend
};

public class EnemyDTO
{
    public string EnemyName { get; set; }
    public string Sprite { get; set; }
    public int MinHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Armor { get; set; }
    public int MaxIntent { get; set; }
    public int MinIntent { get; set; }
}

public partial class Enemy : Character
{
    private Intent intentType = Intent.Attack;
    public int intentMinValue = 0;
    public int intentMaxValue = 0;
    private int intentValue = 0;

    [Export]
    public string EnemyName = "";
    public string SpritePath = "";
    private ProgressBar healthBar;
    private ProgressBar armorBar;
    private Label healthLabel;
    private Label armorLabel;
    private TextureRect sprite;

    /*public Enemy(int health, int maxHealth, int armor)
        : base(health, maxHealth, armor) { }*/

    public override void _Ready()
    {
        sprite = GetNode<TextureRect>("%EnemyImage");
        healthBar = GetNode<ProgressBar>("%HealthBar");
        healthBar.Value = Health;
        healthBar.MaxValue = MaxHealth;
        healthLabel = GetNode<Label>("%HealthLabel");
        healthLabel.Text = $"{Health}/{MaxHealth}";
        armorBar = GetNode<ProgressBar>("%ArmorBar");
        armorBar.Value = Armor;
        armorBar.MaxValue = Armor == 0 ? 1 : Armor;
        armorLabel = GetNode<Label>("%ArmorLabel");
        armorLabel.Text = Armor.ToString();
        InitializeEnemy();
    }

    public void InitializeEnemy()
    {
        sprite.Texture = GD.Load<Texture2D>(SpritePath);
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

        if (intentType == Intent.Attack)
        {
            AttackPlayer(player);
        }
        else if (intentType == Intent.Defend)
            AddArmor(intentValue);
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
