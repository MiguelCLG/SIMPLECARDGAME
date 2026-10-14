using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class RunManagerPathRulesTest
    {
        [TestCase]
        public void IsSelectable_FirstRowAllColumnsSelectable()
        {
            var run = MakeRun(currentRow: 0);
            AssertThat(RunManager.IsSelectable(run, 0, 0)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 0, 1)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 0, 2)).IsTrue();
        }

        [TestCase]
        public void IsSelectable_OutOfRangeColumnsAreNotSelectable()
        {
            var run = MakeRun(currentRow: 0);
            AssertThat(RunManager.IsSelectable(run, 0, -1)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 0, 3)).IsFalse();
        }

        [TestCase]
        public void IsSelectable_RespectsPreviousNodeConnections()
        {
            var run = MakeRun(currentRow: 1);
            run.ChosenColumns[0] = 1;
            AssertThat(RunManager.IsSelectable(run, 1, 0)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 1, 1)).IsTrue();
        }

        [TestCase]
        public void IsSelectable_ExcludesColumnsNotConnectedToPrevious()
        {
            var run = MakeRun(currentRow: 1);
            run.ChosenColumns[0] = 0;
            AssertThat(RunManager.IsSelectable(run, 1, 0)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 1, 1)).IsFalse();

            run.ChosenColumns[0] = 2;
            AssertThat(RunManager.IsSelectable(run, 1, 1)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 1, 0)).IsFalse();
        }

        [TestCase]
        public void IsSelectable_SingleNodeRowReachableThroughConnections()
        {
            var run = MakeRun(currentRow: 2);
            run.ChosenColumns[1] = 1;
            AssertThat(RunManager.IsSelectable(run, 2, 0)).IsTrue();
        }

        [TestCase]
        public void IsSelectable_NonCurrentRowsAreNotSelectable()
        {
            var run = MakeRun(currentRow: 1);
            AssertThat(RunManager.IsSelectable(run, 0, 1)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 2, 0)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 4, 1)).IsFalse();
        }

        [TestCase]
        public void IsSelectable_NullRunReturnsFalse()
        {
            AssertThat(RunManager.IsSelectable(null, 0, 0)).IsFalse();
        }

        [TestCase]
        public void IsSelectable_OutOfRangeRowReturnsFalse()
        {
            var run = MakeRun(currentRow: 0);
            AssertThat(RunManager.IsSelectable(run, -1, 0)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 99, 0)).IsFalse();
        }

        private static RunData MakeRun(int currentRow)
        {
            var row0 = Nodes(0, 3);
            var row1 = Nodes(1, 2);
            var row2 = Nodes(2, 1);
            var row3 = Nodes(3, 4);
            var row4 = Nodes(4, 2);

            row0[0].Connections = new List<int> { 0 };
            row0[1].Connections = new List<int> { 0, 1 };
            row0[2].Connections = new List<int> { 1 };
            row1[0].Connections = new List<int> { 0 };
            row1[1].Connections = new List<int> { 0 };
            row2[0].Connections = new List<int> { 0, 1, 2, 3 };
            row3[0].Connections = new List<int> { 0 };
            row3[1].Connections = new List<int> { 1 };
            row3[2].Connections = new List<int> { 0 };
            row3[3].Connections = new List<int> { 1 };

            return new RunData
            {
                CurrentRow = currentRow,
                Path = new List<List<PathNode>> { row0, row1, row2, row3, row4 },
                ChosenColumns = new List<int> { -1, -1, -1, -1, -1 }
            };
        }

        private static List<PathNode> Nodes(int row, int count)
        {
            var list = new List<PathNode>();
            for (int c = 0; c < count; c++)
                list.Add(new PathNode { Row = row, Column = c, Type = NodeType.Encounter });
            return list;
        }
    }
}
