using System;
using System.Collections.Generic;

public class RunData
{
    public int SchemaVersion = 1;
    public int Seed;
    public DateTime GeneratedAt;
    public DateTime StartedAt;
    public List<List<PathNode>> Path = new();
    public int CurrentRow;
    public List<int> ChosenColumns = new();
    public bool ActiveEncounter;
    public bool IsBossNode;
    public PlayerSaveState PlayerState = new();
    public List<CardSaveData> Deck = new();
    public List<CardSaveData> Hand = new();
    public List<CardSaveData> Discard = new();
    public List<EnemySaveData> Enemies = new();
    public RunStats Stats = new();
    public bool PendingReward;
    public List<CardSaveData> PendingRewardCards = new();

    /// <summary>
    /// Single source of truth for "a reward is awaiting the player": the flag is set
    /// only together with a non-empty card list, so legacy saves (both defaulting to
    /// empty) report false. Not serialized (methods are ignored by the serializer).
    /// </summary>
    public bool HasPendingReward()
    {
        return PendingReward && PendingRewardCards != null && PendingRewardCards.Count > 0;
    }

    public class PlayerSaveState
    {
        public int Health;    // run-scoped: persists across encounters (healed only at rest nodes)
        public int MaxHealth; // run-scoped
        public int Armor;     // encounter-scoped: cleared on fresh encounters via Player.ResetEncounterStats()
        public int Mana;      // turn-scoped: refilled each player turn in GameManager.ResolvePlayerTurn
        public int MaxMana;   // run-scoped
    }
}
