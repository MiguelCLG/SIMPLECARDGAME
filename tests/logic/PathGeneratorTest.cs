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
        public void Generate_EveryStepRowHasThreeToFourNodes()
        {
            for (int i = 0; i < 25; i++)
            {
                var path = PathGenerator.Generate();
                for (int r = 0; r < PathGenerator.StepRows; r++)
                {
                    int count = path[r].Count;
                    AssertThat(count >= 3 && count <= 4).IsTrue();
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
        public void GetSelectableColumns_FirstRowReturnsAllColumns()
        {
            var path = PathGenerator.Generate();
            var cols = PathGenerator.GetSelectableColumns(path, 0, 0);
            AssertThat(cols.Count).IsEqual(path[0].Count);
        }

        [TestCase]
        public void GetSelectableColumns_ReturnsPreviousPlusMinusOne()
        {
            var path = new List<List<PathNode>>
            {
                new List<PathNode> { N(0, 0), N(0, 1), N(0, 2) },
                new List<PathNode> { N(1, 0), N(1, 1), N(1, 2), N(1, 3) }
            };

            var cols = PathGenerator.GetSelectableColumns(path, 1, 0);
            AssertThat(cols.Count).IsEqual(2);
            AssertThat(cols.Contains(0)).IsTrue();
            AssertThat(cols.Contains(1)).IsTrue();
        }

        [TestCase]
        public void GetSelectableColumns_ClampsToRowBounds()
        {
            var path = new List<List<PathNode>>
            {
                new List<PathNode> { N(0, 0), N(0, 1), N(0, 2) },
                new List<PathNode> { N(1, 0), N(1, 1), N(1, 2), N(1, 3) }
            };

            var cols = PathGenerator.GetSelectableColumns(path, 1, 3);
            AssertThat(cols.Count).IsEqual(2);
            AssertThat(cols.Contains(2)).IsTrue();
            AssertThat(cols.Contains(3)).IsTrue();
        }

        [TestCase]
        public void GetSelectableColumns_OutOfRangeRowReturnsEmpty()
        {
            var path = PathGenerator.Generate();
            var cols = PathGenerator.GetSelectableColumns(path, 99, 0);
            AssertThat(cols.Count).IsEqual(0);
        }

        private static PathNode N(int row, int col)
        {
            return new PathNode { Row = row, Column = col, Type = NodeType.Encounter };
        }
    }
}