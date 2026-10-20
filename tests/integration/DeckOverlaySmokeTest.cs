using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    [TestSuite]
    public class DeckOverlaySmokeTest
    {
        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckButton_TogglesOverlayAndListsEveryOwnedCard()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            Panel overlay = sceneRoot.GetNode<Panel>("%DeckOverlay");
            AssertThat(overlay.Visible).IsFalse();

            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(2);

            AssertThat(overlay.Visible).IsTrue();
            CardGrid grid = sceneRoot.GetNode<CardGrid>("%CardGrid");
            int owned = player.Deck.Count + player.Hand.Count + player.DiscardPile.Count;
            AssertThat(grid.CardCount).IsEqual(owned);

            GridContainer container = grid.GetChild<GridContainer>(0);
            AssertThat(container.GetChildCount()).IsEqual(owned);
            foreach (Node child in container.GetChildren())
            {
                // Cards live in expanding cells so the grid spreads them over the overlay.
                AssertThat(child is CenterContainer).IsTrue();
                CenterContainer cell = (CenterContainer)child;
                AssertThat(cell.SizeFlagsHorizontal).IsEqual(Control.SizeFlags.ExpandFill);
                AssertThat(cell.SizeFlagsVertical).IsEqual(Control.SizeFlags.ExpandFill);
                AssertThat(cell.GetChildCount()).IsEqual(1);
                AssertThat(cell.GetChild(0) is Card).IsTrue();
                AssertThat(((Card)cell.GetChild(0)).FocusMode).IsEqual(Control.FocusModeEnum.None);
            }

            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(2);
            AssertThat(overlay.Visible).IsFalse();
        }

        /// <summary>
        /// Regression: cards left in hand/discard when an encounter ends used to stay in
        /// those piles for the next fight, so the draw deck kept shrinking. A fresh
        /// encounter must pool everything back into the deck and start with discard 0.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task FreshEncounter_PoolsHandAndDiscardBackIntoDeck()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;

            // State left behind by a decided encounter: some cards in hand and discard.
            CardSaveData handCard = run.Deck[0];
            CardSaveData discardCard = run.Deck[1];
            run.Deck.RemoveRange(0, 2);
            run.Hand.Add(handCard);
            run.Discard.Add(discardCard);

            int total = run.Deck.Count + run.Hand.Count + run.Discard.Count;
            AssertThat(run.ActiveEncounter).IsFalse();
            AssertThat(run.Enemies.Count).IsEqual(0);

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Player player = sceneRunner.Scene().GetNode<Player>("%Player");
            AssertThat(player.DiscardPile.Count).IsEqual(0);
            AssertThat(player.Hand.Count).IsEqual(3); // first turn draws a fresh hand
            AssertThat(player.Deck.Count + player.Hand.Count + player.DiscardPile.Count).IsEqual(total);
        }

        /// <summary>
        /// Regression (issue 12): the full-screen deck overlay composites ABOVE every live
        /// enemy. Scenes/EnemyUI.tscn sets z_index = 1 on its root while DeckOverlay used to
        /// keep the default 0 on the same CanvasLayer, and Godot composites by effective
        /// z_index before tree order — so enemies painted over the overlay's card grid.
        /// Also guards that the overlay's own close button stays on screen and that closing
        /// the overlay returns to a playable encounter where enemy targeting still works.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckOverlay_DrawsAboveLiveEnemies_AndCloseButtonIsOnScreen()
        {
            RunManager.NewRun();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            Panel overlay = sceneRoot.GetNode<Panel>("%DeckOverlay");
            Button close = sceneRoot.GetNode<Button>("%DeckCloseButton");
            Node enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");

            List<Enemy> enemies = new();
            foreach (Node child in enemySpawn.GetChildren())
            {
                if (child is Enemy enemy) enemies.Add(enemy);
            }

            AssertThat(overlay.Visible).IsFalse();
            AssertThat(enemies.Count >= 1).IsTrue();

            // Open the overlay the way the in-game Deck button does.
            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(4);
            AssertThat(overlay.Visible).IsTrue();

            // The regression itself: every live enemy subtree draws below the overlay subtree.
            foreach (Enemy enemy in enemies)
            {
                AssertThat(DrawsAbove(overlay, enemy)).IsTrue();
                AssertThat(DrawsAbove(enemy, overlay)).IsFalse();
            }

            // Narrow smoke guard over the overlay's own chrome: the Close button is visible
            // and still laid out inside the overlay's content column (VBoxContainer), i.e. it
            // was not hidden, collapsed, or detached while the grid re-rendered. Looked up by
            // name on purpose so a child-order change in the scene cannot pick the wrong node.
            //
            // Honest about its limits: this does NOT prove the button stays on screen.
            // Godot clamps a Control's size to its combined minimum size, so an anchored
            // VBoxContainer grows to fit its children and always encloses them — even when it
            // overflows the 360x718 viewport. On top of that the headless gdUnit4 runner lays
            // scenes out in a 64x64 viewport (verified by probe), so a viewport-containment
            // check would be meaningless here. Overflow is therefore left uncovered.
            AssertThat(close.IsVisibleInTree()).IsTrue();
            AssertThat(close.GetGlobalRect().Size.Y > 0).IsTrue();
            Control vbox = overlay.GetNode<Control>("VBoxContainer");
            AssertThat(vbox.GetGlobalRect().Encloses(close.GetGlobalRect())).IsTrue();

            // Closing returns to a playable encounter: a single-target Attack card the
            // player can actually afford still resolves through card click -> enemy click.
            close.EmitSignal(BaseButton.SignalName.Pressed);
            await sceneRunner.SimulateFrames(2);
            AssertThat(overlay.Visible).IsFalse();

            Enemy target = enemies[0];
            // Zero the target's armor so the damage assertion proves "the card landed"
            // rather than coinciding with this enemy's starting armor stat.
            target.Armor = 0;
            int healthBefore = target.Health;
            int cardsPlayedBefore = RunManager.CurrentRun.Stats.CardsPlayed;

            // Cost must fit MaxMana or PlayCard silently no-ops (mana is refilled below).
            CardSaveData attack = player.Deck.FirstOrDefault(
                d => d.EffectString == "Attack" && !d.isMultipleTargets && d.Cost <= player.MaxMana
            );
            AssertThat(attack).IsNotNull();

            player.SetManaToMax();
            player.SpawnCard(attack);
            Card card = player.Hand[player.Hand.Count - 1];
            card.EmitSignal(BaseButton.SignalName.Pressed);
            await sceneRunner.SimulateFrames(2);
            target.EmitSignal(BaseButton.SignalName.Pressed);

            // CardsPlayed is the primary proof the card resolved; the health drop confirms
            // its damage actually landed on the intended target.
            AssertThat(RunManager.CurrentRun.Stats.CardsPlayed).IsEqual(cardsPlayedBefore + 1);
            AssertThat(target.Health < healthBefore).IsTrue();
        }

        /// <summary>
        /// The same invariant on the highest-risk run state: the boss row, where the encounter
        /// is a single ×1.5-health enemy. The fix is state-independent (one CanvasLayer with
        /// the overlay at z 100 vs the enemy at z 1), but the boss is the fight a player is
        /// most likely to reach with a full deck, so it gets one focused check of its own.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckOverlay_DrawsAboveBossEnemyOnBossRow()
        {
            RunManager.NewRun();

            // Deterministically reach the boss row through the public run API: paths are
            // random per run, so pick the first selectable column at each step rather than
            // hard-coding a route, then choose the single boss node of the final row.
            for (int row = 0; row < PathGenerator.StepRows; row++)
            {
                RunData run = RunManager.CurrentRun;
                int col = Enumerable.Range(0, run.Path[row].Count).First(c => RunManager.IsSelectable(row, c));
                AssertThat(RunManager.ChooseNode(row, col)).IsTrue();
                RunManager.CompleteCurrentNode();
            }
            AssertThat(RunManager.ChooseNode(PathGenerator.StepRows, 0)).IsTrue();
            AssertThat(RunManager.CurrentRun.IsBossNode).IsTrue();

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Player player = sceneRoot.GetNode<Player>("%Player");
            Panel overlay = sceneRoot.GetNode<Panel>("%DeckOverlay");
            Node enemySpawn = sceneRoot.GetNode<Node>("%EnemySpawn");

            List<Enemy> enemies = new();
            foreach (Node child in enemySpawn.GetChildren())
            {
                if (child is Enemy enemy) enemies.Add(enemy);
            }

            AssertThat(enemies.Count).IsEqual(1); // a boss row spawns exactly one enemy
            Enemy boss = enemies[0];

            player.OnDeckViewPress();
            await sceneRunner.SimulateFrames(4);
            AssertThat(overlay.Visible).IsTrue();

            AssertThat(DrawsAbove(overlay, boss)).IsTrue();
            AssertThat(DrawsAbove(boss, overlay)).IsFalse();
        }

        /// <summary>Walks up from a node to the CanvasLayer it is rendered into.</summary>
        private static CanvasLayer EnclosingLayer(Node node)
        {
            Node current = node;
            while (current != null)
            {
                if (current is CanvasLayer layer) return layer;
                current = current.GetParent();
            }
            return null;
        }

        /// <summary>
        /// Effective z of a CanvasItem: sums ZIndex up the CanvasItem chain while
        /// ZAsRelative holds, stopping at the enclosing CanvasLayer (which has no z_index).
        /// </summary>
        private static int EffectiveZ(CanvasItem item)
        {
            int z = 0;
            Node current = item;
            while (current is CanvasItem canvasItem)
            {
                z += canvasItem.ZIndex;
                if (!canvasItem.ZAsRelative) break;
                current = canvasItem.GetParent();
            }
            return z;
        }

        /// <summary>Index path from the scene root down to the node (root-most child first).</summary>
        private static List<int> IndexPath(Node node)
        {
            List<int> path = new();
            Node current = node;
            while (current != null && current.GetParent() != null)
            {
                path.Add(current.GetIndex());
                current = current.GetParent();
            }
            path.Reverse();
            return path;
        }

        /// <summary>True when a is painted after b in a plain depth-first tree walk.</summary>
        private static bool IsLaterInTree(Node a, Node b)
        {
            List<int> pathA = IndexPath(a);
            List<int> pathB = IndexPath(b);
            int shared = Math.Min(pathA.Count, pathB.Count);
            for (int i = 0; i < shared; i++)
            {
                if (pathA[i] != pathB[i]) return pathA[i] > pathB[i];
            }
            return pathA.Count > pathB.Count;
        }

        /// <summary>
        /// Mechanism-agnostic paint order: higher CanvasLayer.Layer wins, then effective
        /// z_index, then depth-first tree order — Godot's own compositing precedence.
        /// </summary>
        private static bool DrawsAbove(CanvasItem above, CanvasItem below)
        {
            int aboveLayer = EnclosingLayer(above)?.Layer ?? 0;
            int belowLayer = EnclosingLayer(below)?.Layer ?? 0;
            if (aboveLayer != belowLayer) return aboveLayer > belowLayer;

            int aboveZ = EffectiveZ(above);
            int belowZ = EffectiveZ(below);
            if (aboveZ != belowZ) return aboveZ > belowZ;

            return IsLaterInTree(above, below);
        }
    }
}