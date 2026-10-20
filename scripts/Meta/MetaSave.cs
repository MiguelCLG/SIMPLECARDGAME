using System;
using Newtonsoft.Json;

public class MetaSave
{
    public int SchemaVersion = 1;
    public int RunsFinished;

    // Cumulative totals, accumulated once per finished run (win or lose).
    public int TotalSuccesses;      // count of wins (Success == true)
    public int TotalStepsCompleted;
    public int TotalDamageDealt;
    public int TotalDamageTaken;
    public int TotalTurnsPlayed;
    public int TotalCardsPlayed;
    public int TotalEnemiesDefeated;
    public int TotalRestsVisited;
    public int TotalBossesDefeated; // count of runs where BossDefeated == true
    public int TotalDurationSec;

    // Derived on read; NEVER persisted. Integer division truncates.
    [JsonIgnore]
    public int AverageDurationSec =>
        RunsFinished > 0 ? TotalDurationSec / RunsFinished : 0;
}
