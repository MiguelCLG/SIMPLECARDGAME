using System;
using System.Collections.Generic;
using System.IO;
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
    [Export] private DeckResource initialDeck;

    public override void _Ready()
    {
        RegisterEvents();
        player = GetNode<Control>("%Player") as Player;
        enemies = new();
        enemyContainer = GetNode<Control>("%EnemySpawn");
        StartGame();
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

    public void Restart()
    {
        foreach (var enemy in enemies)
        {
            enemy.GetParent().RemoveChild(enemy);
            enemy.QueueFree();
        }
        enemies = new();
        turn = Turns.PlayerTurn;
        selectedCard = null;
        player.ResetPlayer();
        StartGame();
    }

    public void StartGame()
    {
        // Get Cards for deck
        // Add cards to player deck
        // Starts the encounter
        /* LoadDeckFromJson("Data/initialDeck.json"); */
        LoadDeck();
        StartEncounter();
    }

    public void StartEncounter()
    {
        // Implement encounter initialization
        GenerateEnemies();
        player.StartEncounter();
        ResolvePlayerTurn();
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
        var enemyScene = GD.Load<PackedScene>("res://Scenes/enemy.tscn");
        Random rng = new();
        int numberOfEnemies = rng.Next(4) + 1;
        List<EnemyDTO> enemyDTOs = GetEnemyTypesFromJson("Data/enemyTypes.json");
        for (int i = 0; i < numberOfEnemies; i++)
        {
            var enemyNode = enemyScene.Instantiate();
            if (enemyNode is Enemy enemy)
            {
                int enemyTypeIndex = rng.Next(0, 2);
                EnemyDTO enemyDTO = enemyDTOs[enemyTypeIndex];
                int enemyHealth = rng.Next(enemyDTO.MinHealth, enemyDTO.MaxHealth);

                enemy.intentMinValue = enemyDTO.MinIntent;
                enemy.intentMaxValue = enemyDTO.MaxIntent;

                enemy.EnemyName = enemyDTO.EnemyName;
                enemy.SpritePath = enemyDTO.Sprite;
                enemy.Health = enemyHealth;
                enemy.MaxHealth = enemyHealth;
                enemy.Armor = enemyDTO.Armor;

                enemies.Add(enemy);
                enemyContainer.AddChild(enemy);
            }
        }
    }

    public void ResolvePlayerTurn()
    {
        // Implement player turn logic
        GenerateEnemyIntent();
        player.SetManaToMax();
        player.RefreshHand();
    }

    private void GenerateEnemyIntent()
    {
        // For example, an enemy might attack or defend like the player, but the player sees that on his turn

        foreach (Enemy enemy in enemies)
        {
            Random rng = new();
            Intent intent = rng.Next(2) == 0 ? Intent.Attack : Intent.Defend;
            int intentValue = rng.Next(enemy.intentMinValue, enemy.intentMaxValue);
            enemy.SetIntent(intent, intentValue);
            if (intent == Intent.Attack)
                enemy.GetNode<TextureRect>("%IntentImage").Texture = GD.Load<Texture2D>(
                    "res://Images/Icons/sword.png"
                );
            else if (intent == Intent.Defend)
                enemy.GetNode<TextureRect>("%IntentImage").Texture = GD.Load<Texture2D>(
                    "res://Images/Icons/shield.png"
                );
        }
    }

    public void ResolveEnemyTurn()
    {
        // Implement enemy turn logic

        foreach (Enemy enemy in enemies)
        {
            Timer timer = Utils.TimerUtils.CreateTimer(() => enemy.PlayTurn(player), this, .5f);
        }
        NextTurn();
    }

    public void NextTurn()
    {
        if (turn == Turns.PlayerTurn)
        {
            turn = Turns.EnemyTurn;
            ResolveEnemyTurn();
        }
        else
        {
            turn = Turns.PlayerTurn;
            Utils.TimerUtils.CreateTimer(ResolvePlayerTurn, this, .5f);
        }
    }

    // Add other methods as needed

    /*   public void LoadDeckFromJson(string filePath)
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
              player.Hand = new();
              player.DiscardPile = new();
              player.Deck = loadedDeck;
          }
          catch (Exception e)
          {
              GD.PrintErr($"Error loading deck from JSON file: {e.Message}");
          }
      } */
    public void LoadDeck()
    {
        try
        {
            // Deserialize the JSON into a list of cards
            Array<Card> loadedDeck = new();

            foreach (CardResource card in initialDeck.cards)
            {
                Card newCard = new();
                newCard.CardName = card.CardName;
                newCard.Description = card.Description;
                newCard.Cost = card.Cost;
                newCard.EffectString = card.EffectString;
                newCard.Value = card.Value;
                newCard.InitializeEffect();
                loadedDeck.Add(newCard);
            }
            // Assign the loaded deck to the player's deck
            player.Hand = new();
            player.DiscardPile = new();
            player.Deck = loadedDeck;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading deck: {e.Message}");
        }
    }

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
            // PLAYER LOSES ENCOUNTER
            GD.Print("PLAYER Loses");
            Utils.TimerUtils.CreateTimer(Restart, this, 1f);
        }
    }

    private void OnEnemyDie(object sender, object obj)
    {
        if (obj is Enemy enemy)
        {
            enemies.Remove(enemy);
            var parent = enemy.GetParent();
            parent.RemoveChild(enemy);
            if (enemies.Count <= 0)
            {
                // PLAYER WINS ENCOUNTER
                GD.Print("PLAYER WINS");
                Utils.TimerUtils.CreateTimer(Restart, this, .5f);
            }
        }
    }

    private void OnEnemyClick(object sender, object obj)
    {
        if (selectedCard != null)
            if (obj is Enemy enemy)
            {
                player.PlayCard(selectedCard, enemy);
            }
        selectedCard = null;
    }

    private void OnCardClick(object sender, object obj)
    {
        if (turn != Turns.PlayerTurn)
            return;
        if (obj is Card card)
        {
            if (card.Effect.Type == CardEffectType.Attack)
            {
                selectedCard = card;
                return;
            }
            player.PlayCard(card, null);
        }
        else
        {
            GD.PrintErr("ERROR: Not a card, what the fudge?");
        }
    }
}
