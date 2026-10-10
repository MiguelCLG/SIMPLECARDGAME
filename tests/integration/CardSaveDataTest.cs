using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class CardSaveDataTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public void FromCard_CapturesAllFields()
        {
            var card = new Card
            {
                CardName = "Strike",
                Description = "Deal damage",
                Cost = 1,
                EffectString = "Attack",
                Value = 6,
                Amount = 1,
                isTargetingSelf = false,
                isMultipleTargets = false
            };

            CardSaveData data = CardSaveData.FromCard(card);

            AssertThat(data.CardName).IsEqual("Strike");
            AssertThat(data.Description).IsEqual("Deal damage");
            AssertThat(data.Cost).IsEqual(1);
            AssertThat(data.EffectString).IsEqual("Attack");
            AssertThat(data.Value).IsEqual(6);
            AssertThat(data.Amount).IsEqual(1);
            AssertThat(data.isTargetingSelf).IsFalse();
            AssertThat(data.isMultipleTargets).IsFalse();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void FromCard_FallsBackToEffectClassName()
        {
            var card = new Card
            {
                EffectString = "",
                Effect = new AttackEffect()
            };

            CardSaveData data = CardSaveData.FromCard(card);

            AssertThat(data.EffectString).IsEqual("Attack");
        }

        [TestCase]
        [RequireGodotRuntime]
        public void ToCard_InitializesMatchingEffect()
        {
            var data = new CardSaveData
            {
                CardName = "Strike",
                Description = "Deal damage",
                Cost = 1,
                EffectString = "Attack",
                Value = 6,
                Amount = 1
            };

            Card card = data.ToCard();

            AssertThat(card.Effect).IsInstanceOf<AttackEffect>();
            AssertThat(card.Effect.Value).IsEqual(6);
            AssertThat(card.Effect.Amount).IsEqual(1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void RoundTrip_PreservesFields()
        {
            var card = new Card
            {
                CardName = "Defend",
                Description = "Block",
                Cost = 1,
                EffectString = "Defense",
                Value = 5,
                Amount = 1
            };

            Card restored = CardSaveData.FromCard(card).ToCard();

            AssertThat(restored.CardName).IsEqual("Defend");
            AssertThat(restored.Description).IsEqual("Block");
            AssertThat(restored.Cost).IsEqual(1);
            AssertThat(restored.Effect).IsInstanceOf<DefenseEffect>();
            AssertThat(restored.Effect.Value).IsEqual(5);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void InitializeEffect_MapsEveryEffectString()
        {
            AssertThat(MakeCard("Attack").Effect).IsInstanceOf<AttackEffect>();
            AssertThat(MakeCard("Defense").Effect).IsInstanceOf<DefenseEffect>();
            AssertThat(MakeCard("Draw").Effect).IsInstanceOf<DrawEffect>();
            AssertThat(MakeCard("Mana").Effect).IsInstanceOf<ManaEffect>();
            AssertThat(MakeCard("Cleave").Effect).IsInstanceOf<CleaveEffect>();
            AssertThat(MakeCard("Poison").Effect).IsInstanceOf<PoisonEffect>();
        }

        private static Card MakeCard(string effectString)
        {
            var card = new Card { EffectString = effectString };
            card.InitializeEffect();
            return card;
        }
    }
}