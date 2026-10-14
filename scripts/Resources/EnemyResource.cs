using Godot;

[GlobalClass]
public partial class EnemyResource : Resource
{
  [Export] public string EnemyName { get; set; }
  [Export] public Texture2D Texture { get; set; }
  [Export] public int MinHealth { get; set; }
  [Export] public int MaxHealth { get; set; }
  [Export] public int Armor { get; set; }
  [Export] public int AttackMin { get; set; }
  [Export] public int AttackMax { get; set; }
  [Export] public int AttackGrowthPerStep { get; set; }
  [Export] public int DefendMin { get; set; }
  [Export] public int DefendMax { get; set; }
  [Export] public int DefendGrowthPerStep { get; set; }
}
