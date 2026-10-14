using Godot;

[GlobalClass]
public partial class ActConfig : Resource
{
    [Export] public string ActName { get; set; }
    [Export] public float IntentMultiplier { get; set; } = 1f;
}