using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    [TestSuite]
    public class ResourceCacheTest
    {
        private static readonly string[] PlanetPaths = new string[]
        {
            "res://Resources/PathDistribution/green_planet.tres",
            "res://Resources/PathDistribution/orange_planet.tres",
            "res://Resources/PathDistribution/purple_planet.tres",
            "res://Resources/PathDistribution/red_planet.tres",
            "res://Resources/PathDistribution/moon.tres"
        };

        [TestCase]
        [RequireGodotRuntime]
        public void Load_PlanetsExposeBackgroundAndEnemies()
        {
            foreach (string path in PlanetPaths)
            {
                PathDistribution dist = ResourceCache.Load<PathDistribution>(path);
                AssertThat(dist).IsNotNull();
                AssertThat(dist.PlanetBackground).IsNotNull();
                AssertThat(dist.Enemies).IsNotNull();
                AssertThat(dist.Enemies.Count > 0).IsTrue();
                AssertThat(dist.Enemies[0]).IsInstanceOf<EnemyResource>();
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Load_RestHasBackgroundButNoEnemies()
        {
            PathDistribution rest = ResourceCache.Load<PathDistribution>("res://Resources/PathDistribution/rest.tres");
            AssertThat(rest).IsNotNull();
            AssertThat(rest.PlanetBackground).IsNotNull();
            AssertThat(rest.Enemies).IsNotNull();
            AssertThat(rest.Enemies.Count).IsEqual(0);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Load_ReturnsSameStronglyHeldInstance()
        {
            PathDistribution first = ResourceCache.Load<PathDistribution>(PlanetPaths[0]);
            PathDistribution second = ResourceCache.Load<PathDistribution>(PlanetPaths[0]);
            AssertThat(ReferenceEquals(first, second)).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Load_SurvivesGarbageCollection()
        {
            PathDistribution first = ResourceCache.Load<PathDistribution>(PlanetPaths[0]);

            first = null;
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            PathDistribution second = ResourceCache.Load<PathDistribution>(PlanetPaths[0]);
            AssertThat(GodotObject.IsInstanceValid(second)).IsTrue();
            AssertThat(second.PlanetBackground).IsNotNull();
        }
    }
}
