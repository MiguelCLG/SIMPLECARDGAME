using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DeckResource : Resource
{
  [Export] public Array<CardResource> cards = new Array<CardResource>();
}
