using System.Collections.Generic;
using System.Linq;
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
        public void IsSelectable_RespectsPreviousColumnAdjacency()
        {
            var run = MakeRun(currentRow: 3);
            run.ChosenColumns[2] = 0;
            AssertThat(RunManager.IsSelectable(run, 3, 0)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 3, 1)).IsTrue();
            AssertThat(RunManager.IsSelectable(run, 3, 2)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 3, 3)).IsFalse();
        }

        [TestCase]
        public void IsSelectable_AdjacencyClampedToRowEdge()
        {
            var run = MakeRun(currentRow: 1);
            run.ChosenColumns[0] = 2;
            AssertThat(RunManager.IsSelectable(run, 1, 0)).IsFalse();
            AssertThat(RunManager.IsSelectable(run, 1, 1)).IsTrue();
        }

        [TestCase]
        public void IsSelectable_SingleNodeRowAlwaysSelectable()
        {
            var run = MakeRun(currentRow: 2);
            run.ChosenColumns[1] = 0;
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
            var run = new RunData
            {
                CurrentRow = currentRow,
                Path = new List<List<PathNode>>
                {
                    new List<PathNode> { Encounter(0), Encounter(1), Encounter(2) },
                    new List<PathNode> { Encounter(0), Encounter(1) },
                    new List<PathNode> { Encounter(0) },
                    new List<PathNode> { Encounter(0), Encounter(1), Encounter(2), Encounter(3) },
                    new List<PathNode> { Encounter(0), Encounter(1) }
                },
                ChosenColumns = new List<int> { -1, -1, -1, -1, -1 }
            };
            return run;
        }

        private static PathNode Encounter(int col)
        {
            return new PathNode { Row = 0, Column = col, Type = NodeType.Encounter };
        }
    }
}