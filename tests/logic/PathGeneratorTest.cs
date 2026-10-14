using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class PathGeneratorTest
    {
        [TestCase]
        public void Generate_CreatesNineStepRowsPlusBossRow()
        {
            var path = PathGenerator.Generate();
            AssertThat(path.Count).IsEqual(PathGenerator.StepRows + 1);
        }

        [TestCase]
        public void Generate_EveryStepRowHasOneToFourNodes()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 0; r < PathGenerator.StepRows; r++)
                {
                    int count = path[r].Count;
                    AssertThat(count >= 1 && count <= 4).IsTrue();
                }
            }
        }

        [TestCase]
        public void Generate_BossRowIsSingleBossNode()
        {
            var path = PathGenerator.Generate();
            var bossRow = path[PathGenerator.StepRows];
            AssertThat(bossRow.Count).IsEqual(1);
            AssertThat(bossRow[0].Type).IsEqual(NodeType.Boss);
            AssertThat(bossRow[0].Row).IsEqual(PathGenerator.StepRows);
            AssertThat(bossRow[0].Column).IsEqual(0);
        }

        [TestCase]
        public void Generate_RestNodesAppearOnlyInRowsTwoAndFive()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 0; r < PathGenerator.StepRows; r++)
                {
                    int rests = path[r].Count(n => n.Type == NodeType.Rest);
                    int expected = (r == 2 || r == 5) ? 1 : 0;
                    AssertThat(rests).IsEqual(expected);
                }
            }
        }

        [TestCase]
        public void Generate_ColumnsAreContiguousPerRow()
        {
            var path = PathGenerator.Generate();
            for (int r = 0; r <= PathGenerator.StepRows; r++)
            {
                for (int c = 0; c < path[r].Count; c++)
                {
                    AssertThat(path[r][c].Row).IsEqual(r);
                    AssertThat(path[r][c].Column).IsEqual(c);
                }
            }
        }

        [TestCase]
        public void Generate_EveryNodeLeadsSomewhere()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 0; r < path.Count - 1; r++)
                {
                    foreach (var node in path[r])
                        AssertThat(node.Connections.Count >= 1).IsTrue();
                }
            }
        }

        [TestCase]
        public void Generate_EveryNodeIsReachableFromPreviousRow()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 1; r < path.Count; r++)
                {
                    for (int c = 0; c < path[r].Count; c++)
                    {
                        bool hasParent = path[r - 1].Any(n => n.Connections.Contains(c));
                        AssertThat(hasParent).IsTrue();
                    }
                }
            }
        }

        [TestCase]
        public void Generate_EveryLastStepNodeConnectsToBoss()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                foreach (var node in path[PathGenerator.StepRows - 1])
                    AssertThat(node.Connections.Contains(0)).IsTrue();
            }
        }

        [TestCase]
        public void Generate_ConnectionsAreUniqueAndWithinNextRow()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 0; r < path.Count - 1; r++)
                {
                    foreach (var node in path[r])
                    {
                        AssertThat(node.Connections.Count).IsEqual(node.Connections.Distinct().Count());
                        foreach (int c in node.Connections)
                            AssertThat(c >= 0 && c < path[r + 1].Count).IsTrue();
                    }
                }
            }
        }

        [TestCase]
        public void BuildConnections_EqualCountsMakeOneRoutePerNode()
        {
            var connections = PathGenerator.BuildConnections(3, 3);
            AssertThat(connections.Length).IsEqual(3);
            for (int i = 0; i < 3; i++)
            {
                AssertThat(connections[i].Count).IsEqual(1);
                AssertThat(connections[i][0]).IsEqual(i);
            }
        }

        [TestCase]
        public void BuildConnections_WideningBranchesSomeNodes()
        {
            var connections = PathGenerator.BuildConnections(2, 3);
            int branching = connections.Count(list => list.Count > 1);
            int single = connections.Count(list => list.Count == 1);
            AssertThat(branching >= 1).IsTrue();
            AssertThat(single >= 1).IsTrue();
        }

        [TestCase]
        public void BuildConnections_NarrowingMergesSomeNodes()
        {
            var connections = PathGenerator.BuildConnections(4, 1);
            for (int i = 0; i < 4; i++)
            {
                AssertThat(connections[i].Count).IsEqual(1);
                AssertThat(connections[i][0]).IsEqual(0);
            }
        }

        [TestCase]
        public void BuildConnections_EveryNodeAndTargetIsCovered()
        {
            for (int n = 1; n <= 4; n++)
            {
                for (int m = 1; m <= 4; m++)
                {
                    var connections = PathGenerator.BuildConnections(n, m);
                    for (int i = 0; i < n; i++)
                        AssertThat(connections[i].Count >= 1).IsTrue();
                    for (int j = 0; j < m; j++)
                        AssertThat(connections.Any(list => list.Contains(j))).IsTrue();
                }
            }
        }

        [TestCase]
        public void RepairConnections_FillsMissingEdgesOnly()
        {
            var run = new RunData
            {
                Path = new List<List<PathNode>>
                {
                    new List<PathNode> { N(0, 0), N(0, 1) },
                    new List<PathNode> { N(1, 0) }
                }
            };

            PathGenerator.RepairConnections(run);

            AssertThat(run.Path[0][0].Connections.Count >= 1).IsTrue();
            AssertThat(run.Path[0][1].Connections.Contains(0)).IsTrue();
        }

        [TestCase]
        public void Generate_SameSeedProducesIdenticalPath()
        {
            int seed = 123456789;
            var first = PathGenerator.Generate(seed);
            var second = PathGenerator.Generate(seed);
            AssertPathsEqual(first, second);
        }

        [TestCase]
        public void Generate_DifferentSeedsProduceDifferentPaths()
        {
            int differences = 0;
            int[][] pairs =
            {
                new[] { 1, 2 },
                new[] { 42, 1337 },
                new[] { 999, 1000 }
            };
            foreach (int[] pair in pairs)
            {
                var a = PathGenerator.Generate(pair[0]);
                var b = PathGenerator.Generate(pair[1]);
                if (!PathsEqual(a, b))
                    differences++;
            }
            AssertThat(differences >= 1).IsTrue();
        }

        private static void AssertPathsEqual(List<List<PathNode>> a, List<List<PathNode>> b)
        {
            AssertThat(a.Count).IsEqual(b.Count);
            for (int r = 0; r < a.Count; r++)
            {
                AssertThat(a[r].Count).IsEqual(b[r].Count);
                for (int c = 0; c < a[r].Count; c++)
                {
                    PathNode x = a[r][c];
                    PathNode y = b[r][c];
                    AssertThat(x.Type).IsEqual(y.Type);
                    AssertThat(x.DistributionPath).IsEqual(y.DistributionPath);
                    AssertThat(x.Row).IsEqual(y.Row);
                    AssertThat(x.Column).IsEqual(y.Column);
                    AssertThat(x.Connections.Count).IsEqual(y.Connections.Count);
                    for (int i = 0; i < x.Connections.Count; i++)
                        AssertThat(x.Connections[i]).IsEqual(y.Connections[i]);
                }
            }
        }

        private static bool PathsEqual(List<List<PathNode>> a, List<List<PathNode>> b)
        {
            if (a.Count != b.Count) return false;
            for (int r = 0; r < a.Count; r++)
            {
                if (a[r].Count != b[r].Count) return false;
                for (int c = 0; c < a[r].Count; c++)
                {
                    PathNode x = a[r][c];
                    PathNode y = b[r][c];
                    if (x.Type != y.Type) return false;
                    if (x.DistributionPath != y.DistributionPath) return false;
                    if (x.Row != y.Row || x.Column != y.Column) return false;
                    if (x.Connections.Count != y.Connections.Count) return false;
                    for (int i = 0; i < x.Connections.Count; i++)
                        if (x.Connections[i] != y.Connections[i]) return false;
                }
            }
            return true;
        }

        private static PathNode N(int row, int col)
        {
            return new PathNode { Row = row, Column = col, Type = NodeType.Encounter };
        }
    }
}
