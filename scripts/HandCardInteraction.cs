using System.Collections.Generic;
using Godot;

/// <summary>
/// Per-hand-card interaction: hold-to-enlarge, drag-to-target, drag-to-center.
/// Attached only to cards spawned into the player's hand (Player.SpawnCard), never
/// to CardUi views (reward panel, deck overlay, compendium), which stay full size.
/// </summary>
/// <remarks>
/// Input is handled on this plain <see cref="Node"/> through <see cref="_Input"/> (not on the
/// <see cref="Card"/> control via <c>gui_input</c>): once the pointer leaves the card's rect a
/// gui-input-based approach stops receiving motion/release events.
/// <para>
/// Godot dispatches a release as <c>_Input</c> (nodes) -&gt; Control <c>_gui_input</c> -&gt;
/// <c>_UnhandledInput</c>. Because <c>_Input</c> runs before GUI hit-testing, a drag would double
/// play the card unless it is blocked: <see cref="Control.MouseFilterEnum.Ignore"/> is latched the
/// moment the gesture becomes a drag, so the release never reaches <c>BaseButton</c> and
/// <c>Card.OnButtonPressed()</c> never fires. A plain click keeps <c>MouseFilter.Stop</c> and plays
/// through the existing <c>OnCardClick</c> path exactly as before.
/// </para>
/// <para>
/// Same reason, second guard: <c>_Input</c> also bypasses modal overlays, so
/// <see cref="CanBeginHold"/> asks <see cref="GameManager.IsHandBlocked"/> and whether the control
/// actually under the pointer belongs to this card before a hold may start.
/// </para>
/// <para>
/// The held card scales around its CENTRE (<c>PivotOffset = size * 0.5f</c>). Rest is
/// <see cref="RestScale"/> 0.5, i.e. a 40.5x75 visual inside a <see cref="HandCardSlotSize"/> slot;
/// held is <see cref="HeldScale"/> 1.0, the full authored 81x150 card - which stays inside the
/// 360x718 portrait. The layout rect never changes (see <see cref="InitializeCardVisuals"/>).
/// </para>
/// <para>
/// Touch is handled natively (<c>InputEventScreenTouch/Drag</c>) and Godot's emulated mouse events
/// from the same touch are dropped (<see cref="InputEvent.DeviceIdEmulation"/>, plus an
/// <c>activeTouchIndex</c> latch), so one finger never starts two gestures.
/// </para>
/// </remarks>
public partial class HandCardInteraction : Node
{
    /// <summary>
    /// Authored card size (the 81x150 root Button of <c>Scenes/card.tscn</c>). A hand card keeps
    /// this layout rect at all times; only its <see cref="CanvasItem.Scale"/> changes.
    /// </summary>
    public static readonly Vector2 AuthoredCardSize = new(81f, 150f);

    /// <summary>Rest scale: the in-hand card renders at half the authored size (40.5x75).</summary>
    public const float RestScale = 0.5f;

    /// <summary>Held scale: the held card renders at the full authored size (81x150).</summary>
    public const float HeldScale = 1f;

    /// <summary>
    /// Layout slot a hand card occupies in the hand <c>GridContainer</c> at rest (40.5x75).
    /// A Control's <c>Scale</c> does NOT shrink its container slot (containers sort by the
    /// unscaled combined minimum size), so <see cref="Player.SpawnCard"/> wraps every hand card
    /// in a plain <c>Control</c> of exactly this size and the card is scaled/offset inside it.
    /// </summary>
    public static readonly Vector2 HandCardSlotSize = AuthoredCardSize * RestScale;

    /// <summary>
    /// Card-local position centring the half-size visual inside its <see cref="HandCardSlotSize"/>
    /// slot: (-20.25, -37.5). Applied on top of the unchanged authored layout rect.
    /// </summary>
    public static readonly Vector2 RestOffset = (HandCardSlotSize - AuthoredCardSize) * 0.5f;

    public const float PressTweenSeconds = 0.12f;
    public const float ReleaseTweenSeconds = 0.10f;
    public const float DragThresholdPx = 9f;
    public const int HeldZIndex = 10;

    // No hand-only layout squeeze any more: the old 105 px budget only existed to cram the
    // authored 151 px of content into a 108 px card. The card is now always 81x150 at layout
    // time and scaled down for rendering, so the authored internals are correct again.

