using Godot;
using Godot.Collections;
public partial class PathDistribution : Resource
{
    [Export]
    public Texture2D Planet {get; set;} 
    [Export]
    public Texture2D PlanetBackground {get; set;}
    [Export]
    #nullable enable
    public Array<EnemyResource>? Enemies {get; set;} 
}