using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class CardCatalogTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public void All_ReturnsCardsWithCoreFields()
        {
            List<CardResource> cards = CardCatalog.All();

            AssertThat(cards.Count > 0).IsTrue();
            foreach (CardResource card in cards)
            {
                AssertThat(card).IsNotNull();
                AssertThat(string.IsNullOrEmpty(card.CardName)).IsFalse();
                AssertThat(string.IsNullOrEmpty(card.EffectString)).IsFalse();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void All_IsSortedByName()
        {
            List<CardResource> cards = CardCatalog.All();

            for (int i = 1; i < cards.Count; i++)
            {
                AssertThat(string.CompareOrdinal(cards[i - 1].CardName, cards[i].CardName) <= 0).IsTrue();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void All_ContainsEveryCardResourceInCardsFolder()
        {
            List<CardResource> catalog = CardCatalog.All();

            string[] files = System.IO.Directory.GetFiles(
                Godot.ProjectSettings.GlobalizePath("res://Resources/Cards"),
                "*.tres"
            );

            AssertThat(catalog.Count).IsEqual(files.Length);
        }
    }
}