using System;
using System.Collections.Generic;
using GdUnit4;
using Newtonsoft.Json;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class RunDataSerializationTest
    {
        [TestCase]
        public void Serialize_Deserialize_PreservesRunState()
        {
            RunData run = BuildSampleRun();

            string json = JsonConvert.SerializeObject(run, Formatting.Indented);
            RunData loaded = JsonConvert.DeserializeObject<RunData>(json);

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.CurrentRow).IsEqual(4);
            AssertThat(loaded.ActiveEncounter).IsTrue();
            AssertThat(loaded.IsBossNode).IsFalse();

            AssertThat(loaded.ChosenColumns.Count).IsEqual(3);
            AssertThat(loaded.ChosenColumns[1]).IsEqual(2);

            AssertThat(loaded.PlayerState.Health).IsEqual(64);
            AssertThat(loaded.PlayerState.MaxHealth).IsEqual(100);
            AssertThat(loaded.PlayerState.Armor).IsEqual(7);
            AssertThat(loaded.PlayerState.Mana).IsEqual(1);
            AssertThat(loaded.PlayerState.MaxMana).IsEqual(3);

            AssertThat(loaded.Deck.Count).IsEqual(5);
            AssertThat(loaded.Deck[0].CardName).IsEqual("Strike");
            AssertThat(loaded.Deck[0].Cost).IsEqual(1);
            AssertThat(loaded.Deck[0].EffectString).IsEqual("Attack");

            AssertThat(loaded.Hand.Count).IsEqual(3);
            AssertThat(loaded.Hand[2].Description).IsEqual("Block");

            AssertThat(loaded.Discard.Count).IsEqual(2);

            AssertThat(loaded.Enemies.Count).IsEqual(2);
            AssertThat(loaded.Enemies[0].EnemyName).IsEqual("Wolf");
            AssertThat(loaded.Enemies[0].Health).IsEqual(33);
            AssertThat(loaded.Enemies[0].MaxHealth).IsEqual(40);
            AssertThat(loaded.Enemies[0].IntentType).IsEqual(Intent.Attack);
            AssertThat(loaded.Enemies[0].IntentValue).IsEqual(12);
            AssertThat(loaded.Enemies[1].IntentType).IsEqual(Intent.Defend);

            AssertThat(loaded.Path.Count).IsEqual(2);
            AssertThat(loaded.Path[1][0].Type).IsEqual(NodeType.Rest);

            AssertThat(loaded.Stats.Success).IsFalse();
            AssertThat(loaded.Stats.CardsPlayed).IsEqual(9);
            AssertThat(loaded.Stats.EnemiesDefeated).IsEqual(3);
        }

        [TestCase]
        public void Serialize_Deserialize_EmptyRunStillValid()
        {
            var run = new RunData();
            string json = JsonConvert.SerializeObject(run, Formatting.Indented);
            RunData loaded = JsonConvert.DeserializeObject<RunData>(json);

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.Path.Count).IsEqual(0);
            AssertThat(loaded.Enemies.Count).IsEqual(0);
        }

        private static RunData BuildSampleRun()
        {
            var run = new RunData
            {
                GeneratedAt = DateTime.Now,
                StartedAt = DateTime.Now,
                CurrentRow = 4,
                ChosenColumns = new List<int> { 1, 2, -1 },
                ActiveEncounter = true,
                IsBossNode = false,
                PlayerState = new RunData.PlayerSaveState
                {
                    Health = 64,
                    MaxHealth = 100,
                    Armor = 7,
                    Mana = 1,
                    MaxMana = 3
                },
                Path = new List<List<PathNode>>
                {
                    new List<PathNode>
                    {
                        new PathNode { Row = 0, Column = 0, Type = NodeType.Encounter },
                        new PathNode { Row = 0, Column = 1, Type = NodeType.Encounter }
                    },
                    new List<PathNode>
                    {
                        new PathNode { Row = 1, Column = 0, Type = NodeType.Rest }
                    }
                }
            };

            run.Deck = new List<CardSaveData>
            {
                Card("Strike", "Deal damage", 1, "Attack"),
                Card("Strike", "Deal damage", 1, "Attack"),
                Card("Defend", "Block", 1, "Defense"),
                Card("Bash", "Strike", 3, "Attack"),
                Card("Cleave", "Hit all", 2, "Cleave")
            };

            run.Hand = new List<CardSaveData>
            {
                Card("Strike", "Deal damage", 1, "Attack"),
                Card("Strike", "Deal damage", 1, "Attack"),
                Card("Defend", "Block", 1, "Defense")
            };

            run.Discard = new List<CardSaveData>
            {
                Card("Poison", "Poison", 1, "Poison"),
                Card("Mana", "Gain mana", 0, "Mana")
            };

            run.Enemies = new List<EnemySaveData>
            {
                new EnemySaveData
                {
                    EnemyName = "Wolf",
                    TexturePath = "res://Resources/Enemy/Wolf.tres",
                    Health = 33,
                    MaxHealth = 40,
                    Armor = 0,
                    AttackMin = 6,
                    AttackMax = 10,
                    DefendMin = 3,
                    DefendMax = 5,
                    IntentType = Intent.Attack,
                    IntentValue = 12
                },
                new EnemySaveData
                {
                    EnemyName = "Eye",
                    TexturePath = "res://Resources/Enemy/Eye.tres",
                    Health = 15,
                    MaxHealth = 15,
                    Armor = 0,
                    AttackMin = 4,
                    AttackMax = 8,
                    DefendMin = 2,
                    DefendMax = 4,
                    IntentType = Intent.Defend,
                    IntentValue = 5
                }
            };

            run.Stats = new RunStats
            {
                StepsCompleted = 4,
                DamageDealt = 120,
                DamageTaken = 55,
                TurnsPlayed = 6,
                CardsPlayed = 9,
                EnemiesDefeated = 3,
                RestsVisited = 1,
                Timestamp = DateTime.Now,
                DurationSec = 320
            };

            return run;
        }

        private static CardSaveData Card(string name, string description, int cost, string effect)
        {
            return new CardSaveData
            {
                CardName = name,
                Description = description,
                Cost = cost,
                EffectString = effect,
                Value = 0,
                Amount = 0
            };
        }
    }
}