using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;

public static class SaveManager
{
    private static string SavePath => Path.Combine(OS.GetUserDataDir(), "savegame.json");
    private static string RunsPath => Path.Combine(OS.GetUserDataDir(), "runs.json");

    public static bool HasSave() => File.Exists(SavePath);

    public static void SaveRun(RunData run)
    {
        try
        {
            File.WriteAllText(SavePath, JsonConvert.SerializeObject(run, Formatting.Indented));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Save failed: {e.Message}");
        }
    }

    public static RunData LoadRun()
    {
        if (!HasSave()) return null;
        try
        {
            return JsonConvert.DeserializeObject<RunData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Load failed: {e.Message}");
            return null;
        }
    }

    public static void DeleteRun()
    {
        try
        {
            if (HasSave()) File.Delete(SavePath);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Delete failed: {e.Message}");
        }
    }

    public static void AppendRun(RunStats stats)
    {
        try
        {
            List<RunStats> history = new();
            if (File.Exists(RunsPath))
            {
                List<RunStats> existing = JsonConvert.DeserializeObject<List<RunStats>>(File.ReadAllText(RunsPath));
                if (existing != null) history = existing;
            }
            history.Add(stats);
            File.WriteAllText(RunsPath, JsonConvert.SerializeObject(history, Formatting.Indented));
        }
        catch (Exception e)
        {
            GD.PrintErr($"History save failed: {e.Message}");
        }
    }
}
