using Godot;

[GlobalClass]
public partial class EnemyResource : Resource
{
  [Export] public string EnemyName { get; set; }
  [Export] public Texture Texture { get; set; }
  [Export] public int MinHealth { get; set; }
  [Export] public int MaxHealth { get; set; }
  [Export] public int Armor { get; set; }
  [Export] public int MaxIntent { get; set; }
  [Export] public int MinIntent { get; set; }
}
