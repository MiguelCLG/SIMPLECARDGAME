using System;
using System.Collections.Generic;
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
    public int attackMinValue = 0;
    public int attackMaxValue = 0;
    public int defendMinValue = 0;
    public int defendMaxValue = 0;
    private int intentValue = 0;

    [Export]
    public string EnemyName = "";
    public string SpritePath = "";
    private ProgressBar healthBar;
    private ProgressBar armorBar;
    private Label healthLabel;
    private Label armorLabel;
    private TextureRect sprite;
    public Texture2D texture;

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
        sprite.Texture = texture;
    }

    public void SetIntent(Intent intent, int value)
    {
        intentType = intent;
        intentValue = value;
        ApplyIntentVisuals();
    }

    public void ApplyIntentVisuals()
    {
        if (!IsInsideTree()) return;
        var intentImage = GetNode<TextureRect>("%IntentImage");
        if (intentType == Intent.Attack)
        {
            intentImage.Texture = GD.Load<Texture2D>("res://Images/Icons/sword.png");
            intentImage.Modulate = Color.Color8(255, 25, 85);
            GetNode<Label>("%IntentValue").AddThemeColorOverride("font_color", Color.Color8(255, 25, 85));
        }
        else
        {
            intentImage.Texture = GD.Load<Texture2D>("res://Images/Icons/shield.png");
            intentImage.Modulate = Color.Color8(165, 208, 255);
            GetNode<Label>("%IntentValue").AddThemeColorOverride("font_color", Color.Color8(165, 208, 255));
        }
        GetNode<Label>("%IntentValue").Text = intentValue.ToString();
    }

    public EnemySaveData CollectState()
    {
        return new EnemySaveData
        {
            EnemyName = EnemyName,
            TexturePath = texture?.ResourcePath,
            Health = Health,
            MaxHealth = MaxHealth,
            Armor = Armor,
            AttackMin = attackMinValue,
            AttackMax = attackMaxValue,
            DefendMin = defendMinValue,
            DefendMax = defendMaxValue,
            IntentType = intentType,
            IntentValue = intentValue
        };
    }

    public void ApplyState(EnemySaveData data)
    {
        if (data == null) return;
        EnemyName = data.EnemyName;
        texture = GD.Load<Texture2D>(data.TexturePath);
        Health = data.Health;
        MaxHealth = data.MaxHealth;
        Armor = data.Armor;
        attackMinValue = data.AttackMin;
        attackMaxValue = data.AttackMax;
        defendMinValue = data.DefendMin;
        defendMaxValue = data.DefendMax;
        intentType = data.IntentType;
        intentValue = data.IntentValue;
    }

    public override void AddArmor(int armor)
    {
        Armor += armor;
        armorLabel.Text = Armor.ToString();
        armorBar.MaxValue = Armor > 0 ? Armor : 1;
        armorBar.Value = Armor;
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
            armorBar.MaxValue = Armor > 0 ? Armor : 1;
            armorBar.Value = Armor;
        }
        Health -= variableDamage;
        var stats = RunManager.CurrentRun?.Stats;
        if (stats != null) stats.DamageDealt += variableDamage;
        healthBar.Value = Health;
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

    public override void AddMana(int mana)
    {
        throw new NotImplementedException();
    }

    public override void DrawCard()
    {
        throw new NotImplementedException();
    }

    // Add other methods as needed

}
