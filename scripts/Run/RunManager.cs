using System;
using System.Linq;
using Godot;

public static class RunManager
{
    public static RunData CurrentRun { get; private set; }

    public static void NewRun()
    {
        RunData run = new();
        run.GeneratedAt = DateTime.Now;
        run.StartedAt = run.GeneratedAt;
        run.Path = PathGenerator.Generate();
        run.CurrentRow = 0;
        run.ChosenColumns = run.Path.Select(row => -1).ToList();
        run.PlayerState = new RunData.PlayerSaveState
        {
            Health = 100,
            MaxHealth = 100,
            Armor = 0,
            Mana = 3,
            MaxMana = 3
        };

        DeckResource deck = ResourceCache.Load<DeckResource>("res://Resources/Decks/InitialDeck_test.tres");
        if (deck != null)
        {
            foreach (CardResource card in deck.cards)
            {
                run.Deck.Add(CardSaveData.FromResource(card));
            }
        }
        else
        {
            GD.PrintErr("Initial deck resource not found");
        }

        CurrentRun = run;
        SaveRun();
    }

    public static bool LoadSave()
    {
        RunData loaded = SaveManager.LoadRun();
        if (loaded == null) return false;
        CurrentRun = loaded;
        return true;
    }

    public static bool HasSave() => SaveManager.HasSave();

    public static void SaveRun() => SaveManager.SaveRun(CurrentRun);

    public static void DeleteSave() => SaveManager.DeleteRun();

    public static bool IsSelectable(int row, int col)
    {
        return IsSelectable(CurrentRun, row, col);
    }

    public static bool IsSelectable(RunData run, int row, int col)
    {
        if (run == null) return false;
        if (row != run.CurrentRow) return false;
        if (row >= run.Path.Count) return false;
        if (col < 0 || col >= run.Path[row].Count) return false;
        if (row == 0) return true;
        if (run.Path[row].Count == 1) return true;
        int prev = run.ChosenColumns[row - 1];
        return col >= prev - 1 && col <= prev + 1;
    }

    public static bool ChooseNode(int row, int col)
    {
        if (!IsSelectable(row, col)) return false;
        CurrentRun.ChosenColumns[row] = col;
        CurrentRun.IsBossNode = CurrentRun.Path[row][col].Type == NodeType.Boss;
        CurrentRun.ActiveEncounter = CurrentRun.Path[row][col].Type != NodeType.Rest;
        SaveRun();
        return true;
    }

    public static void HealPlayer(int percent)
    {
        if (CurrentRun == null) return;
        RunData.PlayerSaveState p = CurrentRun.PlayerState;
        int heal = p.MaxHealth * percent / 100;
        p.Health = Math.Min(p.MaxHealth, p.Health + heal);
    }

    public static void CompleteCurrentNode()
    {
        if (CurrentRun == null) return;
        if (CurrentRun.ChosenColumns[CurrentRun.CurrentRow] >= 0)
        {
            NodeType type = CurrentRun.Path[CurrentRun.CurrentRow][CurrentRun.ChosenColumns[CurrentRun.CurrentRow]].Type;
            if (type == NodeType.Rest) CurrentRun.Stats.RestsVisited++;
        }
        CurrentRun.Stats.StepsCompleted++;
        CurrentRun.CurrentRow++;
        CurrentRun.ActiveEncounter = false;
        CurrentRun.IsBossNode = false;
        CurrentRun.Enemies.Clear();
        SaveRun();
    }

    public static void EndRun(bool success)
    {
        if (CurrentRun == null) return;
        CurrentRun.Stats.Success = success;
        CurrentRun.Stats.BossDefeated = success;
        CurrentRun.Stats.Timestamp = DateTime.Now;
        CurrentRun.Stats.DurationSec = (int)(DateTime.Now - CurrentRun.StartedAt).TotalSeconds;
        SaveManager.AppendRun(CurrentRun.Stats);
        SaveManager.DeleteRun();
        CurrentRun = null;
    }
}
