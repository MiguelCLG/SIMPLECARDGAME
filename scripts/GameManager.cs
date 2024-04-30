using System;
using System.Collections.Generic;

public class GameManager
{
    private Player player;
    private List<Enemy> enemies;

    public GameManager(Player player)
    {
        this.player = player;
    }

    public void StartGame()
    {
        // Implement game initialization

        // Get Cards for deck
        // Add cards to player deck
    }

    public void StartEncounter()
    {
        // Implement encounter initialization
        GenerateEnemies();
    }

    private void GenerateEnemies()
    {
        Random rng = new();
        int numberOfEnemies = rng.Next(2) + 1;
        for (int i = 0; i < numberOfEnemies; i++)
        {
            int enemyHealth = rng.Next(99) + 1;
            int enemyArmor = rng.Next(4) + 1;
            enemies.Add(new Enemy(enemyHealth, enemyHealth, enemyArmor));
        }
    }

    public void ResolvePlayerTurn()
    {
        // Implement player turn logic
        GenerateEnemyIntent();
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
    }

    // Add other methods as needed
}
