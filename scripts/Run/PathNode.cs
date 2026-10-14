using System.Collections.Generic;

public class PathNode
{
    public int Row;
    public int Column;
    public NodeType Type;
    public string DistributionPath;

    /// <summary>
    /// Columns in the next row this node leads to. Empty for the boss row (no outgoing edges).
    /// Connectivity is explicit so a node can have a single route or branch into several, and so
    /// every node is guaranteed to lead somewhere (all routes reach the boss).
    /// </summary>
    public List<int> Connections = new();
}
