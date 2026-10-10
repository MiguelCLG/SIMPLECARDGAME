using System;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class EnemyScalerTest
    {
        [TestCase]
        public void HealthScale_BaseAndStepValues()
        {
            AssertThat(Math.Abs(EnemyScaler.HealthScale(0) - 1f) < 0.0001f).IsTrue();
            AssertThat(Math.Abs(EnemyScaler.HealthScale(5) - 1.75f) < 0.0001f).IsTrue();
            AssertThat(Math.Abs(EnemyScaler.HealthScale(9) - 2.35f) < 0.0001f).IsTrue();
        }

        [TestCase]
        public void ComputeHealth_FixedMinMaxScaleByRow()
        {
            int row0 = EnemyScaler.ComputeHealth(100, 100, 0, false, new Random(1));
            AssertThat(row0).IsEqual(100);

            int row5 = EnemyScaler.ComputeHealth(100, 100, 5, false, new Random(1));
            AssertThat(row5).IsEqual(175);
        }

        [TestCase]
        public void ComputeHealth_BossMultipliesByOnePointFive()
        {
            int boss = EnemyScaler.ComputeHealth(100, 100, 0, true, new Random(1));
            AssertThat(boss).IsEqual(150);
        }

        [TestCase]
        public void ComputeHealth_RandomizedStaysWithinScaledBounds()
        {
            var rng = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int min = rng.Next(10, 200);
                int max = min + rng.Next(0, 100);
                int row = rng.Next(0, 10);
                float scale = EnemyScaler.HealthScale(row);
                int result = EnemyScaler.ComputeHealth(min, max, row, false, rng);
                int largestSample = max == min ? min : max - 1;
                AssertThat(result >= (int)(min * scale)).IsTrue();
                AssertThat(result <= (int)(largestSample * scale)).IsTrue();
            }
        }

        [TestCase]
        public void ComputeAttack_GrowsPerRowAndScalesByActMultiplier()
        {
            var row0 = EnemyScaler.ComputeAttack(10, 20, 2, 0, 1f);
            AssertThat(row0.Min).IsEqual(10);
            AssertThat(row0.Max).IsEqual(20);

            var row5 = EnemyScaler.ComputeAttack(10, 20, 2, 5, 1f);
            AssertThat(row5.Min).IsEqual(20);
            AssertThat(row5.Max).IsEqual(30);

            var act2 = EnemyScaler.ComputeAttack(10, 20, 2, 5, 2f);
            AssertThat(act2.Min).IsEqual(40);
            AssertThat(act2.Max).IsEqual(60);
        }

        [TestCase]
        public void ComputeDefend_GrowsPerRowAndScalesByActMultiplier()
        {
            var row0 = EnemyScaler.ComputeDefend(5, 10, 1, 0, 1f);
            AssertThat(row0.Min).IsEqual(5);
            AssertThat(row0.Max).IsEqual(10);

            var row7 = EnemyScaler.ComputeDefend(5, 10, 1, 7, 1f);
            AssertThat(row7.Min).IsEqual(12);
            AssertThat(row7.Max).IsEqual(17);
        }

        [TestCase]
        public void EnemyCount_BossAlwaysOne()
        {
            AssertThat(EnemyScaler.EnemyCount(new Random(1), true)).IsEqual(1);
            AssertThat(EnemyScaler.EnemyCount(new Random(2), true)).IsEqual(1);
        }

        [TestCase]
        public void EnemyCount_NormalEncounterBetweenOneAndTwo()
        {
            for (int i = 0; i < 100; i++)
            {
                int count = EnemyScaler.EnemyCount(new Random(i), false);
                AssertThat(count >= 1 && count <= 2).IsTrue();
            }
        }
    }
}