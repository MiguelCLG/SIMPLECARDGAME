using Godot;

[GlobalClass]
public partial class CardResource : Resource
{
  [Export] public string CardName { get; set; }
  [Export] public string Description { get; set; }
  [Export] public int Cost { get; set; }
  [Export] public int Value { get; set; }
  [Export] public string EffectString;
  [Export] public CardEffectType cardEffectType { get; set; }
}
