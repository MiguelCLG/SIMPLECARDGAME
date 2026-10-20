using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    /// <summary>
    /// Covers the PathMap half of the shared deck viewer (issue 12): the top-bar %DeckButton
    /// opens Scenes/DeckOverlay.tscn read-only over the run piles, the button toggles it shut,
    /// the overlay's own CloseButton hides it, and a missing run (RunManager.CurrentRun == null)
    /// disables the button instead of crashing.
    /// </summary>
    [TestSuite]
    public class PathMapDeckOverlayTest
    {
        private const uint TransitionFrames = 31;
        private const uint MillisecondsPerFrame = 20;

        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckButton_RendersAtTopAndOpensOverlayWithRunPiles()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;
            SeedHandAndDiscard(run);

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/PathMap.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Button deckButton = sceneRoot.GetNode<Button>("%DeckButton");
            DeckOverlay overlay = sceneRoot.GetNode<DeckOverlay>("%DeckOverlay");

            // Top bar: the button lives in the HBox inside the top MarginContainer and is
            // visible/interactive (no tween here, but must be laid out and enabled).
            AssertThat(deckButton.Visible).IsTrue();
            AssertThat(deckButton.IsVisibleInTree()).IsTrue();
            AssertThat(deckButton.Disabled).IsFalse();
            AssertThat(deckButton.GetParent().GetParent() is MarginContainer).IsTrue();

            // Overlay starts closed with an empty grid.
            AssertThat(overlay.Visible).IsFalse();
            AssertThat(overlay.IsOpen).IsFalse();
            AssertThat(overlay.Grid.CardCount).IsEqual(0);

            string before = PileSignature(run);
            List<CardSaveData> deckList = run.Deck;

            deckButton.EmitSignal(BaseButton.SignalName.Pressed);
            await sceneRunner.SimulateFrames(4);

            AssertThat(overlay.Visible).IsTrue();
            AssertThat(overlay.IsOpen).IsTrue();

            // Grid lists every owned card across the three piles.
            int total = run.Deck.Count + run.Hand.Count + run.Discard.Count;
            AssertThat(total > 0).IsTrue();
            AssertThat(overlay.Grid.CardCount).IsEqual(total);

            // Label reports the pile counts in the documented format.
            Label countLabel = overlay.GetNode<Label>("%DeckCountLabel");
            AssertThat(countLabel.Text).IsEqual(
                $"Draw {run.Deck.Count} · Hand {run.Hand.Count} · Discard {run.Discard.Count}");
            AssertThat(countLabel.Text.Contains("Draw")).IsTrue();

            // Rendering is read-only: same signature, same list instances.
            AssertThat(PileSignature(run)).IsEqual(before);
            AssertThat(ReferenceEquals(run.Deck, deckList)).IsTrue();
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task DeckButton_Toggle_ClosesTheOpenOverlay()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;
            string before = PileSignature(run);

            ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/PathMap.tscn", autoFree: true);
            await sceneRunner.SimulateFrames(30);

            Node sceneRoot = sceneRunner.Scene();
            Button deckButton = sceneRoot.GetNode<Button>("%DeckButton");
            DeckOverlay overlay = sceneRoot.GetNode<DeckOverlay>("%DeckOverlay");

            deckButton.EmitSignal(BaseButton.SignalName.Pressed);
            await sceneRunner.SimulateFrames(4);
            AssertThat(overlay.Visible).IsTrue();

            // Pressing the deck button again runs ToggleDeck's hide branch.
            deckButton.EmitSignal(BaseButton.SignalName.Pressed);
            await sceneRunner.SimulateFrames(4);
            AssertThat(overlay.Visible).IsFalse();
            AssertThat(overlay.IsOpen).IsFalse();
            AssertThat(PileSignature(run)).IsEqual(before);
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task CloseButton_HidesOverlay_AndRunPilesStayUnchanged()
        {
            RunManager.NewRun();
            RunData run = RunManager.CurrentRun;
            SeedHandAndDiscard(run);

            // Protect the on-disk run state this test only reads: EndRun-free, but NewRun
            // overwrote the auto-save, so back both files up like RewardFlowTest does.
            string runsPath = Path.Combine(OS.GetUserDataDir(), "runs.json");
            string savePath = Path.Combine(OS.GetUserDataDir(), "savegame.json");
            string runsBackup = File.Exists(runsPath) ? File.ReadAllText(runsPath) : null;
            string saveBackup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;

            try
            {
                ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/PathMap.tscn", autoFree: true);
                await sceneRunner.SimulateFrames(30);

                Node sceneRoot = sceneRunner.Scene();
                Button deckButton = sceneRoot.GetNode<Button>("%DeckButton");
                DeckOverlay overlay = sceneRoot.GetNode<DeckOverlay>("%DeckOverlay");
                Button close = overlay.CloseButton;

                string before = PileSignature(run);
                List<CardSaveData> deckList = run.Deck;
                List<CardSaveData> handList = run.Hand;
                List<CardSaveData> discardList = run.Discard;
                int deckCount = run.Deck.Count;
                int handCount = run.Hand.Count;
                int discardCount = run.Discard.Count;

                deckButton.EmitSignal(BaseButton.SignalName.Pressed);
                await sceneRunner.SimulateFrames(4);
                AssertThat(overlay.Visible).IsTrue();

                close.EmitSignal(BaseButton.SignalName.Pressed);
                await sceneRunner.SimulateFrames(4);

                // Hidden, and PathMap is still the live scene (no redirect happened).
                AssertThat(overlay.Visible).IsFalse();
                AssertThat(ReferenceEquals(sceneRunner.Scene(), sceneRoot)).IsTrue();
                AssertThat(sceneRoot.IsInsideTree()).IsTrue();
                AssertThat(deckButton.IsVisibleInTree()).IsTrue();
                AssertThat(deckButton.Disabled).IsFalse();

                // Read-only viewer: counts, order, and list instances all untouched.
                AssertThat(run.Deck.Count).IsEqual(deckCount);
                AssertThat(run.Hand.Count).IsEqual(handCount);
                AssertThat(run.Discard.Count).IsEqual(discardCount);
                AssertThat(PileSignature(run)).IsEqual(before);
                AssertThat(ReferenceEquals(run.Deck, deckList)).IsTrue();
                AssertThat(ReferenceEquals(run.Hand, handList)).IsTrue();
                AssertThat(ReferenceEquals(run.Discard, discardList)).IsTrue();
            }
            finally
            {
                RestoreFile(runsPath, runsBackup);
                RestoreFile(savePath, saveBackup);
            }
        }

        [TestCase]
        [RequireGodotRuntime]
        public async Task NullRun_DisablesDeckButtonAndRedirectsWithoutCrash()
        {
            // EndRun() appends to runs.json and deletes the auto-save — back both up.
            string runsPath = Path.Combine(OS.GetUserDataDir(), "runs.json");
            string savePath = Path.Combine(OS.GetUserDataDir(), "savegame.json");
            string runsBackup = File.Exists(runsPath) ? File.ReadAllText(runsPath) : null;
            string saveBackup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;

            try
            {
                RunManager.NewRun();
                RunManager.EndRun(false);
                AssertThat(RunManager.CurrentRun).IsNull();

                ISceneRunner sceneRunner = ISceneRunner.Load("res://Scenes/PathMap.tscn", autoFree: true);

                // _Ready disables the deck button before redirecting to the menu. The
                // redirect may free the runner's scene (ChangeSceneToFile), so poll with
                // IsInstanceValid guards instead of assuming the scene survives.
                Button deckButton = await FindDeckButton(sceneRunner);
                if (deckButton != null)
                {
                    AssertThat(deckButton.Disabled).IsTrue();

                    // Exercise OnDeckPressed's own null-guard even though a disabled
                    // button would swallow real input — the handler must not crash.
                    deckButton.EmitSignal(BaseButton.SignalName.Pressed);
                }

                // Reaching this line after the redirect frames = no crash.
                await sceneRunner.SimulateFrames(TransitionFrames, MillisecondsPerFrame);

                Node sceneRoot = sceneRunner.Scene();
                if (GodotObject.IsInstanceValid(sceneRoot))
                {
                    DeckOverlay overlay = sceneRoot.GetNodeOrNull<DeckOverlay>("%DeckOverlay");
                    if (overlay != null)
                        AssertThat(overlay.Visible).IsFalse();
                }
            }
            finally
            {
                RestoreFile(runsPath, runsBackup);
                RestoreFile(savePath, saveBackup);
            }
        }

        /// <summary>
        /// Waits (without long transitions — _Ready runs synchronously on add-to-tree) for
        /// the PathMap instance to be inside the tree and return its %DeckButton, or null
        /// if the scene was already swapped out by the null-run redirect.
        /// </summary>
        private static async Task<Button> FindDeckButton(ISceneRunner sceneRunner)
        {
            for (int i = 0; i < 5; i++)
            {
                Node sceneRoot = sceneRunner.Scene();
                if (GodotObject.IsInstanceValid(sceneRoot) && sceneRoot.IsInsideTree())
                {
                    Button button = sceneRoot.GetNodeOrNull<Button>("%DeckButton");
                    if (button != null) return button;
                }
                await sceneRunner.SimulateFrames(1);
            }
            return null;
        }

        /// <summary>Moves two cards out of the freshly seeded deck so every pile is non-empty.</summary>
        private static void SeedHandAndDiscard(RunData run)
        {
            CardSaveData handCard = run.Deck[0];
            CardSaveData discardCard = run.Deck[1];
            run.Deck.RemoveRange(0, 2);
            run.Hand.Add(handCard);
            run.Discard.Add(discardCard);
        }

        /// <summary>
        /// Counts plus order-sensitive card names per pile — catches any mutation (add,
        /// remove, or reorder) caused by opening/closing the viewer. CardSaveData does not
        /// override Equals, so SequenceEqual compares element identity.
        /// </summary>
        private static string PileSignature(RunData run) =>
            $"{run.Deck.Count}/{run.Hand.Count}/{run.Discard.Count} :: " +
            $"{Key(run.Deck)} | {Key(run.Hand)} | {Key(run.Discard)}";

        private static string Key(List<CardSaveData> pile) =>
            string.Join(",", pile.Select(c => c.CardName ?? "?"));

        private static void RestoreFile(string path, string backup)
        {
            if (backup == null)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else
            {
                File.WriteAllText(path, backup);
            }
        }
    }
}
