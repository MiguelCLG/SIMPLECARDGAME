using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardCatalogResource : Resource
{
    [Export] public Array<CardResource> cards = new();
}