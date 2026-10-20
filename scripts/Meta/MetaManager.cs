using System;
using System.IO;
using Godot;
using Newtonsoft.Json;

public static class MetaManager
{
    private const int CurrentSchemaVersion = 1;

    private static MetaSave _current;
    public static MetaSave Current => _current ??= LoadOrCreate();

    private static string MetaPath => Path.Combine(OS.GetUserDataDir(), "meta.json");

    public static void Load() => _current = LoadOrCreate();

    public static void Save() => SaveCurrent();

    /// <summary>
    /// Records one *finished* run (win or lose) into the cross-run meta totals.
    /// Called by RunManager.EndRun after SaveManager.AppendRun. The stats object
    /// is READ ONLY here and is never stored in MetaSave (meta never lives in
    /// RunData, and RunData never lives in meta). Null is ignored.
    /// </summary>
    public static void RecordRunFinished(RunStats stats)
    {
        if (stats == null) return;

        MetaSave meta = Current;
        meta.RunsFinished++;

        if (stats.Success) meta.TotalSuccesses++;
        if (stats.BossDefeated) meta.TotalBossesDefeated++;

        meta.TotalStepsCompleted   += stats.StepsCompleted;
        meta.TotalDamageDealt      += stats.DamageDealt;
        meta.TotalDamageTaken      += stats.DamageTaken;
        meta.TotalTurnsPlayed      += stats.TurnsPlayed;
        meta.TotalCardsPlayed      += stats.CardsPlayed;
        meta.TotalEnemiesDefeated  += stats.EnemiesDefeated;
        meta.TotalRestsVisited     += stats.RestsVisited;
        meta.TotalDurationSec      += stats.DurationSec;

        SaveCurrent();   // mutate-then-save, same as before; no exit-time flush
    }

    /// <summary>
    /// Test-only: clears the static cache. Does NOT touch any files.
    /// Tests own their own file cleanup.
    /// </summary>
    public static void ResetForTests()
    {
        _current = null;
    }

    internal static MetaSave LoadOrCreate()
    {
        if (!File.Exists(MetaPath))
            return EnsureDefaults(new MetaSave());

        try
        {
            MetaSave meta = JsonConvert.DeserializeObject<MetaSave>(File.ReadAllText(MetaPath));
            if (meta == null)
            {
                GD.PrintErr("Meta load failed: deserialized null");
                MoveCorruptAside();
                return EnsureDefaults(new MetaSave());
            }
            if (meta.SchemaVersion > CurrentSchemaVersion)
                GD.Print($"Meta schema {meta.SchemaVersion} is newer than {CurrentSchemaVersion}; loading as-is");
            return EnsureDefaults(meta);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Meta load failed: {e.Message}");
            MoveCorruptAside();
            return EnsureDefaults(new MetaSave());
        }
    }

    private static void MoveCorruptAside()
    {
        try
        {
            if (File.Exists(MetaPath)) File.Move(MetaPath, MetaPath + ".bak");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Meta backup failed: {e.Message}");
        }
    }

    private static MetaSave EnsureDefaults(MetaSave meta)
    {
        if (meta == null) return new MetaSave();
        if (meta.SchemaVersion == 0) meta.SchemaVersion = CurrentSchemaVersion;
        return meta;
    }

    private static void SaveCurrent()
    {
        try
        {
            File.WriteAllText(MetaPath, JsonConvert.SerializeObject(Current, Formatting.Indented));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Meta save failed: {e.Message}");
        }
    }
}
