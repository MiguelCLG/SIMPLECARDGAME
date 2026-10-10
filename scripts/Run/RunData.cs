using System;
using System.Collections.Generic;

public class RunData
{
    public int SchemaVersion = 1;
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

    public class PlayerSaveState
    {
        public int Health;
        public int MaxHealth;
        public int Armor;
        public int Mana;
        public int MaxMana;
    }
}