    private Card card;
    private Player player;
    private GameManager gameManager;
    private Tween scaleTween;
    private Vector2 pressGlobalPosition;
    private Vector2 pointerGlobalPosition;
    private int originalZIndex;
    private Control.MouseFilterEnum originalMouseFilter = Control.MouseFilterEnum.Stop;
    private Enemy highlightedEnemy;
    private bool isHeld;
    private bool isDragging;

    // Pointer offset from the visual centre at grab time, clamped to half the rest slot.
    private Vector2 grabOffset;

    // Index of the finger that owns the current gesture, -1 when no touch gesture is running.
    private int activeTouchIndex = -1;

    /// <summary>
    /// Wires the behaviour to its card. Resolves the <see cref="GameManager"/> by walking
    /// up the tree from the player instead of using unique names/absolute paths.
    /// <para>
    /// Test seam warning: a card instantiated outside an <c>Encounter.tscn</c> (i.e. with no
    /// <see cref="GameManager"/> ancestor) gets a null <see cref="gameManager"/>, and therefore
    /// <see cref="CanBeginHold"/> always returns false - the hold never starts. There is no
    /// bare-card seam: interaction tests must load <c>res://Scenes/Encounter.tscn</c> through an
    /// <c>ISceneRunner</c>, or they would pass vacuously.
    /// </para>
    /// </summary>
    public void Bind(Card owner, Player ownerPlayer)
    {
        card = owner;
        player = ownerPlayer;
        gameManager = null;

        for (Node n = ownerPlayer?.GetParent(); n != null; n = n.GetParent())
        {
            if (n is GameManager gm)
            {
                gameManager = gm;
                break;
            }
        }

        // Bind is called right after AddChild (see Player.SpawnCard), so _Ready ran with a null
        // card - initialise the visuals here as well.
        InitializeCardVisuals();
    }

    public override void _Ready()
    {
        // Explicit: a plain Node only receives _Input/_UnhandledInput while input processing is on.
        SetProcessInput(true);
        SetProcessUnhandledInput(true);
        // Per-frame work only exists while a card is held (see ApplyHeldPose).
        SetProcess(false);

        InitializeCardVisuals();
    }

    public override void _ExitTree()
    {
        if (scaleTween != null && scaleTween.IsValid()) scaleTween.Kill();
        scaleTween = null;

        // Writing to the enemy during teardown is intentional: an enemy outlives the card, and a
        // red-tinted one after the fight would be a visible bug. It is safe because SetHighlight
        // bails on freed instances (IsInstanceValid).
        ClearHighlight();
    }

    /// <summary>
    /// Sets the rest geometry: the authored 81x150 layout rect, rendered at
    /// <see cref="RestScale"/> and offset by <see cref="RestOffset"/> so the half-size visual sits
    /// centred in its <see cref="HandCardSlotSize"/> slot. Safe to call before the first layout
    /// pass, and harmless on a card that has no slot parent yet (nothing to offset then).
    /// </summary>
    private void InitializeCardVisuals()
    {
        if (card == null || !IsInstanceValid(card)) return;

        // The layout rect always stays the authored size: the authored internals (VBox offset_top
        // 23, 50x50 %CardImage, name, description) need those 150 px, and a container sorts by the
        // UNSCALED combined minimum size - shrinking the rect would make the hand overflow or grow.
        card.CustomMinimumSize = AuthoredCardSize;
        card.CustomMaximumSize = AuthoredCardSize;
        card.Size = AuthoredCardSize;

        // Centre pivot: rest 40.5x75 and held 81x150 both stay centred on the slot.
        card.PivotOffset = AuthoredCardSize * 0.5f;
        card.Scale = Vector2.One * RestScale;

        // Only meaningful once the card lives in its CardSlot (see Player.SpawnCard); a card parented
        // elsewhere/nowhere keeps the position it already has.
        if (card.GetParent() is Control) card.Position = RestOffset;
    }

