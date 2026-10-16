using Godot;
using Godot.Collections;

/// <summary>
/// A pool of cards an encounter can offer as a reward. Kept as a resource so future
/// class/act systems can supply their own pools (via <see cref="ClassName"/> or by
/// wiring a different .tres) without changing code.
/// </summary>
[GlobalClass]
public partial class CardPoolResource : Resource
{
    [Export] public Array<CardResource> cards = new Array<CardResource>();

    /// <summary>Optional identifier for future class-specific pools. Unused for now.</summary>
    [Export] public string ClassName { get; set; } = "Default";
}
