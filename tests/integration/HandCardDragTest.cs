using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace SpaceOdyssey.Tests.Integration
{
    /// <summary>
    /// Covers the in-hand card gesture end to end on the real <c>Encounter.tscn</c>
    /// (a card bound outside a GameManager ancestor can never hold, so there is no bare-card seam):
    /// rest/held geometry (authored 81x150 rect rendered at 0.5/1.0 inside a 40.5x75 CardSlot),
    /// pointer follow without drift during the scale tween, drag-to-enemy, drag-to-center, native
    /// touch ownership, and every cancel path (dead zone, insufficient mana, Escape).
    /// Also pins the boundary that only hand cards shrink — <see cref="CardUi"/> views keep their
    /// authored 81x150 size and stay non-interactive — and that a plain click still plays through
    /// the existing <c>OnCardClick</c> path exactly once.
    /// </summary>
    [TestSuite]
    public class HandCardDragTest
    {
        // Real image: Player.SpawnCard calls ResourceLoader.Load(data.CardImage) unguarded.
        private const string CardImagePath = "res://Images/inkscape_exports/card-icons/attack.webp";

        // Authored card size in Scenes/card.tscn; hand cards render at half the authored scale.
        private static readonly Vector2 AuthoredCardSize = new(81f, 150f);

        // PressTweenSeconds = 0.12 s, ReleaseTweenSeconds = 0.10 s.
        private const uint SettleFrames = 10;
        private const uint ShortSettleFrames = 8;
        private const uint MsPerFrame = 20;

        // Half a pixel is far below the 40.5x75 rest slot, so it cannot hide a geometry mistake.
        private const float GeometryTolerance = 0.5f;

        // Pointer positions are exact math here (no physics), so 1 px is generous.
        private const float PointerTolerance = 1f;

        /// <summary>
        /// Rest geometry: the layout rect stays the authored 81x150 (the authored internals need
        /// it), it renders at <see cref="HandCardInteraction.RestScale"/> offset by
        /// <see cref="HandCardInteraction.RestOffset"/>, and it lives in a plain Control slot of
        /// exactly <see cref="HandCardInteraction.HandCardSlotSize"/>. The *visual* rect is
        /// therefore the half-size slot, not the authored card.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task Rest_KeepsAuthoredLayoutRect_InsideHalfSizeCardSlot()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            AssertThat(ctx.Player.Hand.Count >= 1).IsTrue();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            // The layout rect never shrinks: only Scale does.
            AssertThat(card.Size).IsEqual(HandCardInteraction.AuthoredCardSize);
            AssertThat(card.CustomMinimumSize).IsEqual(HandCardInteraction.AuthoredCardSize);
            AssertThat(card.CustomMaximumSize).IsEqual(HandCardInteraction.AuthoredCardSize);

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Scale.Y).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);

            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);

            // The card sits in its own slot, and the slot (not the card) is the GridContainer child.
            Node parent = card.GetParent();
            AssertThat(parent is Control).IsTrue();
            var slot = (Control)parent;

            // A plain Control, NOT a Container: the GridContainer lays the slot out and the card is
            // then free to be scaled/offset inside it (a Container would overwrite its position).
            AssertThat(slot is Container).IsFalse();
            AssertThat(slot.CustomMinimumSize).IsEqual(HandCardInteraction.HandCardSlotSize);
            AssertThat(slot.CustomMaximumSize).IsEqual(HandCardInteraction.HandCardSlotSize);
            AssertThat(slot.GetParent() is GridContainer).IsTrue();
            AssertThat(slot.GetChildCount()).IsEqual(1);
            AssertThat(slot.GetChild(0)).IsSame(card);

            // Name: SpawnCard names every slot "CardSlot", but Godot keeps that name only for the
            // FIRST slot of the hand. The 2nd+ sibling with the same requested name falls back to
            // an engine-generated "@Control@<n>" (observed on this 4.7 build), so the product's
            // intent is asserted as "one of those two forms". Nothing looks a slot up by name
            // (Player.DiscardCard walks the parent chain), so this stays purely descriptive.
            string slotName = slot.Name.ToString();
            AssertThat(slotName == "CardSlot" || slotName.StartsWith("@Control@"))
                .OverrideFailureMessage(
                    $"card slot name is '{slotName}'; expected 'CardSlot' or the engine fallback '@Control@<n>'")
                .IsTrue();

            // What the player sees at rest is the slot-sized visual, i.e. half the authored card.
            Rect2 visual = interaction.VisualRectForTest();
            AssertThat(visual.Size.X).IsEqualApprox(HandCardInteraction.HandCardSlotSize.X, GeometryTolerance);
            AssertThat(visual.Size.Y).IsEqualApprox(HandCardInteraction.HandCardSlotSize.Y, GeometryTolerance);
            AssertThat(visual.Size.X).IsEqualApprox(AuthoredCardSize.X * HandCardInteraction.RestScale, GeometryTolerance);
            AssertThat(visual.Size.Y).IsEqualApprox(AuthoredCardSize.Y * HandCardInteraction.RestScale, GeometryTolerance);

            // ... and it is centred in the slot, not shoved into a corner.
            AssertVectorApprox(
                visual.GetCenter(),
                slot.GetGlobalRect().GetCenter(),
                GeometryTolerance,
                "rest visual centre inside CardSlot");
        }

        /// <summary>
        /// Holding grows the card to the full authored size around its centre and lifts it above
        /// its siblings with ZIndex — the hand is a GridContainer, so reordering children would
        /// visibly re-sort the grid.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task Hold_EnlargesToAuthoredSize_WithoutReorderingHand()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            Node slot = card.GetParent();
            int slotIndexBefore = slot.GetIndex();
            Vector2 centreBefore = interaction.VisualCentreForTest();

            interaction.PressForTest(centreBefore);
            AssertThat(interaction.IsHeldForTest()).IsTrue();

            await ctx.Runner.SimulateFrames(SettleFrames, MsPerFrame); // 200 ms > 0.12 s press tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.HeldScale, 0.01f);
            AssertThat(card.Scale.Y).IsEqualApprox(HandCardInteraction.HeldScale, 0.01f);

            Rect2 visual = interaction.VisualRectForTest();
            AssertThat(visual.Size.X).IsEqualApprox(AuthoredCardSize.X, GeometryTolerance);
            AssertThat(visual.Size.Y).IsEqualApprox(AuthoredCardSize.Y, GeometryTolerance);

            AssertThat(card.ZIndex).IsEqual(HandCardInteraction.HeldZIndex);

            // No sibling reorder: the card stays in the same hand cell, and its slot keeps the
            // very same index in the GridContainer.
            AssertThat(slot.GetIndex()).IsEqual(slotIndexBefore);
            AssertThat(card.GetParent()).IsSame(slot);

            // The hold tween scales around the pivot, so it must not move the visual centre.
            AssertVectorApprox(
                interaction.VisualCentreForTest(),
                centreBefore,
                PointerTolerance,
                "visual centre after hold tween");

            interaction.EscapeForTest(); // cleanup: leave no gesture running behind
        }

        /// <summary>
        /// The held card follows the pointer, and the centre STAYS under the pointer while the
        /// 0.5 -> 1.0 scale tween runs: the tween changes Scale every frame, so without the
        /// per-frame position lock the visual would drift away from the finger mid-tween.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task Hold_FollowsPointer_WithoutDriftingDuringScaleTween()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            Vector2 startCentre = interaction.VisualCentreForTest();
            Vector2 delta = new(40f, -30f);
            Vector2 target = startCentre + delta;
            Vector2 positionAtRest = card.Position;

            interaction.PressForTest(startCentre);
            AssertThat(interaction.IsHeldForTest()).IsTrue();

            interaction.PointerFollowForTest(target);

            // The press grabbed the visual centre, so the target centre is the pointer itself.
            AssertVectorApprox(interaction.VisualCentreForTest(), target, PointerTolerance, "centre right after the follow");

            // The card really moved: at rest scale the offset translation is (40,-30) on top of
            // RestOffset, so asserting "not the rest position" alone would be too weak.
            AssertThat(card.Position.DistanceTo(positionAtRest) > 20f).IsTrue();
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X + delta.X, PointerTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y + delta.Y, PointerTolerance);

            // Sample mid-tween (2 of 6 frames of the 0.12 s press tween): the scale must really be
            // between rest and held, otherwise the no-drift check below would be vacuous.
            await ctx.Runner.SimulateFrames(2, MsPerFrame);
            AssertThat(card.Scale.X > HandCardInteraction.RestScale + 0.01f).IsTrue();
            AssertThat(card.Scale.X < HandCardInteraction.HeldScale - 0.01f).IsTrue();
            AssertVectorApprox(interaction.VisualCentreForTest(), target, PointerTolerance, "centre mid-tween");

            // ... and still no drift once the tween finished.
            await ctx.Runner.SimulateFrames(SettleFrames, MsPerFrame);
            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.HeldScale, 0.01f);
            AssertVectorApprox(interaction.VisualCentreForTest(), target, PointerTolerance, "centre after the press tween");

            interaction.EscapeForTest(); // cleanup: leave no gesture running behind
        }

        /// <summary>
        /// Release without crossing the drag threshold stays a click: the hold is undone, the card
        /// glides back to <see cref="HandCardInteraction.RestOffset"/> at
        /// <see cref="HandCardInteraction.RestScale"/> with its original ZIndex, and nothing is
        /// played (no mana, no discard, no played-card stat).
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task HoldAndReleaseWithoutDrag_RestoresRestGeometry_AndPlaysNothing()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);
            int zIndexBefore = card.ZIndex;
            int cardsPlayedBefore = CardsPlayed();
            int manaBefore = ctx.Player.Mana;
            Vector2 centreBefore = interaction.VisualCentreForTest();

            interaction.PressForTest(centreBefore);
            await ctx.Runner.SimulateFrames(SettleFrames, MsPerFrame);
            AssertThat(interaction.IsHeldForTest()).IsTrue();
            AssertThat(card.ZIndex).IsEqual(HandCardInteraction.HeldZIndex);

            interaction.ReleaseForTest(centreBefore);
            AssertThat(interaction.IsHeldForTest()).IsFalse();
            AssertThat(interaction.IsDraggingForTest()).IsFalse();

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(interaction.IsHeldForTest()).IsFalse();
            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Scale.Y).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(card.ZIndex).IsEqual(zIndexBefore);

            // Back to the slot-sized visual, centred in its slot again.
            Rect2 visual = interaction.VisualRectForTest();
            AssertThat(visual.Size.X).IsEqualApprox(HandCardInteraction.HandCardSlotSize.X, GeometryTolerance);
            AssertThat(visual.Size.Y).IsEqualApprox(HandCardInteraction.HandCardSlotSize.Y, GeometryTolerance);
            AssertVectorApprox(interaction.VisualCentreForTest(), centreBefore, PointerTolerance, "rest centre after release");

            AssertThat(ctx.Player.Hand.Contains(card)).IsTrue();
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
            AssertThat(ctx.Player.Mana).IsEqual(manaBefore);
        }

        /// <summary>
        /// The main drag path: dragging an Attack card onto an enemy targets exactly that enemy and
        /// runs the normal play pipeline (discard, mana, stats).
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DragAttackOntoEnemy_PlaysOnThatEnemy()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;

            Enemy enemy = FirstEnemy(ctx);
            player.Mana = 10;

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            int healthBefore = enemy.Health;
            int manaBefore = player.Mana;
            int handBefore = player.Hand.Count;
            int discardBefore = player.DiscardPile.Count;
            int cardsPlayedBefore = CardsPlayed();

            // A zero-sized rect would make the drop point meaningless and the test vacuous.
            Rect2 enemyRect = enemy.GetGlobalRect();
            AssertThat(enemyRect.Size.X > 0f && enemyRect.Size.Y > 0f).IsTrue();
            Vector2 dropPoint = enemyRect.GetCenter();

            interaction.PressForTest(interaction.VisualCentreForTest());
            interaction.DragToForTest(dropPoint);
            AssertThat(interaction.IsDraggingForTest()).IsTrue();
            interaction.ReleaseForTest(dropPoint);

            // The card is QueueFree'd by a successful play — never touch it from here on.
            AssertThat(enemy.Health).IsEqual(healthBefore - 5);
            AssertThat(player.Mana).IsEqual(manaBefore - 1);
            AssertThat(player.Hand.Any(c => c.CardName == "T_Attack")).IsFalse();
            AssertThat(player.Hand.Count).IsEqual(handBefore - 1);
            AssertThat(player.DiscardPile.Count).IsEqual(discardBefore + 1);
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore + 1);
        }

        /// <summary>
        /// A self-targeted card dropped in the viewport centre zone applies to the player.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DragDefenseToCenter_AddsArmor()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;
            player.Mana = 10;

            Card card = await SpawnCardIntoHand(ctx, DefenseCard());
            HandCardInteraction interaction = InteractionOf(card);

            int armorBefore = player.Armor;
            int manaBefore = player.Mana;

            Vector2 dropPoint = CenterZoneFor(card).GetCenter();

            interaction.PressForTest(interaction.VisualCentreForTest());
            interaction.DragToForTest(dropPoint);
            interaction.ReleaseForTest(dropPoint);

            AssertThat(player.Armor).IsEqual(armorBefore + 5);
            AssertThat(player.Mana).IsEqual(manaBefore - 1);
            AssertThat(player.Hand.Any(c => c.CardName == "T_Defense")).IsFalse();
        }

        /// <summary>
        /// Dropping outside every zone cancels: the card stays in the hand (same hand size, nothing
        /// spent), and the enlarged visual glides back to the rest offset/scale in its slot.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DragToDeadZone_Cancels()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;

            FirstEnemy(ctx);
            player.Mana = 10;

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            Vector2 deadDrop = new(5f, 5f);

            // Precondition: this point really is outside every accepted zone.
            AssertThat(CenterZoneFor(card).HasPoint(deadDrop)).IsFalse();
            foreach (Enemy enemy in ctx.GameManager.GetLiveEnemies())
                AssertThat(enemy.GetGlobalRect().HasPoint(deadDrop)).IsFalse();

            int manaBefore = player.Mana;
            int handBefore = player.Hand.Count;
            int cardsPlayedBefore = CardsPlayed();
            int zIndexBefore = card.ZIndex;

            interaction.PressForTest(interaction.VisualCentreForTest());
            interaction.DragToForTest(deadDrop);
            AssertThat(interaction.IsDraggingForTest()).IsTrue();
            interaction.ReleaseForTest(deadDrop);

            AssertThat(player.Hand.Contains(card)).IsTrue();
            AssertThat(player.Hand.Count).IsEqual(handBefore);
            AssertThat(player.Mana).IsEqual(manaBefore);
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
            AssertThat(card.ZIndex).IsEqual(zIndexBefore);
            AssertThat(interaction.IsHeldForTest()).IsFalse();

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Scale.Y).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(interaction.VisualRectForTest().Size.X)
                .IsEqualApprox(HandCardInteraction.HandCardSlotSize.X, GeometryTolerance);
        }

        /// <summary>
        /// An accepted drop still cancels when the card is unaffordable:
        /// GameManager.PlayCardFromDrag refuses before Player.PlayCard mutates anything.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task DragWithInsufficientMana_Cancels()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;

            Enemy enemy = FirstEnemy(ctx);
            player.Mana = 0;

            Card card = await SpawnCardIntoHand(ctx, AttackCard()); // Cost = 1
            HandCardInteraction interaction = InteractionOf(card);

            int healthBefore = enemy.Health;
            Vector2 dropPoint = enemy.GetGlobalRect().GetCenter();

            interaction.PressForTest(interaction.VisualCentreForTest());
            interaction.DragToForTest(dropPoint);
            interaction.ReleaseForTest(dropPoint);

            AssertThat(enemy.Health).IsEqual(healthBefore);
            AssertThat(player.Hand.Contains(card)).IsTrue();
            AssertThat(player.Mana).IsEqual(0);

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame);
            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
        }

        /// <summary>
        /// Escape mid-hold abandons the gesture: no play, rest geometry restored, card still in hand.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task EscapeDuringHold_Cancels()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);
            int zIndexBefore = card.ZIndex;

            int manaBefore = player.Mana;
            int cardsPlayedBefore = CardsPlayed();

            interaction.PressForTest(interaction.VisualCentreForTest());
            AssertThat(interaction.IsHeldForTest()).IsTrue();

            interaction.EscapeForTest();
            AssertThat(interaction.IsHeldForTest()).IsFalse();

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(card.ZIndex).IsEqual(zIndexBefore);
            AssertThat(player.Hand.Contains(card)).IsTrue();
            AssertThat(player.Mana).IsEqual(manaBefore);
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
        }

        /// <summary>
        /// Native touch: the first finger that starts a hold owns the gesture. A second finger is
        /// ignored entirely (no re-press, no move, no release), and releasing with the owning finger
        /// ends the gesture and clears the latch.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task Touch_FirstFingerOwnsGesture_SecondFingerIsIgnored()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            Vector2 centre = interaction.VisualCentreForTest();
            Vector2 otherPoint = centre + new Vector2(60f, -100f);

            interaction.TouchPressForTest(0, centre);
            AssertThat(interaction.IsHeldForTest()).IsTrue();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(0);

            // Second finger: must neither re-press nor steal ownership nor move the card.
            interaction.TouchPressForTest(1, otherPoint);
            AssertThat(interaction.IsHeldForTest()).IsTrue();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(0);
            AssertVectorApprox(interaction.VisualCentreForTest(), centre, PointerTolerance, "centre after second finger press");

            // A foreign drag and a foreign release are both no-ops.
            interaction.TouchDragForTest(1, otherPoint);
            AssertVectorApprox(interaction.VisualCentreForTest(), centre, PointerTolerance, "centre after foreign touch drag");

            interaction.TouchReleaseForTest(1, otherPoint);
            AssertThat(interaction.IsHeldForTest()).IsTrue();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(0);

            // The owning finger releases: gesture ends, latch cleared, rest geometry restored.
            interaction.TouchReleaseForTest(0, centre);
            AssertThat(interaction.IsHeldForTest()).IsFalse();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(-1);

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(ctx.Player.Hand.Contains(card)).IsTrue();
        }

        /// <summary>
        /// A plain click is untouched by the gesture layer: the hold/release pair itself plays
        /// nothing, and the Button's own <c>pressed</c> signal still reaches
        /// <c>Card.OnButtonPressed</c> -&gt; <c>OnCardClick</c> and plays the card exactly once
        /// (a double play would cost 2 mana and bump CardsPlayed by 2).
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task PlainClick_StillPlaysThroughOnCardClick_ExactlyOnce()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;
            player.Mana = 10;

            Card card = await SpawnCardIntoHand(ctx, DefenseCard()); // self-targeted: plays on click
            HandCardInteraction interaction = InteractionOf(card);

            int armorBefore = player.Armor;
            int manaBefore = player.Mana;
            int cardsPlayedBefore = CardsPlayed();

            Vector2 centre = interaction.VisualCentreForTest();

            // A tap is a press + release inside the drag threshold.
            interaction.PressForTest(centre);
            await ctx.Runner.SimulateFrames(SettleFrames, MsPerFrame);
            interaction.ReleaseForTest(centre);
            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame);

            // The gesture itself must not play: only the Button's pressed signal may.
            AssertThat(interaction.IsHeldForTest()).IsFalse();
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
            AssertThat(player.Hand.Contains(card)).IsTrue();
            AssertThat(player.Mana).IsEqual(manaBefore);

            // The real click path (scene-wired "pressed" -> Card.OnButtonPressed).
            card.EmitSignal(BaseButton.SignalName.Pressed);

            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore + 1);
            AssertThat(player.Armor).IsEqual(armorBefore + 5);
            AssertThat(player.Mana).IsEqual(manaBefore - 1);
            AssertThat(player.Hand.Any(c => c.CardName == "T_Defense")).IsFalse();
        }

        /// <summary>
        /// Boundary of the feature: CardUi views (reward panel, deck overlay, compendium) are not
        /// hand cards — full authored size, non-interactive, and no interaction child.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public void NonHandCardViews_StayFullSize()
        {
            Card view = CardUi.CreateCardView(new CardSaveData
            {
                CardName = "View Card",
                CardImage = CardImagePath,
                Description = "Not a hand card",
                Cost = 1,
                EffectString = "Attack",
                Value = 5,
                Amount = 1,
                isTargetingSelf = false,
                isMultipleTargets = false
            });

            try
            {
                AssertThat(view.CustomMinimumSize).IsEqual(AuthoredCardSize);
                AssertThat(view.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(view.FocusMode).IsEqual(Control.FocusModeEnum.None);
                AssertThat(view.GetNodeOrNull<HandCardInteraction>("HandCardInteraction")).IsNull();
            }
            finally
            {
                // Detached view: Free() immediately, QueueFree() would never flush without a tree.
                view.Free();
            }
        }

        // ---- Real input: no test hooks, the events go through Godot's input pipeline ----

        /// <summary>
        /// A REAL left-button press over the visible 40.5x75 rect starts the hold, a real motion
        /// event drags the card with the pointer, and the real release cancels the dead-zone drop
        /// back to rest. Everything below the coordinates is production code
        /// (<c>Node._Input</c> -&gt; try-begin/follow/release); no <c>*ForTest</c> hook is involved.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task RealMousePress_OverCardVisual_CapturesHoldAndFollowsPointer()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();
            Player player = ctx.Player;

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);

            Vector2 pressPoint = interaction.VisualCentreForTest();
            Vector2 target = pressPoint + new Vector2(35f, -25f);
            int handBefore = player.Hand.Count;
            int manaBefore = player.Mana;
            int cardsPlayedBefore = CardsPlayed();

            // A real mouse has to travel to the card before it can press it; this also settles
            // GuiGetHoveredControl onto the card, which is the authoritative branch of the gate.
            DispatchInput(MouseMotionAt(pressPoint));

            DispatchInput(MouseButtonAt(pressPoint, pressed: true));
            AssertThat(interaction.IsHeldForTest())
                .OverrideFailureMessage($"a real left press at {pressPoint} (the card's visual centre) did not start the hold")
                .IsTrue();

            DispatchInput(MouseMotionAt(target));
            AssertThat(interaction.IsDraggingForTest())
                .OverrideFailureMessage(
                    $"a real motion of {(target - pressPoint).Length():0.##} px did not cross the {HandCardInteraction.DragThresholdPx} px drag threshold")
                .IsTrue();

            // The press grabbed the visual centre exactly, so the centre must sit on the pointer.
            AssertVectorApprox(interaction.VisualCentreForTest(), target, PointerTolerance, "visual centre after a real motion event");

            // The release must land in dead space; over an enemy or in the centre zone it would
            // play the card and free the node, which would make the rest-pose assertions meaningless.
            AssertThat(CenterZoneFor(card).HasPoint(target))
                .OverrideFailureMessage($"the motion target {target} is inside the centre drop zone; the release would play the card")
                .IsFalse();
            foreach (Enemy enemy in ctx.GameManager.GetLiveEnemies())
            {
                AssertThat(enemy.GetGlobalRect().HasPoint(target))
                    .OverrideFailureMessage($"the motion target {target} is over enemy '{enemy.Name}'; the release would play the card")
                    .IsFalse();
            }

            DispatchInput(MouseButtonAt(target, pressed: false));
            AssertThat(interaction.IsHeldForTest())
                .OverrideFailureMessage("a real release in the dead zone left the card held")
                .IsFalse();
            AssertThat(interaction.IsDraggingForTest()).IsFalse();

            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Scale.Y).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(interaction.VisualRectForTest().Size.X)
                .IsEqualApprox(HandCardInteraction.HandCardSlotSize.X, GeometryTolerance);

            // A dead-zone drag plays nothing and spends nothing.
            AssertThat(player.Hand.Contains(card)).IsTrue();
            AssertThat(player.Hand.Count).IsEqual(handBefore);
            AssertThat(player.Mana).IsEqual(manaBefore);
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
        }

        /// <summary>
        /// The hit-rect fix, driven by real input: the 16 px gap between two neighbouring hand
        /// cards is inside the old unscaled 81x150 layout rect but outside both visible 40.5x75
        /// rects. A real press there must not grab any card.
        /// <para>
        /// The test first proves delivery: a real press on a card's own visible centre DOES start
        /// that card's hold in this very fixture, so "nothing grabbed" below cannot be the trivial
        /// result of events never arriving.
        /// </para>
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task RealPress_InGapBetweenCards_DoesNotCaptureHold()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            AssertThat(ctx.Player.Hand.Count >= 2)
                .OverrideFailureMessage($"the hand must hold at least 2 cards, it holds {ctx.Player.Hand.Count}")
                .IsTrue();

            // Control card for the delivery proof: an Attack card (a plain click only selects it,
            // it is never played or freed), spawned last so it cannot sit between cards 0 and 1.
            Card control = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction controlInteraction = InteractionOf(control);
            Vector2 controlCentre = controlInteraction.VisualCentreForTest();

            DispatchInput(MouseMotionAt(controlCentre));
            DispatchInput(MouseButtonAt(controlCentre, pressed: true));
            AssertThat(controlInteraction.IsHeldForTest())
                .OverrideFailureMessage(
                    $"real input does not reach the hand card at all: a press at {controlCentre} (its visible centre) did not start the hold")
                .IsTrue();

            DispatchInput(MouseButtonAt(controlCentre, pressed: false));
            AssertThat(controlInteraction.IsHeldForTest()).IsFalse();
            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // let it glide home again

            Card first = ctx.Player.Hand[0];
            Card second = ctx.Player.Hand[1];
            Rect2 firstVisual = InteractionOf(first).VisualRectForTest();
            Rect2 secondVisual = InteractionOf(second).VisualRectForTest();

            // Same grid row, otherwise "the gap between them" is not a horizontal gap at all.
            AssertThat(Mathf.Abs(firstVisual.GetCenter().Y - secondVisual.GetCenter().Y) <= GeometryTolerance)
                .OverrideFailureMessage(
                    $"hand cards 0 and 1 are not in the same row (centres {firstVisual.GetCenter()} / {secondVisual.GetCenter()})")
                .IsTrue();

            float gapWidth = secondVisual.Position.X - firstVisual.End.X;
            AssertThat(gapWidth > 1f)
                .OverrideFailureMessage($"cards 0 and 1 leave no horizontal gap (right edge {firstVisual.End.X}, left edge {secondVisual.Position.X})")
                .IsTrue();

            Vector2 gapPoint = new(
                (firstVisual.End.X + secondVisual.Position.X) * 0.5f,
                firstVisual.GetCenter().Y);

            // The gap point is outside BOTH visible cards ...
            AssertThat(firstVisual.HasPoint(gapPoint))
                .OverrideFailureMessage($"the gap point {gapPoint} is inside card 0's visible rect {firstVisual}")
                .IsFalse();
            AssertThat(secondVisual.HasPoint(gapPoint))
                .OverrideFailureMessage($"the gap point {gapPoint} is inside card 1's visible rect {secondVisual}")
                .IsFalse();

            // ... but inside the legacy hit rect (the unscaled 81x150 layout rect the pre-fix
            // hit test used, i.e. centred on the visual), so the press below is exactly the one
            // that used to grab the wrong card.
            Rect2 legacyRect = new(firstVisual.GetCenter() - (AuthoredCardSize * 0.5f), AuthoredCardSize);
            AssertThat(legacyRect.HasPoint(gapPoint))
                .OverrideFailureMessage($"the gap point {gapPoint} is not inside the legacy hit rect {legacyRect}; the test would prove nothing")
                .IsTrue();

            DispatchInput(MouseMotionAt(gapPoint));
            DispatchInput(MouseButtonAt(gapPoint, pressed: true));

            foreach (Card card in ctx.Player.Hand.ToList())
            {
                AssertThat(InteractionOf(card).IsHeldForTest())
                    .OverrideFailureMessage($"pressing the {gapWidth:0.##} px gap between cards 0 and 1 at {gapPoint} grabbed '{card.CardName}'")
                    .IsFalse();
            }

            // Leave no button latched behind (nothing was held, so this is a no-op for the cards).
            DispatchInput(MouseButtonAt(gapPoint, pressed: false));
        }

        /// <summary>
        /// Native touch owns the gesture: the finger claims the hold (index 0), and an EMULATED
        /// mouse press (<c>Device == -1</c>, what Godot synthesises from a touch) aimed at a second
        /// card must neither start a second hold nor hijack the first one. The touch release ends
        /// it and clears the latch.
        /// </summary>
        [TestCase]
        [RequireGodotRuntime]
        public async Task RealTouchPress_OwnsGesture_AndEmulatedMouseIsIgnored()
        {
            EncounterFixture ctx = await EncounterFixture.LoadAsync();

            Card card = await SpawnCardIntoHand(ctx, AttackCard());
            Card other = await SpawnCardIntoHand(ctx, AttackCard());
            HandCardInteraction interaction = InteractionOf(card);
            HandCardInteraction otherInteraction = InteractionOf(other);

            Vector2 centre = interaction.VisualCentreForTest();
            Vector2 otherPoint = otherInteraction.VisualCentreForTest();
            int cardsPlayedBefore = CardsPlayed();

            // A point on a different card: if the emulated press below were accepted, it would
            // start a hold there (or move this card), rather than being a no-op.
            AssertThat(centre.DistanceTo(otherPoint) > HandCardInteraction.HandCardSlotSize.X)
                .OverrideFailureMessage($"the two touch points are too close ({centre} / {otherPoint}) for the hijack check to mean anything")
                .IsTrue();

            DispatchInput(new InputEventScreenTouch { Index = 0, Pressed = true, Position = centre });
            AssertThat(interaction.IsHeldForTest())
                .OverrideFailureMessage($"a real touch press at {centre} did not start the hold")
                .IsTrue();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(0);

            // Emulated mouse press from a touch: device -1, aimed at the OTHER card.
            DispatchInput(MouseButtonAt(otherPoint, pressed: true, device: (int)InputEvent.DeviceIdEmulation));

            AssertThat(interaction.IsHeldForTest())
                .OverrideFailureMessage("an emulated mouse press cancelled the touch gesture")
                .IsTrue();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(0);
            AssertVectorApprox(interaction.VisualCentreForTest(), centre, PointerTolerance, "centre after the emulated mouse press");
            AssertThat(otherInteraction.IsHeldForTest())
                .OverrideFailureMessage("an emulated mouse press started a second hold on another card")
                .IsFalse();

            DispatchInput(new InputEventScreenTouch { Index = 0, Pressed = false, Position = centre });
            AssertThat(interaction.IsHeldForTest())
                .OverrideFailureMessage("the owning touch release did not end the gesture")
                .IsFalse();
            AssertThat(interaction.ActiveTouchIndexForTest).IsEqual(-1);

            // A tap is a click, not a play: the Attack card only gets selected, it stays in hand.
            await ctx.Runner.SimulateFrames(ShortSettleFrames, MsPerFrame); // 160 ms > 0.10 s release tween

            AssertThat(card.Scale.X).IsEqualApprox(HandCardInteraction.RestScale, 0.01f);
            AssertThat(card.Position.X).IsEqualApprox(HandCardInteraction.RestOffset.X, GeometryTolerance);
            AssertThat(card.Position.Y).IsEqualApprox(HandCardInteraction.RestOffset.Y, GeometryTolerance);
            AssertThat(ctx.Player.Hand.Contains(card)).IsTrue();
            AssertThat(CardsPlayed()).IsEqual(cardsPlayedBefore);
        }

        // ---- Helpers ----

        // Runner note: ISceneRunner runs in Godot's 64x64 window, so the hand is laid out at
        // negative viewport coordinates. The coordinates stay in ONE space (the scene root is a
        // CanvasLayer with no offset, so canvas == viewport), GuiGetHoveredControl() is null for
        // every event, and the product's hit test therefore takes its documented rect fallback
        // (HandCardInteraction.PointerIsOverThisCard). The event itself still reaches _Input and
        // the production gates from there - which is the whole point of these tests.

        /// <summary>
        /// Pushes an event through Godot's real input pipeline (the same entry point gdUnit4's own
        /// SimulateMouse*/SimulateTouch helpers use), so it reaches every node's <c>_Input</c> and
        /// GUI hit-testing instead of entering the gesture at a test seam.
        /// <para>
        /// The flush is required: <c>Input.ParseInputEvent</c> only queues the event
        /// (<c>agile_input_event_flushing</c> is off by default) and the engine flushes it at the
        /// start of the next frame. Without it the assertions below run before the event exists.
        /// </para>
        /// </summary>
        private static void DispatchInput(InputEvent ev)
        {
            Input.ParseInputEvent(ev);
            Input.FlushBufferedEvents();
        }

        /// <summary>Real left-button event in viewport coordinates.</summary>
        private static InputEventMouseButton MouseButtonAt(Vector2 point, bool pressed, int device = 0) => new()
        {
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
            // Both are set deliberately: the viewport re-derives global_position from position
            // while it makes the event local, so a zero position would deliver the event at (0,0).
            Position = point,
            GlobalPosition = point,
            Device = device
        };

        /// <summary>Real pointer motion in viewport coordinates.</summary>
        private static InputEventMouseMotion MouseMotionAt(Vector2 point, int device = 0) => new()
        {
            Position = point,
            GlobalPosition = point,
            Device = device
        };

        /// <summary>
        /// The loaded encounter plus the nodes every test needs. The GameManager is the
        /// "Encounter" child of the CanvasLayer root, which is what <c>Bind</c> finds by walking
        /// up from the player.
        /// </summary>
        private sealed class EncounterFixture
        {
            public ISceneRunner Runner;
            public Node Root;
            public Player Player;
            public GameManager GameManager;

            public static async Task<EncounterFixture> LoadAsync()
            {
                RunManager.NewRun();

                ISceneRunner runner = ISceneRunner.Load("res://Scenes/Encounter.tscn", autoFree: true);
                await runner.SimulateFrames(30);

                Node root = runner.Scene();
                return new EncounterFixture
                {
                    Runner = runner,
                    Root = root,
                    Player = root.GetNode<Player>("%Player"),
                    GameManager = root.GetNode<GameManager>("Encounter")
                };
            }
        }

        private static CardSaveData AttackCard() => new()
        {
            CardName = "T_Attack",
            CardImage = CardImagePath,
            Description = "Deal 5 damage",
            Cost = 1,
            EffectString = "Attack",
            Value = 5,
            Amount = 1,
            isTargetingSelf = false,
            isMultipleTargets = false
        };

        private static CardSaveData DefenseCard() => new()
        {
            CardName = "T_Defense",
            CardImage = CardImagePath,
            Description = "Gain 5 armor",
            Cost = 1,
            EffectString = "Defense",
            Value = 5,
            Amount = 1,
            isTargetingSelf = true,
            isMultipleTargets = false
        };

        /// <summary>Spawns a real hand card (Bind needs the live GameManager) and returns it.</summary>
        private static async Task<Card> SpawnCardIntoHand(EncounterFixture ctx, CardSaveData data)
        {
            ctx.Player.SpawnCard(data);
            Card card = ctx.Player.Hand[ctx.Player.Hand.Count - 1];

            // Let the GridContainer lay the new card out before a gesture is derived from its rect.
            await ctx.Runner.SimulateFrames(2);
            return card;
        }

        private static HandCardInteraction InteractionOf(Card card) =>
            card.GetNode<HandCardInteraction>("HandCardInteraction");

        /// <summary>
        /// First live enemy, made safe to assert against: no armor (damage delta is exactly the
        /// card value) and high health (OnEnemyDie cannot free the node mid-test).
        /// </summary>
        private static Enemy FirstEnemy(EncounterFixture ctx)
        {
            IReadOnlyList<Enemy> enemies = ctx.GameManager.GetLiveEnemies();
            AssertThat(enemies.Count >= 1).IsTrue();

            Enemy enemy = enemies[0];
            AssertThat(enemy).IsNotNull();
            enemy.Armor = 0;
            enemy.Health = 50;
            return enemy;
        }

        /// <summary>Mirrors HandCardInteraction's viewport-derived center drop zone.</summary>
        private static Rect2 CenterZoneFor(Card card)
        {
            Rect2 viewportRect = card.GetViewportRect();
            Vector2 zoneSize = new(
                Mathf.Min(viewportRect.Size.X, 300f),
                Mathf.Min(viewportRect.Size.Y * 0.25f, 180f));
            return new Rect2(viewportRect.Position + ((viewportRect.Size - zoneSize) * 0.5f), zoneSize);
        }

        /// <summary>
        /// Vector2 proximity with the actual/expected values in the failure message (the framework
        /// has no component-wise approx assertion for vectors).
        /// </summary>
        private static void AssertVectorApprox(Vector2 actual, Vector2 expected, float tolerance, string what)
        {
            float distance = actual.DistanceTo(expected);
            AssertThat(distance)
                .IsLessEqual(tolerance)
                .OverrideFailureMessage(
                    $"{what}: expected ~{expected} (tolerance {tolerance}), got {actual} (distance {distance:0.###})");
        }

        private static int CardsPlayed() => RunManager.CurrentRun?.Stats?.CardsPlayed ?? -1;
    }
}
