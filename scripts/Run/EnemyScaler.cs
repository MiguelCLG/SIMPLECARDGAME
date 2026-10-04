using System;

public static class EnemyScaler
{
    public static float HealthScale(int row)
    {
        return 1f + 0.15f * row;
    }

    public static int ComputeHealth(int min, int max, int row, bool isBoss, Random rng)
    {
        int health = (int)(rng.Next(min, max) * HealthScale(row));
        if (isBoss) health = (int)(health * 1.5f);
        return health;
    }

    public static (int Min, int Max) ComputeAttack(int min, int max, int growthPerStep, int row, float actMultiplier)
    {
        int attackMin = (int)((min + row * growthPerStep) * actMultiplier);
        int attackMax = (int)((max + row * growthPerStep) * actMultiplier);
        return (attackMin, attackMax);
    }

    public static (int Min, int Max) ComputeDefend(int min, int max, int growthPerStep, int row, float actMultiplier)
    {
        int defendMin = (int)((min + row * growthPerStep) * actMultiplier);
        int defendMax = (int)((max + row * growthPerStep) * actMultiplier);
        return (defendMin, defendMax);
    }

    public static int EnemyCount(Random rng, bool isBoss)
    {
        return isBoss ? 1 : rng.Next(2) + 1;
    }
}