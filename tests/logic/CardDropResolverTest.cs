using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Logic
{
    /// <summary>
    /// Unit tests for <see cref="CardDropResolver"/>, the pure drop-zone classifier used by the
    /// hand-card drag. No scene, node or IO access is needed; the cases are marked
    /// <see cref="RequireGodotRuntimeAttribute"/> only because the API takes <see cref="Rect2"/> and
    /// <see cref="Vector2"/> values (the gdUnit4 analyzer requires it for Godot types).
    /// </summary>
    [TestSuite]
    public class CardDropResolverTest
    {
        // Shared fixtures: the center drop zone and two non-overlapping enemy rects.
        private static readonly Rect2 CenterZone = new Rect2(30, 269, 300, 180);
        private static readonly Rect2[] EnemyRects =
        {
            new Rect2(60, 120, 81, 150),
            new Rect2(200, 120, 81, 150)
        };

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_AttackSingleTarget_DroppedOnFirstEnemy_ReturnsThatEnemy()
        {
            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(100, 180),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Enemy, 0);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_AttackSingleTarget_DroppedOnSecondEnemy_ReturnsThatEnemy()
        {
            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(240, 180),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Enemy, 1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_AttackSingleTarget_DroppedOnCenterZone_IsRejected()
        {
            // "Attack" needs a target, so the center zone must never accept it.
            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_AttackSingleTarget_DroppedOutsideEveryZone_IsRejected()
        {
            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(5, 5),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Poison_DroppedOnEnemy_ReturnsThatEnemyEvenWhenMultiTargetFlagIsSet()
        {
            // "Poison" is an explicit table entry, so the isMultipleTargets flag must be ignored.
            var result = CardDropResolver.Resolve(
                "Poison",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: true,
                new Vector2(100, 180),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Enemy, 0);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Poison_DroppedOnCenterZone_IsRejected()
        {
            // "Poison" is enemy-only: the multi-target flag must not redirect it to the center.
            var result = CardDropResolver.Resolve(
                "Poison",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: true,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Cleave_DroppedOnCenterZone_IsAccepted()
        {
            var result = CardDropResolver.Resolve(
                "Cleave",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: true,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Cleave_DroppedOnEnemy_IsRejected()
        {
            // Cleave hits everything, so it only accepts the center zone even over an enemy.
            var result = CardDropResolver.Resolve(
                "Cleave",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: true,
                new Vector2(100, 180),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Defense_DroppedOnCenterZone_IsAccepted()
        {
            var result = CardDropResolver.Resolve(
                "Defense",
                CardEffectType.Defense,
                isTargetingSelf: true,
                isMultipleTargets: false,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Defense_DroppedOnEnemy_IsRejected()
        {
            // Self-targeted card: an enemy drop is not a valid target.
            var result = CardDropResolver.Resolve(
                "Defense",
                CardEffectType.Defense,
                isTargetingSelf: true,
                isMultipleTargets: false,
                new Vector2(100, 180),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Mana_DroppedOnCenterZone_IsAccepted()
        {
            var result = CardDropResolver.Resolve(
                "Mana",
                CardEffectType.Support,
                isTargetingSelf: true,
                isMultipleTargets: false,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_Draw_DroppedOnCenterZone_IsAccepted()
        {
            var result = CardDropResolver.Resolve(
                "Draw",
                CardEffectType.Support,
                isTargetingSelf: true,
                isMultipleTargets: false,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_PointInsideTwoOverlappingEnemyRects_ReturnsFirstIndex()
        {
            // The shared fixture is deliberately non-overlapping, so this case needs its own
            // rect array: both rects contain (100, 180) and the loop must stop at the first hit
            // (index 0), never reporting a later overlap.
            var overlapping = new[]
            {
                new Rect2(60, 120, 81, 150),
                new Rect2(80, 140, 81, 150)
            };

            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(100, 180),
                CenterZone,
                overlapping);

            AssertThat(overlapping[0].HasPoint(new Vector2(100, 180))).IsTrue();
            AssertThat(overlapping[1].HasPoint(new Vector2(100, 180))).IsTrue();
            AssertResult(result, CardDropZone.Enemy, 0);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_PointOnExclusiveRightBottomEnemyEdge_IsRejected()
        {
            // Rect2.HasPoint is exclusive on the right/bottom edge, so position+size misses.
            // The edge point here is (141, 270): it IS inside CenterZone on Y, but "Attack" is not
            // center-droppable, so the result must still be None. This is the regression guard for
            // "enemy rects are tested before the center zone" plus the exclusive-edge behaviour.
            Vector2 edge = EnemyRects[0].Position + EnemyRects[0].Size;
            AssertThat(edge.X).IsEqual(141f);
            AssertThat(edge.Y).IsEqual(270f);
            AssertThat(EnemyRects[0].HasPoint(edge)).IsFalse();
            AssertThat(CenterZone.HasPoint(edge)).IsTrue();

            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                edge,
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_PointOnInclusiveLeftTopCenterEdge_IsAccepted()
        {
            // Rect2.HasPoint is inclusive on the left/top edge, so the zone's own position hits.
            Vector2 edge = CenterZone.Position;
            AssertThat(CenterZone.HasPoint(edge)).IsTrue();

            var result = CardDropResolver.Resolve(
                "Defense",
                CardEffectType.Defense,
                isTargetingSelf: true,
                isMultipleTargets: false,
                edge,
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_UnknownEffectWithAttackType_FallsBackToEnemyOnly()
        {
            // Unknown effect + plain attack => centerOnly is false, so the center zone is ignored.
            var result = CardDropResolver.Resolve(
                null,
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(5, 5),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.None, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_UnknownEffectWithMultiTargetFlag_FallsBackToCenterZone()
        {
            // Unknown effect + multi-target behaves like Cleave: center zone only.
            var result = CardDropResolver.Resolve(
                "Mystery",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: true,
                new Vector2(180, 350),
                CenterZone,
                EnemyRects);

            AssertResult(result, CardDropZone.Center, -1);
        }

        [TestCase]
        [RequireGodotRuntime]
        public void Resolve_NullEnemyRects_DoesNotThrowAndIsRejected()
        {
            var result = CardDropResolver.Resolve(
                "Attack",
                CardEffectType.Attack,
                isTargetingSelf: false,
                isMultipleTargets: false,
                new Vector2(5, 5),
                CenterZone,
                null);

            AssertResult(result, CardDropZone.None, -1);
        }

        /// <summary>
        /// Asserts the two <see cref="CardDropResult"/> members separately, since the framework has
        /// no built-in assertion for the record struct.
        /// </summary>
        private static void AssertResult(CardDropResult actual, CardDropZone expectedZone, int expectedEnemyIndex)
        {
            AssertThat(actual.Zone).IsEqual(expectedZone);
            AssertThat(actual.EnemyIndex).IsEqual(expectedEnemyIndex);
        }
    }
}
