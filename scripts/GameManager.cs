using System;
using System.Collections.Generic;
using System.IO;
using Godot;
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
    private List<Node2D> enemyContainers;

    public override void _Ready()
    {
        RegisterEvents();
        player = GetNode<Node2D>("%Player") as Player;
        enemyContainers = new()
        {
            GetNode<Node2D>("%EnemySpawn"),
            GetNode<Node2D>("%EnemySpawn2"),
            GetNode<Node2D>("%EnemySpawn3"),
            GetNode<Node2D>("%EnemySpawn4")
        };

        StartGame();
    }

    private void RegisterEvents()
    {
        // Register card clicks
        EventRegistry.RegisterEvent("OnCardClick");
        EventSubscriber.SubscribeToEvent("OnCardClick", OnCardClick);

        EventRegistry.RegisterEvent("OnEnemyClick");
        EventSubscriber.SubscribeToEvent("OnEnemyClick", OnEnemyClick);

        // Register inputs
        EventRegistry.RegisterEvent("OnEscapeKey");
        EventSubscriber.SubscribeToEvent("OnEscapeKey", OnCardClick);
    }

    public void StartGame()
    {
        // Get Cards for deck
        // Add cards to player deck
        // Starts the encounter
        LoadDeckFromJson("Data/initialDeck.json");
        StartEncounter();
    }

    public void StartEncounter()
    {
        // Implement encounter initialization
        GenerateEnemies();
        player.StartEncounter();
        ResolvePlayerTurn();
    }

    private void GenerateEnemies()
    {
        var enemyScene = GD.Load<PackedScene>("res://Scenes/enemy.tscn");
        Random rng = new();
        int numberOfEnemies = rng.Next(2) + 1;
        for (int i = 0; i < numberOfEnemies; i++)
        {
            int enemyHealth = rng.Next(99) + 1;
            int enemyArmor = rng.Next(4) + 1;
            var enemyNode = enemyScene.Instantiate();

            if (enemyNode is Character enemy)
            {
                enemy.Health = enemyHealth;
                enemy.MaxHealth = enemyHealth;
                enemy.Armor = enemyArmor;
                // enemies.Add(enemy);
                enemyContainers[i].AddChild(enemy);
            }
        }
    }

    public void ResolvePlayerTurn()
    {
        // Implement player turn logic
        // GenerateEnemyIntent();
    }

    private void GenerateEnemyIntent()
    {
        // For example, an enemy might attack or defend like the player, but the player sees that on his turn

        foreach (Enemy enemy in enemies)
        {
            Random rng = new();
            Intent intent = rng.Next(1) == 0 ? Intent.Attack : Intent.Defend;
            int intentValue = rng.Next(10);
            enemy.SetIntent(intent, intentValue);
        }
    }

    public void ResolveEnemyTurn()
    {
        // Implement enemy turn logic
        foreach (Enemy enemy in enemies)
        {
            enemy.PlayTurn(player);
        }
        NextTurn();
    }

    public void NextTurn()
    {
        if (turn == Turns.PlayerTurn)
            ResolveEnemyTurn();
        else
            ResolvePlayerTurn();
    }

    // Add other methods as needed

    public void LoadDeckFromJson(string filePath)
    {
        try
        {
            // Read the JSON file
            string jsonString = File.ReadAllText(filePath);

            // Deserialize the JSON into a list of cards
            List<Card> loadedDeck = JsonConvert.DeserializeObject<List<Card>>(jsonString);

            foreach (Card card in loadedDeck)
            {
                card.InitializeEffect();
            }
            // Assign the loaded deck to the player's deck
            player.Deck = loadedDeck;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading deck from JSON file: {e.Message}");
        }
    }

    // Events
    private void OnEscapeKey()
    {
        selectedCard = null;
    }

    private void OnEnemyClick(object sender, object obj)
    {
        if (selectedCard != null)
            if (obj is Enemy enemy)
            {
                GD.Print("Enemy successfully clicked!");
                player.PlayCard(selectedCard, enemy);
            }
    }

    private void OnCardClick(object sender, object obj)
    {
        if (obj is Card card)
        {
            GD.Print($"{card.CardName} clicked.");
            selectedCard = card;
        }
        else
        {
            GD.PrintErr("ERROR: Not a card, what the fudge?");
        }
    }
}
