using Godot;
using System.Collections.Generic;
using System.Linq;

public static class CardCatalog
{
    private const string CatalogPath = "res://Resources/CardCatalog.tres";

    public static List<CardResource> All()
    {
        CardCatalogResource catalog = ResourceCache.Load<CardCatalogResource>(CatalogPath);
        if (catalog?.cards == null) return new List<CardResource>();
        return catalog.cards
            .Where(c => c != null)
            .OrderBy(c => c.CardName)
            .ToList();
    }
}