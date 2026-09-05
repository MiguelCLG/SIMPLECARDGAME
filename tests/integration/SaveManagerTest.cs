using System.Collections.Generic;
using System.IO;
using GdUnit4;
using Godot;
using Newtonsoft.Json;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class SaveManagerTest
    {
        private static string RunsPath => System.IO.Path.Combine(OS.GetUserDataDir(), "runs.json");

        [TestCase]
        [RequireGodotRuntime]
        public void Save_Load_Delete_RoundTrip()
        {
            SaveManager.DeleteRun();

            var run = new RunData
            {
                CurrentRow = 3,
                ActiveEncounter = true,
                PlayerState = new RunData.PlayerSaveState
                {
                    Health = 50,
                    MaxHealth = 100,
                    Armor = 2,
                    Mana = 1,
                    MaxMana = 3
                }
            };

            SaveManager.SaveRun(run);

            AssertThat(SaveManager.HasSave()).IsTrue();

            RunData loaded = SaveManager.LoadRun();

            AssertThat(loaded).IsNotNull();
            AssertThat(loaded.CurrentRow).IsEqual(3);
            AssertThat(loaded.ActiveEncounter).IsTrue();
            AssertThat(loaded.PlayerState.Health).IsEqual(50);
            AssertThat(loaded.PlayerState.Armor).IsEqual(2);

            SaveManager.DeleteRun();
            AssertThat(SaveManager.HasSave()).IsFalse();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void LoadRun_WithoutSaveReturnsNull()
        {
            SaveManager.DeleteRun();
            AssertThat(SaveManager.LoadRun()).IsNull();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void AppendRun_GrowsHistoryList()
        {
            DeleteRunsHistoryFile();

            SaveManager.AppendRun(new RunStats { Success = true, CardsPlayed = 5 });
            SaveManager.AppendRun(new RunStats { Success = false, CardsPlayed = 2, EnemiesDefeated = 4 });

            List<RunStats> history = JsonConvert.DeserializeObject<List<RunStats>>(File.ReadAllText(RunsPath));

            AssertThat(history).IsNotNull();
            AssertThat(history.Count).IsEqual(2);
            AssertThat(history[0].Success).IsTrue();
            AssertThat(history[0].CardsPlayed).IsEqual(5);
            AssertThat(history[1].Success).IsFalse();
            AssertThat(history[1].EnemiesDefeated).IsEqual(4);

            DeleteRunsHistoryFile();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void AppendRun_CreatesFileIfMissing()
        {
            DeleteRunsHistoryFile();
            SaveManager.AppendRun(new RunStats { Success = true });

            AssertThat(File.Exists(RunsPath)).IsTrue();

            List<RunStats> history = JsonConvert.DeserializeObject<List<RunStats>>(File.ReadAllText(RunsPath));
            AssertThat(history.Count).IsEqual(1);

            DeleteRunsHistoryFile();
        }

        private static void DeleteRunsHistoryFile()
        {
            if (File.Exists(RunsPath)) File.Delete(RunsPath);
        }
    }
}