    /// <summary>
    /// Per-frame safety net for the hold pose: the scale tween changes <c>Scale</c> between motion
    /// events, which would otherwise shift the visual centre under a stationary pointer.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!isHeld) return;
        ApplyHeldPose();
    }


    public override void _Input(InputEvent @event)
    {
        // Touch is handled NATIVELY and Godot's emulated mouse events are dropped below. Godot
        // emulates mouse input from touch by default
        // (input_devices/pointing/emulate_mouse_from_touch defaults to true in main.cpp and this
        // project does not override it), so a touch otherwise arrives twice. The emulation only
        // covers the first finger and marks its events with InputEvent.DeviceIdEmulation = -1
        // (verified in Godot's input.cpp / input_event.h); the emulated events are only ignored BY
        // THIS NODE, so BaseButton still receives them and a plain tap keeps firing OnCardClick.
        if (@event is InputEventScreenTouch touch)
        {
            OnScreenTouch(touch);
            return;
        }

        if (@event is InputEventScreenDrag drag)
        {
            OnScreenDrag(drag);
            return;
        }

        bool isMouseEvent = @event is InputEventMouseButton || @event is InputEventMouseMotion;
        if (isMouseEvent)
        {
            // Dedup rule: device -1 is Godot's emulation id, and activeTouchIndex >= 0 catches the
            // emulated stream too (a second, independent guard), so a touch gesture is never fed
            // twice into the same state machine.
            if (@event.Device == InputEvent.DeviceIdEmulation) return;
            if (activeTouchIndex >= 0) return;
        }

        if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
        {
            if (button.Pressed)
            {
                // Re-arm the click target on a fresh press. Runs before GUI dispatch of this same
                // press, so the reset never leaves the card disabled for the gesture it serves.
                if (!isHeld) RestoreMouseFilterOnFreshPress();
                TryBeginHold(button.GlobalPosition);
                return;
            }

            if (!isHeld) return;
            pointerGlobalPosition = button.GlobalPosition;
            ReleaseGesture();
            return;
        }

        if (@event is InputEventMouseMotion motion && isHeld)
        {
            MovePointer(motion.GlobalPosition);
        }
    }

    /// <summary>
    /// Native touch press/release. One finger owns a gesture: the first touch that actually starts a
    /// hold claims <see cref="activeTouchIndex"/>; every other index (second finger) is ignored
    /// entirely.
    /// </summary>
    private void OnScreenTouch(InputEventScreenTouch touch)
    {
        if (touch.Pressed)
        {
            if (activeTouchIndex != -1) return;

            Vector2 position = ToGestureSpace(touch.Position);
            if (!isHeld) RestoreMouseFilterOnFreshPress();

            // Same begin path (and gates: turn, modal, visual rect, hovered control) as a press.
            TryBeginHold(position);
            if (isHeld) activeTouchIndex = touch.Index;
            return;
        }

        if (touch.Index != activeTouchIndex) return;

        pointerGlobalPosition = ToGestureSpace(touch.Position);
        ReleaseGesture();
        // Also cleared by RestoreRestVisual; repeated here because the played-card path deliberately
        // never writes to the card and must not leave the latch set.
        activeTouchIndex = -1;
    }

    /// <summary>Native touch drag: ignored unless the owning finger is the active one.</summary>
    private void OnScreenDrag(InputEventScreenDrag drag)
    {
        if (drag.Index != activeTouchIndex) return;
        MovePointer(ToGestureSpace(drag.Position));
    }

    /// <summary>
    /// Touch and mouse events are already in the same space here: this project sets no
    /// <c>display/window/stretch/mode</c> (Godot's default is "disabled"), and Godot transforms
    /// events into viewport coordinates before dispatch - its own touch-&gt;mouse emulation even
    /// copies <c>InputEventScreenTouch.Position</c> verbatim into the mouse <c>global_position</c>.
    /// A single named seam, so a future stretch mode only has to change this one place.
    /// </summary>
    private Vector2 ToGestureSpace(Vector2 viewportPosition) => viewportPosition;

    /// <summary>
    /// Mouse/touch press begin path: state gate, then the visual-rect pre-filter and the
    /// authoritative hit test (a covering modal is the hovered control, so it blocks the hold).
    /// </summary>
    private void TryBeginHold(Vector2 globalPosition)
    {
        if (!CanBeginHold()) return;
        if (!VisualRect().HasPoint(globalPosition)) return;
        if (!PointerIsOverThisCard(globalPosition)) return;
        BeginHold(globalPosition);
    }

    /// <summary>
    /// Shared move path for mouse motion and touch drag: follow the pointer first (a hold with no
    /// drag must follow too), then latch the drag once the threshold is crossed, then refresh the
    /// enemy highlight.
    /// </summary>
    private void MovePointer(Vector2 globalPosition)
    {
        if (!isHeld) return;

        pointerGlobalPosition = globalPosition;
        FollowPointer(globalPosition);

        if (!isDragging && (globalPosition - pressGlobalPosition).Length() >= DragThresholdPx)
        {
            isDragging = true;
            // Latch Ignore the instant this becomes a drag: BaseButton still holds its press
            // state, so if the release lands back on the card it would otherwise emit
            // pressed -> OnCardClick and play a card the player dragged and cancelled.
            if (card != null && IsInstanceValid(card)) card.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        if (isDragging) UpdateHighlight();
    }

    /// <summary>Shared release dispatch: click path (no drag) or drag path.</summary>
    private void ReleaseGesture()
    {
        if (!isHeld) return;
        if (!isDragging) OnHoldEnd();
        else OnDragRelease();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!isHeld) return;

        // Key first (echo filtered), then the action map. The global "OnEscapeKey" event is
        // registered by GameManager but never raised anywhere in the project, so the local cancel
        // cannot conflict with it.
        bool cancel = (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
            || @event.IsActionPressed("ui_cancel");
        if (!cancel) return;

        // keepInputBlocked: the mouse button is still physically down, so restoring Stop now would
        // let the eventual release reach BaseButton with its press state intact and emit pressed
        // -> OnCardClick, playing the card the player just cancelled.
        CancelHold(keepInputBlocked: true);
        GetViewport()?.SetInputAsHandled();
    }

    /// <summary>
    /// Focus loss (alt-tab, OS popup) means the release will never arrive and would otherwise wedge
    /// the gesture forever: held scale, ZIndex 10, possibly a red enemy, and Ignore latched so the
    /// card could never be clicked again.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusOut && what != NotificationWMWindowFocusOut) return;
        if (!isHeld) return;

        CancelHold(keepInputBlocked: true); // restored on the next fresh press
    }

    /// <summary>
    /// Re-enables clicking before a new gesture starts. A cancelled gesture may have left
    /// <see cref="Control.MouseFilterEnum.Ignore"/> latched (see <see cref="CancelHold"/>), so the
    /// stored filter is also re-armed defensively.
    /// </summary>
    private void RestoreMouseFilterOnFreshPress()
    {
        if (card == null || !IsInstanceValid(card)) return;

        if (originalMouseFilter == Control.MouseFilterEnum.Ignore)
            originalMouseFilter = Control.MouseFilterEnum.Stop;

        // BaseButton keeps `press_attempt` / `pressing_inside` internally; SetPressedNoSignal and a
        // restored MouseFilter do not clear them. After a cancelled gesture (drag latch, Escape,
        // focus loss) the Button received the press but never the release, so press_attempt can stay
        // true - and BaseButton's press branch is guarded by `if (!press_attempt)`, which would stop
        // the next press from re-arming and let a later release emit `pressed` without a press pair.
        // Toggling Disabled resets both internal flags; doing it here (before the same press is
        // dispatched to _gui_input) leaves the Button enabled and clean for this gesture.
        card.Disabled = true;
        card.Disabled = false;

        if (card.MouseFilter != originalMouseFilter) card.MouseFilter = originalMouseFilter;
    }

    /// <summary>
    /// Shared precondition for real input and <see cref="PressForTest"/>: correct turn, no modal
    /// over the hand, and this card still in a valid hand.
    /// </summary>
    private bool CanBeginHold()
    {
        if (isHeld) return false;
        if (card == null || !IsInstanceValid(card)) return false;
        if (player == null || !IsInstanceValid(player)) return false;
        if (gameManager == null || !IsInstanceValid(gameManager)) return false;
        // Modal overlays and the enemy turn used to be enforced implicitly by the click path
        // (the overlay swallowed the click). _Input runs before GUI hit-testing, so ask up front.
        // The enemy-turn check is intentional and also disables read-only enlarging while the
        // enemy acts: enlarge-then-read must not become a free "scout the hand" action.
        if (!gameManager.IsPlayerTurn) return false;
        if (gameManager.IsHandBlocked) return false;
        return card.IsVisibleInTree();
    }

    /// <summary>
    /// Visual centre of the card in canvas space. A Control's transform is
    /// <c>T(pivot) * scale * T(-pivot)</c> with <c>position</c> added to the origin, so
    /// <see cref="Control.GlobalPosition"/> (that origin) equals
    /// <c>position + pivot * (1 - scale)</c>. Adding <c>pivot * scale</c> back yields the point the
    /// pivot maps to, i.e. the on-screen centre of the visual - scale-invariant while
    /// <c>pivot == size * 0.5</c>, which is why the 0.5 -> 1.0 hold tween cannot move the centre.
    /// </summary>
    private Vector2 VisualCentre() => card.GlobalPosition + (card.PivotOffset * card.Scale);

    /// <summary>
    /// The rect the player actually sees. <see cref="Control.GetGlobalRect"/> cannot be used: it
    /// reports <c>size * scale</c> at the transform origin, so at rest it would describe the
    /// UNSCALED 81x150 rect (reaching ~20 px into both neighbouring slots) instead of the visible
    /// 40.5x75 one.
    /// </summary>
    private Rect2 VisualRect()
    {
        Vector2 size = AuthoredCardSize * card.Scale;
        return new Rect2(VisualCentre() - (size * 0.5f), size);
    }

    /// <summary>
    /// True when the control under the pointer is this card or one of its children
    /// (<c>%CardImage</c>, labels, ...). Walks the ancestor chain because the hovered control is
    /// usually a child, never the Button itself.
    /// <para>
    /// <c>GuiGetHoveredControl()</c> is only updated by a motion event, so a press arriving without
    /// one (first touch/emulated mouse event) returns null. Falling back to the rect test then keeps
    /// the enlarge, at the cost of trusting the rect when no hover state exists; whenever a control
    /// is hovered, the ancestor walk stays authoritative and a covering modal still blocks the hold.
    /// </para>
    /// </summary>
    private bool PointerIsOverThisCard(Vector2 globalPosition)
    {
        if (card == null || !IsInstanceValid(card)) return false;
        if (!VisualRect().HasPoint(globalPosition)) return false;

        Control hovered = card.GetViewport()?.GuiGetHoveredControl();
        if (hovered == null) return true;

        for (Control c = hovered; c != null; c = c.GetParent() as Control)
        {
            if (c == card) return true;
        }
        return false;
    }

    /// <summary>
    /// Where the pointer sits inside the card when the hold starts, captured ONCE. Each component is
    /// clamped to half the rest slot (20.25 x 37.5), so a press near a corner cannot fling the card
    /// away and the pointer always stays inside the card while it is dragged.
    /// </summary>
    private void CaptureGrabOffset(Vector2 globalPointer)
    {
        Vector2 offset = globalPointer - VisualCentre();
        Vector2 limit = HandCardSlotSize * 0.5f;
        grabOffset = new Vector2(
            Mathf.Clamp(offset.X, -limit.X, limit.X),
            Mathf.Clamp(offset.Y, -limit.Y, limit.Y));
    }

    /// <summary>
    /// Puts the card's visual centre at <c>pointer - grabOffset</c>. Legal because the card is
    /// parented to its <c>CardSlot</c>, a plain Control and NOT a Container: the GridContainer
    /// lays out the slot and never writes the card's own position, so the card may move freely
    /// inside it. <see cref="Control.GlobalPosition"/> is the transform origin, hence the
    /// <c>pivot * scale</c> correction back to the visual centre.
    /// </summary>
    private void FollowPointer(Vector2 globalPointer)
    {
        if (card == null || !IsInstanceValid(card)) return;

        Vector2 targetCentre = globalPointer - grabOffset;
        card.GlobalPosition = targetCentre - (card.PivotOffset * card.Scale);
    }

    /// <summary>Starts the hold/drag gesture at a global pointer position (no real input needed).</summary>
    private void BeginHold(Vector2 globalPosition)
    {
        if (!CanBeginHold()) return;

        pointerGlobalPosition = globalPosition;
        pressGlobalPosition = globalPosition;
        originalZIndex = card.ZIndex;
        originalMouseFilter = card.MouseFilter;
        isHeld = true;
        isDragging = false;
        SetProcess(true);

        // No sibling reorder: the hand sits in a GridContainer, and MoveChild would re-sort the
        // grid so the pressed card visibly jumped to the last slot. ZIndex handles draw order.
        card.ZIndex = HeldZIndex;
        card.PivotOffset = AuthoredCardSize * 0.5f;

        // Grab anchor first, then follow once immediately: the card must be under the finger the
        // moment it is grabbed, not after the first motion event.
        CaptureGrabOffset(globalPosition);
        FollowPointer(globalPosition);

        TweenHoldPose();
    }

    /// <summary>
    /// Release without a drag: the gesture stays a click, so <c>MouseFilter</c> is never touched
    /// here - the Button's <c>pressed</c> signal still runs the regular <c>OnCardClick</c> flow.
    /// </summary>
    private void OnHoldEnd()
    {
        ClearHighlight();
        RestoreRestVisual();
    }

    private void OnDragRelease()
    {
        if (card == null || !IsInstanceValid(card))
        {
            // Full gesture reset (not a partial one): ResetGestureState never touches the card, and
            // it also stops _Process and clears activeTouchIndex, so no stale gesture survives.
            ClearHighlight();
            ResetGestureState();
            return;
        }

        if (card.Effect == null)
        {
            CancelHold();
            return;
        }

        Vector2 dropPosition = pointerGlobalPosition;

        // Center (non-targeted) drop zone: derived from the viewport, never hard-coded absolute.
        // TODO(ux): this zone has no on-screen affordance yet (no droppable hint is drawn), so the
        // rect below is the contract the tests gate asserts: viewport-centred,
        // min(viewport width, 300) x min(viewport height * 0.25, 180).
        Rect2 viewportRect = card.GetViewportRect();
        Vector2 zoneSize = new(
            Mathf.Min(viewportRect.Size.X, 300f),
            Mathf.Min(viewportRect.Size.Y * 0.25f, 180f));
        Rect2 centerZone = new(viewportRect.Position + ((viewportRect.Size - zoneSize) * 0.5f), zoneSize);

        PlayFromDrag(dropPosition, centerZone);
    }

    /// <summary>
    /// The whole drag-success sequence. Single structural exit: the card is either freed by
    /// <see cref="GameManager.PlayCardFromDrag"/> (Player.PlayCard -&gt; DiscardCard -&gt; QueueFree)
    /// or <see cref="CancelHold"/> runs and restores the mouse filter deferred. No other path can
    /// leave the card permanently unclickable.
    /// </summary>
    private void PlayFromDrag(Vector2 dropPosition, Rect2 centerZone)
    {
        if (!TryBuildDragTargets(dropPosition, centerZone, out Godot.Collections.Array<Character> targets))
        {
            CancelHold();
            return;
        }

        // Release dispatch is _Input -> _gui_input -> _UnhandledInput and MouseFilter is already
        // Ignore (latched when the drag started), so the Button cannot emit pressed for this
        // gesture and a drag can never double-play through OnCardClick.
        if (!gameManager.PlayCardFromDrag(card, targets))
        {
            CancelHold();
            return;
        }

        // Played: the node is owned by Player.PlayCard/DiscardCard from here on (it was removed
        // from the tree synchronously, so _ExitTree already restored the enemy highlight). Do not
        // touch card, only local state.
        highlightedEnemy = null;
        grabOffset = Vector2.Zero;
        activeTouchIndex = -1;
        SetProcess(false);
        isHeld = false;
        isDragging = false;
    }

    /// <summary>
    /// Resolves the drop zone for the current pointer position and maps it to play targets.
    /// Returns false when the drop is not accepted (no zone, no valid targets, bad enemy index).
    /// </summary>
    private bool TryBuildDragTargets(
        Vector2 dropPosition,
        Rect2 centerZone,
        out Godot.Collections.Array<Character> targets)
    {
        targets = new Godot.Collections.Array<Character>();

        CardEffect effect = card.Effect;

        // Keep the validated enemies in a parallel list so the resolver's index maps back.
        List<Enemy> liveEnemies = new();
        List<Rect2> enemyRects = new();
        CollectEnemyRects(liveEnemies, enemyRects);

        CardDropResult result = CardDropResolver.Resolve(
            card.EffectString,
            effect.Type,
            effect.isTargetingSelf,
            effect.isMultipleTargets,
            dropPosition,
            centerZone,
            enemyRects);

        switch (result.Zone)
        {
            case CardDropZone.Enemy:
                if (result.EnemyIndex < 0 || result.EnemyIndex >= liveEnemies.Count) return false;
                targets.Add(liveEnemies[result.EnemyIndex]);
                return true;

            case CardDropZone.Center:
                if (effect.isTargetingSelf)
                {
                    if (player == null || !IsInstanceValid(player)) return false;
                    targets.Add(player);
                    return true;
                }
                foreach (Enemy enemy in liveEnemies) targets.Add(enemy);
                return targets.Count > 0;

            default:
                return false;
        }
    }

    /// <summary>
    /// Aborts the gesture: no mana spent, no discard, nothing persisted - just undo the visuals
    /// and hand the card back as a plain click target.
    /// </summary>
    /// <param name="keepInputBlocked">
    /// True for a cancel that happened while the mouse button is still down (Escape, focus loss).
    /// The filter then stays <see cref="Control.MouseFilterEnum.Ignore"/> and the Button's stale
    /// press state is left for <see cref="RestoreMouseFilterOnFreshPress"/> (the single reset point),
    /// because Godot dispatches this very release as
    /// <c>_Input</c> -&gt; <c>_gui_input</c> -&gt; <c>_UnhandledInput</c>: restoring Stop synchronously
    /// (or even deferred within the same frame) would let <c>BaseButton</c> see the release with its
    /// press state intact and emit <c>pressed</c> -&gt; <c>OnCardClick</c>, playing the cancelled
    /// card. Input is re-armed on the next fresh press instead.
    /// </param>
    private void CancelHold(bool keepInputBlocked = false)
    {
        if (card == null || !IsInstanceValid(card))
        {
            // Same full reset as the graceful path: no card writes, but _Process and the touch latch
            // are cleared too (RestoreRestVisual would only reach them through ResetGestureState).
            ClearHighlight();
            ResetGestureState();
            return;
        }

        if (keepInputBlocked)
        {
            // Stay latched so the in-flight release cannot reach the Button; Button state
            // (press_attempt / pressing_inside) is reset by RestoreMouseFilterOnFreshPress, the
            // single place that clears it, on the next fresh press.
            card.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        else
        {
            // Drop accepted-but-not-playable (wrong zone, no mana, ...): restore the same frame but
            // deferred, so the in-flight release never reaches the Button's _gui_input.
            Card target = card;
            Control.MouseFilterEnum restoreTo = originalMouseFilter;
            Callable.From(() =>
            {
                if (target != null && IsInstanceValid(target)) target.MouseFilter = restoreTo;
            }).CallDeferred();
        }

        ClearHighlight();
        RestoreRestVisual();
    }

    /// <summary>
    /// Snapshot of the live enemies with their global rects, dropping null/freed/queued nodes.
    /// Parallel lists: <paramref name="liveEnemies"/>[i] owns <paramref name="rects"/>[i].
    /// </summary>
    private void CollectEnemyRects(List<Enemy> liveEnemies, List<Rect2> rects)
    {
        liveEnemies.Clear();
        rects.Clear();

        if (gameManager == null || !IsInstanceValid(gameManager)) return;

        IReadOnlyList<Enemy> enemies = gameManager.GetLiveEnemies();
        if (enemies == null) return;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !IsInstanceValid(enemy) || enemy.IsQueuedForDeletion()) continue;

            liveEnemies.Add(enemy);
            rects.Add(enemy.GetGlobalRect());
        }
    }

    /// <summary>Highlights the enemy currently under the pointer while dragging.</summary>
    private void UpdateHighlight()
    {
        if (card == null || !IsInstanceValid(card))
        {
            ClearHighlight();
            return;
        }

        List<Enemy> liveEnemies = new();
        List<Rect2> rects = new();
        CollectEnemyRects(liveEnemies, rects);

        Enemy hovered = null;
        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i].HasPoint(pointerGlobalPosition))
            {
                hovered = liveEnemies[i];
                break;
            }
        }

        if (hovered == highlightedEnemy) return;

        ClearHighlight();
        highlightedEnemy = hovered;
        if (highlightedEnemy != null) SetHighlight(highlightedEnemy, true);
    }

    private void ClearHighlight()
    {
        if (highlightedEnemy != null) SetHighlight(highlightedEnemy, false);
        highlightedEnemy = null;
    }

    /// <summary>Modulate-only tint; never theme overrides, so the enemy's theme stays authoritative.</summary>
    private void SetHighlight(Enemy enemy, bool on)
    {
        if (enemy == null || !IsInstanceValid(enemy)) return;
        enemy.Modulate = on ? new Color(1f, 0.5f, 0.5f) : new Color(1f, 1f, 1f);
    }

    /// <summary>
    /// Locks the held card's visual centre to the pointer. <see cref="FollowPointer"/> places it
    /// instantly on every motion event; this runs each frame so the scale tween (which changes
    /// <c>Scale</c> without any pointer motion) cannot drift the centre off the finger.
    /// </summary>
    private void ApplyHeldPose()
    {
        if (card == null || !IsInstanceValid(card)) return;

        card.GlobalPosition = (pointerGlobalPosition - grabOffset) - (card.PivotOffset * card.Scale);
    }

    /// <summary>
    /// Grows the card from rest to held scale. Position is intentionally NOT tweened: the per-frame
    /// <see cref="ApplyHeldPose"/> owns it while held, so the pointer stays locked to the same spot.
    /// </summary>
    private void TweenHoldPose()
    {
        if (scaleTween != null && scaleTween.IsValid()) scaleTween.Kill();
        if (card == null || !IsInstanceValid(card)) return;

        scaleTween = CreateTween();
        scaleTween
            .TweenProperty(card, "scale", Vector2.One * HeldScale, PressTweenSeconds)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    /// <summary>
    /// Glides the card back to its rest pose: scale and position on the SAME tween, so the half-size
    /// visual travels home instead of snapping into its slot.
    /// </summary>
    private void TweenToRest()
    {
        if (scaleTween != null && scaleTween.IsValid()) scaleTween.Kill();
        if (card == null || !IsInstanceValid(card)) return;

        scaleTween = CreateTween().SetParallel(true);
        scaleTween
            .TweenProperty(card, "scale", Vector2.One * RestScale, ReleaseTweenSeconds)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        scaleTween
            .TweenProperty(card, "position", RestOffset, ReleaseTweenSeconds)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    /// <summary>
    /// The single "return to rest" path, used by every graceful exit (click release, drag release,
    /// cancel, focus loss). Restores ZIndex/pivot and either glides (<paramref name="animate"/>) or
    /// snaps home, then clears the gesture state.
    /// </summary>
    private void RestoreRestVisual(bool animate = true)
    {
        if (card == null || !IsInstanceValid(card))
        {
            ResetGestureState();
            return;
        }

        card.ZIndex = originalZIndex;
        card.PivotOffset = AuthoredCardSize * 0.5f;

        if (animate)
        {
            TweenToRest();
        }
        else
        {
            if (scaleTween != null && scaleTween.IsValid()) scaleTween.Kill();
            scaleTween = null;
            card.Scale = Vector2.One * RestScale;
            card.Position = RestOffset;
        }

        ResetGestureState();
    }

    /// <summary>
    /// Clears gesture-local state only - never touches the card, so it is safe on a node already
    /// removed from the tree (the played-card path).
    /// </summary>
    private void ResetGestureState()
    {
        isHeld = false;
        isDragging = false;
        highlightedEnemy = null;
        grabOffset = Vector2.Zero;
        activeTouchIndex = -1;
        SetProcess(false);
    }

    // ---- Test hooks: thin public wrappers over the same private paths used by real input. ----

    /// <summary>
    /// Runs the start-hold path at <paramref name="globalPosition"/>. Bypasses the hovered-control
    /// check (tests have no real mouse, so <c>GuiGetHoveredControl</c> would always miss) but still
    /// honours the turn/modal gate through <see cref="CanBeginHold"/>. Also runs the fresh-press
    /// reset like real input, so tests exercise the same state preparation (including the
    /// BaseButton/`MouseFilter` re-arm) instead of entering at a later step.
    /// </summary>
    public void PressForTest(Vector2 globalPosition)
    {
        if (!isHeld) RestoreMouseFilterOnFreshPress();
        BeginHold(globalPosition);
    }

    /// <summary>Moves the pointer through the same follow/threshold/highlight path as real motion.</summary>
    public void DragToForTest(Vector2 globalPosition)
    {
        if (!isHeld) return;

        MovePointer(globalPosition);
    }

    /// <summary>Releases at <paramref name="globalPosition"/> through the drag or click path.</summary>
    public void ReleaseForTest(Vector2 globalPosition)
    {
        if (!isHeld) return;

        pointerGlobalPosition = globalPosition;
        ReleaseGesture();
    }

    /// <summary>Runs the escape-cancel path (including its input-blocked semantics).</summary>
    public void EscapeForTest() => CancelHold(keepInputBlocked: true);

    public bool IsHeldForTest() => isHeld;

    public bool IsDraggingForTest() => isDragging;

    public Vector2 VisualCentreForTest() => VisualCentre();

    public Rect2 VisualRectForTest() => VisualRect();

    /// <summary>Drives the follow + 9 px drag-threshold path exactly like a pointer motion.</summary>
    public void PointerFollowForTest(Vector2 globalPosition) => MovePointer(globalPosition);

    /// <summary>Touch begin path: same gates as real touch (turn, modal, rect, hover), then claims the index.</summary>
    public void TouchPressForTest(int index, Vector2 globalPosition)
    {
        if (activeTouchIndex != -1) return;

        if (!isHeld) RestoreMouseFilterOnFreshPress();
        TryBeginHold(globalPosition);
        if (isHeld) activeTouchIndex = index;
    }

    /// <summary>Touch drag path: ignored unless <paramref name="index"/> owns the gesture.</summary>
    public void TouchDragForTest(int index, Vector2 globalPosition)
    {
        if (index != activeTouchIndex) return;
        MovePointer(globalPosition);
    }

    /// <summary>Touch release path: ignored unless <paramref name="index"/> owns the gesture.</summary>
    public void TouchReleaseForTest(int index, Vector2 globalPosition)
    {
        if (index != activeTouchIndex) return;

        pointerGlobalPosition = globalPosition;
        ReleaseGesture();
        activeTouchIndex = -1;
    }

    public int ActiveTouchIndexForTest => activeTouchIndex;
}
