using GdUnit4;
using Newtonsoft.Json;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    /// <summary>
    /// Pure Newtonsoft round-trips on the <see cref="MetaSave"/> POCO.
    /// No MetaManager / OS.GetUserDataDir() is touched, so these carry no
    /// [RequireGodotRuntime] -- mirroring RunDataSerializationTest, which is
    /// pure logic. (The adapter still boots Godot for the assembly, but these
    /// cases themselves need no runtime.)
    /// </summary>
    [TestSuite]
    public class MetaSaveSerializationTest
    {
        [TestCase]
        public void Serialize_Deserialize_PreservesRunsFinished()
        {
            var meta = new MetaSave { RunsFinished = 7 };

            string json = JsonConvert.SerializeObject(meta, Formatting.Indented);
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>(json);

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.RunsFinished).IsEqual(7);
            AssertThat(loaded.SchemaVersion).IsEqual(1);
        }

        [TestCase]
        public void Deserialize_EmptyObject_UsesFieldInitializerDefaults()
        {
            // JsonConvert.DeserializeObject<T>() runs the parameterless constructor
            // (and therefore field initializers) BEFORE applying the JSON payload.
            // For an empty object the initializers survive untouched, so
            // SchemaVersion keeps its field initializer value of 1 (NOT 0) and
            // RunsFinished keeps its implicit 0. Verified empirically against
            // Newtonsoft.Json 13.0.3 with the production MetaSave class.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("{}");

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.RunsFinished).IsEqual(0);
            AssertThat(loaded.SchemaVersion).IsEqual(1);
        }

        [TestCase]
        public void Deserialize_LegacyWithoutSchemaVersion_KeepsSchemaDefault()
        {
            // A pre-schema-version file has no SchemaVersion key. The field
            // initializer supplies 1; only the explicitly-serialized
            // RunsFinished comes from the JSON.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("{\"RunsFinished\":3}");

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.RunsFinished).IsEqual(3);
            AssertThat(loaded.SchemaVersion).IsEqual(1);
        }

        [TestCase]
        public void Deserialize_FutureSchemaVersion_IsPreservedNotClamped()
        {
            // A save written by a newer build must deserialize without throwing
            // and keep its higher SchemaVersion verbatim (MetaManager logs but
            // loads it as-is; it never downgrades on the POCO level).
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("{\"SchemaVersion\":99,\"RunsFinished\":4}");

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.SchemaVersion).IsEqual(99);
            AssertThat(loaded.RunsFinished).IsEqual(4);
        }

        [TestCase]
        public void Deserialize_NullLiteral_ReturnsNullWithoutThrowing()
        {
            // MetaManager.LoadOrCreate() relies on this: a deserialized null is
            // its corruption signal (it logs, moves the file aside, and starts
            // fresh). This case pins that contract.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("null");

            AssertThat(loaded).IsNull();
        }

        [TestCase]
        public void Deserialize_Whitespace_ReturnsNullWithoutThrowing()
        {
            // Same corruption-recovery contract as the null literal: an empty or
            // whitespace-only body must yield null rather than throw.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("   ");

            AssertThat(loaded).IsNull();
        }

        [TestCase]
        public void Serialize_DoesNotPersistAverageDurationSec()
        {
            // T6a -- THE REGRESSION GUARD.
            //
            // AverageDurationSec is DERIVED on read (TotalDurationSec /
            // RunsFinished) and must NEVER be written to meta.json.
            //
            // This was a real bug: AverageDurationSec was originally a plain
            // get-only property and Newtonsoft DID serialize it, producing
            // `"AverageDurationSec": 65` in meta.json. [JsonIgnore] was added on
            // MetaSave to fix it. If that attribute is ever removed or a setter /
            // backing field is introduced, this test fails loudly.
            var meta = new MetaSave
            {
                RunsFinished = 2,
                TotalDurationSec = 130
            };

            string json = JsonConvert.SerializeObject(meta, Formatting.Indented);

            // A negative substring assertion on the serialized payload: if the
            // property is no longer [JsonIgnore]d this fails with the offending
            // JSON attached.
            AssertThat(json)
                .OverrideFailureMessage(
                    "meta.json must not contain an \"AverageDurationSec\" key: the average " +
                    "is derived on read and [JsonIgnore]d on MetaSave. Serialized JSON was:\n" + json)
                .NotContains("AverageDurationSec");
        }

        [TestCase]
        public void Serialize_Deserialize_PreservesAllTotalsAndRecomputesAverage()
        {
            // T6b -- every cumulative total round-trips, and the derived average
            // recomputes (identically) after deserialize rather than being read
            // back from disk.
            var meta = new MetaSave
            {
                RunsFinished = 2,
                TotalSuccesses = 1,
                TotalStepsCompleted = 18,
                TotalDamageDealt = 420,
                TotalDamageTaken = 175,
                TotalTurnsPlayed = 24,
                TotalCardsPlayed = 37,
                TotalEnemiesDefeated = 11,
                TotalRestsVisited = 3,
                TotalBossesDefeated = 1,
                TotalDurationSec = 130
            };

            string json = JsonConvert.SerializeObject(meta, Formatting.Indented);
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>(json);

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.SchemaVersion).IsEqual(1);
            AssertThat(loaded.RunsFinished).IsEqual(2);
            AssertThat(loaded.TotalSuccesses).IsEqual(1);
            AssertThat(loaded.TotalStepsCompleted).IsEqual(18);
            AssertThat(loaded.TotalDamageDealt).IsEqual(420);
            AssertThat(loaded.TotalDamageTaken).IsEqual(175);
            AssertThat(loaded.TotalTurnsPlayed).IsEqual(24);
            AssertThat(loaded.TotalCardsPlayed).IsEqual(37);
            AssertThat(loaded.TotalEnemiesDefeated).IsEqual(11);
            AssertThat(loaded.TotalRestsVisited).IsEqual(3);
            AssertThat(loaded.TotalBossesDefeated).IsEqual(1);
            AssertThat(loaded.TotalDurationSec).IsEqual(130);

            // Derived, not stored: 130 / 2 == 65.
            AssertThat(loaded.AverageDurationSec).IsEqual(65);
        }

        [TestCase]
        public void AverageDurationSec_IsZeroWhenNoRunsFinished()
        {
            // T6c -- divide-by-zero guard. Even with a non-zero accumulated
            // duration the average must return 0 while RunsFinished is 0.
            var meta = new MetaSave { RunsFinished = 0, TotalDurationSec = 500 };

            AssertThat(meta.AverageDurationSec).IsEqual(0);
        }

        [TestCase]
        public void AverageDurationSec_TruncatesIntegerDivision()
        {
            // T6d -- pins truncation toward zero (both operands non-negative, so
            // floor). 131 / 2 == 65, NOT 66. A whole-second average is what the UI
            // displays; a future fractional accessor must be a separate member.
            var meta = new MetaSave { RunsFinished = 2, TotalDurationSec = 131 };

            AssertThat(meta.AverageDurationSec).IsEqual(65);
            AssertThat(meta.AverageDurationSec).IsNotEqual(66);
        }

        [TestCase]
        public void Deserialize_EmptyObject_DefaultsAllTotalsToZero()
        {
            // T10 -- an empty payload carries no totals; all ten default to 0 and
            // the derived average is guarded to 0.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("{}");

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.RunsFinished).IsEqual(0);
            AssertThat(loaded.TotalSuccesses).IsEqual(0);
            AssertThat(loaded.TotalStepsCompleted).IsEqual(0);
            AssertThat(loaded.TotalDamageDealt).IsEqual(0);
            AssertThat(loaded.TotalDamageTaken).IsEqual(0);
            AssertThat(loaded.TotalTurnsPlayed).IsEqual(0);
            AssertThat(loaded.TotalCardsPlayed).IsEqual(0);
            AssertThat(loaded.TotalEnemiesDefeated).IsEqual(0);
            AssertThat(loaded.TotalRestsVisited).IsEqual(0);
            AssertThat(loaded.TotalBossesDefeated).IsEqual(0);
            AssertThat(loaded.TotalDurationSec).IsEqual(0);
            AssertThat(loaded.AverageDurationSec).IsEqual(0);
        }

        [TestCase]
        public void Deserialize_LegacySchemaOneFile_LoadsWithZeroTotals()
        {
            // T7 (logic half) -- the shape of the live pre-feature v1 file. The ten
            // totals are additive and absent, so they deserialize to 0 without any
            // migration step; RunsFinished is preserved untouched by the POCO.
            MetaSave loaded = JsonConvert.DeserializeObject<MetaSave>("{\"SchemaVersion\":1,\"RunsFinished\":2}");

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.SchemaVersion).IsEqual(1);
            AssertThat(loaded.RunsFinished).IsEqual(2);
            AssertThat(loaded.TotalSuccesses).IsEqual(0);
            AssertThat(loaded.TotalStepsCompleted).IsEqual(0);
            AssertThat(loaded.TotalDamageDealt).IsEqual(0);
            AssertThat(loaded.TotalDamageTaken).IsEqual(0);
            AssertThat(loaded.TotalTurnsPlayed).IsEqual(0);
            AssertThat(loaded.TotalCardsPlayed).IsEqual(0);
            AssertThat(loaded.TotalEnemiesDefeated).IsEqual(0);
            AssertThat(loaded.TotalRestsVisited).IsEqual(0);
            AssertThat(loaded.TotalBossesDefeated).IsEqual(0);
            AssertThat(loaded.TotalDurationSec).IsEqual(0);
            AssertThat(loaded.AverageDurationSec).IsEqual(0);
        }
    }
}
