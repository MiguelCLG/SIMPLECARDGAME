using System;
using System.Collections.Generic;
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

    /// <summary>Convenience overload: generates a fresh random seed for one-off (non-persisted) paths.</summary>
    public static List<List<PathNode>> Generate() => Generate(Random.Shared.Next(1, int.MaxValue));

    public static List<List<PathNode>> Generate(int seed)
    {
        Random rng = new(seed);
        List<List<PathNode>> path = new();
        for (int r = 0; r < StepRows; r++)
        {
            int count = RollRowCount(rng);
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
        PlaceRest(path[2], rng);
        PlaceRest(path[5], rng);
        string bossDist = EncounterDistributionPaths[rng.Next(EncounterDistributionPaths.Length)];
        path.Add(new List<PathNode> { new PathNode { Row = StepRows, Column = 0, Type = NodeType.Boss, DistributionPath = bossDist } });

        Connect(path);
        return path;
    }

    /// <summary>Rows vary between 1 and 4 nodes so the map mixes single-route corridors and wide choices.</summary>
    private static int RollRowCount(Random rng)
    {
        int roll = rng.Next(100);
        if (roll < 12) return 1;
        if (roll < 42) return 2;
        if (roll < 78) return 3;
        return 4;
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

    private static void PlaceRest(List<PathNode> row, Random rng)
    {
        int col = rng.Next(row.Count);
        row[col].Type = NodeType.Rest;
        row[col].DistributionPath = RestDistributionPath;
    }

    private static void Connect(List<List<PathNode>> path)
    {
        for (int r = 0; r < path.Count - 1; r++)
        {
            List<int>[] connections = BuildConnections(path[r].Count, path[r + 1].Count);
            for (int i = 0; i < path[r].Count; i++)
                path[r][i].Connections = connections[i];
        }
    }

    /// <summary>
    /// Builds non-crossing edges between a row of <paramref name="count"/> nodes and a next row of
    /// <paramref name="nextCount"/> nodes. Guarantees every node has at least one outgoing edge and
    /// every next-row node has at least one incoming edge. Out-degree naturally varies: widening
    /// rows branch, narrowing rows merge, equal rows stay one-to-one (unique routes).
    /// </summary>
    public static List<int>[] BuildConnections(int count, int nextCount)
    {
        var connections = new List<int>[count];
        for (int i = 0; i < count; i++)
            connections[i] = new List<int>();
        if (count == 0 || nextCount == 0)
            return connections;

        // Cover every next-row node: assign each one an evenly spaced parent.
        for (int j = 0; j < nextCount; j++)
        {
            int i = (int)Math.Floor((j + 0.5) * count / nextCount);
            i = Math.Clamp(i, 0, count - 1);
            AddConnection(connections[i], j);
        }

        // Cover every node in this row: give it an evenly spaced child if it has none yet.
        for (int i = 0; i < count; i++)
        {
            int j = (int)Math.Floor((i + 0.5) * nextCount / count);
            j = Math.Clamp(j, 0, nextCount - 1);
            AddConnection(connections[i], j);
        }

        foreach (List<int> list in connections)
            list.Sort();
        return connections;
    }

    private static void AddConnection(List<int> list, int value)
    {
        if (!list.Contains(value))
            list.Add(value);
    }

    /// <summary>Recomputes missing connections (e.g. saves made before explicit connections existed).</summary>
    public static void RepairConnections(RunData run)
    {
        if (run?.Path == null)
            return;

        for (int r = 0; r < run.Path.Count - 1; r++)
        {
            bool missing = false;
            foreach (PathNode node in run.Path[r])
            {
                if (node.Connections == null || node.Connections.Count == 0)
                {
                    missing = true;
                    break;
                }
            }
            if (!missing)
                continue;

            List<int>[] connections = BuildConnections(run.Path[r].Count, run.Path[r + 1].Count);
            for (int i = 0; i < run.Path[r].Count; i++)
                run.Path[r][i].Connections = connections[i];
        }
    }
}
