using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class PathGenerator
{
    public const int StepRows = 9;

    private static readonly string[] EncounterDistributionPaths = new string[]
    {
        "res://Resources/PathDistribution/green_planet.tres",
        "res://Resources/PathDistribution/orange_planet.tres",
        "res://Resources/PathDistribution/purple_planet.tres",
        "res://Resources/PathDistribution/red_planet.tres",
        "res://Resources/PathDistribution/moon.tres"
    };

    private const string RestDistributionPath = "res://Resources/PathDistribution/rest.tres";

    public static List<List<PathNode>> Generate()
    {
        Random rng = new();
        List<List<PathNode>> path = new();
        for (int r = 0; r < StepRows; r++)
        {
            int count = rng.Next(3, 5);
            List<string> rowDistributions = GetShuffledDistributions(rng, count);
            List<PathNode> row = new();
            for (int c = 0; c < count; c++)
            {
                row.Add(new PathNode
                {
                    Row = r,
                    Column = c,
                    Type = NodeType.Encounter,
                    DistributionPath = rowDistributions[c]
                });
            }
            path.Add(row);
        }
        PlaceRest(path[2]);
        PlaceRest(path[5]);
        string bossDist = EncounterDistributionPaths[rng.Next(EncounterDistributionPaths.Length)];
        path.Add(new List<PathNode> { new PathNode { Row = StepRows, Column = 0, Type = NodeType.Boss, DistributionPath = bossDist } });
        return path;
    }

    private static List<string> GetShuffledDistributions(Random rng, int count)
    {
        List<string> list = new List<string>(EncounterDistributionPaths);
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        List<string> result = new List<string>(count);
        for (int i = 0; i < count && i < list.Count; i++)
        {
            result.Add(list[i]);
        }
        while (result.Count < count)
        {
            result.Add(EncounterDistributionPaths[rng.Next(EncounterDistributionPaths.Length)]);
        }
        return result;
    }

    private static void PlaceRest(List<PathNode> row)
    {
        Random rng = new();
        int col = rng.Next(row.Count);
        row[col].Type = NodeType.Rest;
        row[col].DistributionPath = RestDistributionPath;
    }

    public static List<int> GetSelectableColumns(List<List<PathNode>> path, int row, int prevColumn)
    {
        if (row >= path.Count) return new List<int>();
        int count = path[row].Count;
        if (row == 0) return Enumerable.Range(0, count).ToList();
        List<int> cols = new();
        for (int d = -1; d <= 1; d++)
        {
            int c = prevColumn + d;
            if (c >= 0 && c < count) cols.Add(c);
        }
        return cols;
    }
}
