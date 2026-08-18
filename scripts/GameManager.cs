using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;

enum Turns
{
    PlayerTurn,
    EnemyTurn
}

public partial class GameManager : Node2D
{
    private Player player;
    private List<Enemy> enemies;
    private Turns turn = Turns.PlayerTurn;
    private Card selectedCard;
    private Control enemyContainer;
    private bool resumedEncounter;
    [Export] private DeckResource initialDeck;
    [Export] private Array<EnemyResource> enemyTypes;
    [Export] private ActConfig actConfig;

    public override void _Ready()
    {
        RegisterEvents();
        player = GetNode<Control>("%Player") as Player;
        enemies = new();
        enemyContainer = GetNode<Control>("%EnemySpawn");
        SetupRun();
    }

    public override void _ExitTree()
    {
        EventSubscriber.UnsubscribeFromEvent("OnCardClick", OnCardClick);
        EventSubscriber.UnsubscribeFromEvent("OnEnemyClick", OnEnemyClick);
        EventSubscriber.UnsubscribeFromEvent("OnEnemyDie", OnEnemyDie);
        EventSubscriber.UnsubscribeFromEvent("OnPlayerDie", OnPlayerDie);
        EventSubscriber.UnsubscribeFromEvent("OnEndTurnPress", OnEndTurnPress);
        EventSubscriber.UnsubscribeFromEvent("OnEscapeKey", OnEscapeKey);
    }

    private void RegisterEvents()
    {
        // Register card clicks
        EventRegistry.RegisterEvent("OnCardClick");
        EventSubscriber.SubscribeToEvent("OnCardClick", OnCardClick);

        EventRegistry.RegisterEvent("OnEnemyClick");
        EventSubscriber.SubscribeToEvent("OnEnemyClick", OnEnemyClick);

        EventRegistry.RegisterEvent("OnEnemyDie");
        EventSubscriber.SubscribeToEvent("OnEnemyDie", OnEnemyDie);

        EventRegistry.RegisterEvent("OnPlayerDie");
        EventSubscriber.SubscribeToEvent("OnPlayerDie", OnPlayerDie);

        EventRegistry.RegisterEvent("OnEndTurnPress");
        EventSubscriber.SubscribeToEvent("OnEndTurnPress", OnEndTurnPress);

        // Register inputs
        EventRegistry.RegisterEvent("OnEscapeKey");
        EventSubscriber.SubscribeToEvent("OnEscapeKey", OnEscapeKey);
    }

    private void SetupRun()
    {
        RunData run = RunManager.CurrentRun;
        if (run == null)
        {
            GD.PrintErr("No active run, returning to menu");
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            return;
        }

        player.ApplyState(run.PlayerState);
        player.SetDeck(run.Deck);
        player.SetHand(run.Hand);
        player.SetDiscard(run.Discard);

        resumedEncounter = run.ActiveEncounter && run.Enemies.Count > 0;
        if (resumedEncounter)
        {
            foreach (EnemySaveData data in run.Enemies)
            {
                SpawnEnemyFromSave(data);
            }
        }
        else
        {
            GenerateEnemies();
            run.ActiveEncounter = true;
            PersistLiveState();
        }

        player.StartEncounter();
        if (!resumedEncounter) player.Shuffle();
        ResolvePlayerTurn();
    }

    private void PersistLiveState()
    {
        RunData run = RunManager.CurrentRun;
        if (run == null) return;
        run.PlayerState = player.CollectState();
        player.CollectCards(out run.Deck, out run.Hand, out run.Discard);
        run.Enemies = enemies.Select(e => e.CollectState()).ToList();
        RunManager.SaveRun();
    }

    private void SpawnEnemyFromSave(EnemySaveData data)
    {
        var enemyScene = GD.Load<PackedScene>("res://Scenes/EnemyUI.tscn");
        var enemyNode = enemyScene.Instantiate();
        if (enemyNode is Enemy enemy)
        {
            enemy.ApplyState(data);
            enemies.Add(enemy);
            enemyContainer.AddChild(enemy);
            enemy.ApplyIntentVisuals();
        }
    }

    private void ReturnToMenu()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void ReturnToPath()
    {
        GetTree().ChangeSceneToFile("res://Scenes/PathMap.tscn");
    }

    private List<EnemyDTO> GetEnemyTypesFromJson(string filePath)
    {
        try
        {
            // Read the JSON file
            string jsonString = File.ReadAllText(filePath);

            // Deserialize the JSON into a list of cards
            List<EnemyDTO> loadedEnemyTypes = JsonConvert.DeserializeObject<List<EnemyDTO>>(
                jsonString
            );

            return loadedEnemyTypes;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading enemy types from JSON file: {e.Message}");
        }
        return new();
    }

