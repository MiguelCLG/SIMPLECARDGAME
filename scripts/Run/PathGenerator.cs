using System;
using System.Collections.Generic;
using System.Linq;

public static class PathGenerator
{
    public const int StepRows = 9;

    public static List<List<PathNode>> Generate()
    {
        Random rng = new();
        List<List<PathNode>> path = new();
        for (int r = 0; r < StepRows; r++)
        {
            int count = rng.Next(3, 5);
            List<PathNode> row = new();
            for (int c = 0; c < count; c++)
            {
                row.Add(new PathNode { Row = r, Column = c, Type = NodeType.Encounter });
            }
            path.Add(row);
        }
        PlaceRest(path[2]);
        PlaceRest(path[5]);
        path.Add(new List<PathNode> { new PathNode { Row = StepRows, Column = 0, Type = NodeType.Boss } });
        return path;
    }

    private static void PlaceRest(List<PathNode> row)
    {
        Random rng = new();
        int col = rng.Next(row.Count);
        row[col].Type = NodeType.Rest;
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