    private void GenerateEnemies()
    {
        var enemyScene = GD.Load<PackedScene>("res://Scenes/EnemyUI.tscn");
        Random rng = new();
        int row = RunManager.CurrentRun?.CurrentRow ?? 0;
        float scale = 1f + row * 0.15f;
        bool boss = RunManager.CurrentRun?.IsBossNode == true;
        int numberOfEnemies = boss ? 1 : rng.Next(4) + 1;
        for (int i = 0; i < numberOfEnemies; i++)
        {
            var enemyNode = enemyScene.Instantiate();
            if (enemyNode is Enemy enemy)
            {
                int enemyTypeIndex = rng.Next(0, enemyTypes.Count);
                EnemyResource enemyResource = enemyTypes[enemyTypeIndex];
                int enemyHealth = (int)(rng.Next(enemyResource.MinHealth, enemyResource.MaxHealth) * scale);
                if (boss) enemyHealth = (int)(enemyHealth * 1.5f);

                float act = actConfig?.IntentMultiplier ?? 1f;
                int attackMin = (int)((enemyResource.AttackMin + row * enemyResource.AttackGrowthPerStep) * act);
                int attackMax = (int)((enemyResource.AttackMax + row * enemyResource.AttackGrowthPerStep) * act);
                int defendMin = (int)((enemyResource.DefendMin + row * enemyResource.DefendGrowthPerStep) * act);
                int defendMax = (int)((enemyResource.DefendMax + row * enemyResource.DefendGrowthPerStep) * act);

                enemy.attackMinValue = attackMin;
                enemy.attackMaxValue = attackMax;
                enemy.defendMinValue = defendMin;
                enemy.defendMaxValue = defendMax;

                enemy.EnemyName = enemyResource.EnemyName;
                enemy.texture = enemyResource.Texture;
                enemy.Health = enemyHealth;
                enemy.MaxHealth = enemyHealth;
                enemy.Armor = enemyResource.Armor;

                enemies.Add(enemy);
                enemyContainer.AddChild(enemy);
            }
        }
    }

    public void ResolvePlayerTurn()
    {
        // Implement player turn logic
        var stats = RunManager.CurrentRun?.Stats;
        if (stats != null) stats.TurnsPlayed++;
        if (resumedEncounter)
        {
            resumedEncounter = false;
        }
        else
        {
            GenerateEnemyIntent();
            player.SetManaToMax();
            player.RefreshHand();
        }
        player.GetNode<Button>("%EndTurnButton").Disabled = false;
        PersistLiveState();
    }

    private void GenerateEnemyIntent()
    {
        // For example, an enemy might attack or defend like the player, but the player sees that on his turn

        foreach (Enemy enemy in enemies)
        {
            Random rng = new();
            Intent intent = rng.Next(2) == 0 ? Intent.Attack : Intent.Defend;
            int value = intent == Intent.Attack
                ? rng.Next(enemy.attackMinValue, enemy.attackMaxValue)
                : rng.Next(enemy.defendMinValue, enemy.defendMaxValue);
            enemy.SetIntent(intent, value);
        }
    }

    public void ResolveEnemyTurn()
    {
        // Implement enemy turn logic

        foreach (Enemy enemy in enemies)
        {
            Timer timer = Utils.TimerUtils.CreateTimer(() => enemy.PlayTurn(player), this, .5f);
        }
        Utils.TimerUtils.CreateTimer(NextTurn, this, .5f * Math.Max(1, enemies.Count));
    }

    public void NextTurn()
    {
        if (turn == Turns.PlayerTurn)
        {
            turn = Turns.EnemyTurn;
            PersistLiveState();
            ResolveEnemyTurn();
        }
        else
        {
            turn = Turns.PlayerTurn;
            Utils.TimerUtils.CreateTimer(ResolvePlayerTurn, this, .5f);
        }
    }

    // Add other methods as needed

    // Events
    private void OnEndTurnPress(object sender, object obj)
    {
        NextTurn();
    }

    private void OnEscapeKey(object sender, object obj)
    {
        selectedCard = null;
    }

    private void OnPlayerDie(object sender, object obj)
    {
        if (obj is Player player)
        {
            // PLAYER LOSES THE RUN
            GD.Print("PLAYER Loses");
            RunManager.EndRun(false);
            Utils.TimerUtils.CreateTimer(ReturnToMenu, this, 1f);
        }
    }

    private void OnEnemyDie(object sender, object obj)
    {
        if (obj is Enemy enemy)
        {
            var stats = RunManager.CurrentRun?.Stats;
            if (stats != null) stats.EnemiesDefeated++;
            enemies.Remove(enemy);
            var parent = enemy.GetParent();
            parent.RemoveChild(enemy);
            enemy.QueueFree();
            if (enemies.Count <= 0)
            {
                PersistLiveState();
                if (RunManager.CurrentRun.IsBossNode)
                {
                    // PLAYER WINS THE RUN
                    GD.Print("PLAYER WINS THE RUN");
                    RunManager.EndRun(true);
                    Utils.TimerUtils.CreateTimer(ReturnToMenu, this, 1f);
                }
                else
                {
                    // PLAYER WINS ENCOUNTER
                    GD.Print("PLAYER WINS");
                    RunManager.CompleteCurrentNode();
                    Utils.TimerUtils.CreateTimer(ReturnToPath, this, .5f);
                }
            }
        }
    }

    private void OnEnemyClick(object sender, object obj)
    {
        if (selectedCard != null)
            if (obj is Enemy enemy)
            {
                player.PlayCard(selectedCard, new Array<Character>() { enemy });
                PersistLiveState();
            }
        selectedCard = null;
    }

    private void OnCardClick(object sender, object obj)
    {
        if (turn != Turns.PlayerTurn)
            return;
        if (obj is Card card)
        {
            if (card.Effect.Type == CardEffectType.Attack && !card.Effect.isMultipleTargets)
            {
                selectedCard = card;
                return;
            }
            Array<Character> characters = new();
            if (card.Effect.isTargetingSelf)
            {
                characters.Add(player);
            }
            else
            {
                foreach (Enemy enemy in enemies)
                {
                    characters.Add(enemy);
                }
            }
            player.PlayCard(card, characters);
            PersistLiveState();
        }
        else
        {
            GD.PrintErr("ERROR: Not a card, what the fudge?");
        }
    }
}
