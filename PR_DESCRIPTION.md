# Pull Request: level-path run, acts, enemy intents, save/load

## Summary

Adds the full level-path run loop behind the new main menu: **Main Menu → Level Path → Encounter → back to Path Map**, repeating until the boss row, with either a win or loss ending the run. Core systems introduced: procedurally generated run paths, act-based enemy scaling, per-turn enemy intents, and robust mid-encounter save/load. Also fixes a C# node lifecycle bug (double-free of live card nodes during scene teardown).

## Run flow

- `MainMenu` → *New Game* builds a fresh run (`RunManager.NewRun`) and opens `Scenes/PathMap.tscn`; *Continue* loads the autosave and routes to `Encounter.tscn` if a battle is in progress, otherwise back to the path map.
- `PathMap` renders all rows; each step you pick one node in the current row (only columns adjacent to your previous choice, ±1). Completed rows are dimmed/gold, future rows locked.
- Choosing an **Encounter** or **Boss** node opens `Scenes/Encounter.tscn`. Winning an encounter calls `RunManager.CompleteCurrentNode()` and returns to the path map; losing calls `EndRun(false)` → main menu. Winning the boss row calls `EndRun(true)`.
- **Rest** nodes open an in-map panel offering *Rest* (heal 25% of max HP) or *Skip*, then advance.

## Paths

`Run/PathGenerator.cs` generates a run's static path up front (stored in the save, never re-rolled mid-run):

- **9 step rows**, each with a random 3–4 nodes; one node at rows 2 and 5 is replaced with a `Rest`.
- A final **boss row** (single `Boss` node) is appended.
- `NodeType`: `Encounter`, `Rest`, `Boss`. `PathNode` is a plain data class (`Row`, `Column`, `Type`).
- `GetSelectableColumns`/`RunManager.IsSelectable` implement the ±1 column adjacency on every row after the first.

## Acts

`Resources/Acts/Act1.tres` is an `ActConfig` resource (`ActName` = "Act I", `IntentMultiplier` = 1.0) exported on `GameManager.actConfig`. The multiplier scales enemy attack/defend intents in `GenerateEnemies()`. Only one act exists today; the resource plumbing is ready for more acts with different multipliers.

## Enemy incremental intents

`EnemyResource` (`.tres` per enemy) defines base stats **plus per-step growth**: `MinHealth/MaxHealth`, `Armor`, `AttackMin/AttackMax`, `DefendMin/DefendMax`, `AttackGrowthPerStep`, `DefendGrowthPerStep`.

`GameManager.GenerateEnemies()` makes encounters scale with depth:

- **Health** = random(min,max) × `(1 + 0.15 · currentRow)`; boss health ×1.5 on top.
- **Attack/defend** = `(base + currentRow · growthPerStep) × act.IntentMultiplier`.
- Enemy count is 1–4 for normal encounters, exactly 1 for the boss.

Each player turn, `GenerateEnemyIntent()` rolls a 50/50 attack or defend intent within the enemy's scaled min/max; `SetIntent` pushes it to the `Enemy` node, which shows the intent in the UI (sword/shield icon + color-coded value). During the enemy turn, `Enemy.PlayTurn` resolves it: attack → `player.TakeDamage(value)`, defend → `AddArmor(value)`. Strikes are staggered with per-enemy timers so they resolve sequentially.

## Save / load

`Run/SaveManager.cs` writes two JSON files (Newtonsoft, indented) under `user://`:

- `savegame.json` — one autosave of the full `RunData` (schema v1): generated path, `CurrentRow`, `ChosenColumns`, `ActiveEncounter`/`IsBossNode` flags, `PlayerState`, deck/hand/discard as `CardSaveData` lists, `Enemies` as `EnemySaveData`, and `RunStats`.
- `runs.json` — append-only run history (`List<RunStats>`) written once by `EndRun`.

**Nothing serializes Godot nodes** — `Player.CollectState/CollectCards` and `Enemy.CollectState` snapshot plain data (`CardSaveData.FromCard`, including an effect-string fallback to the effect class name), and hydration rebuilds nodes from data (`SetDeck`/`SetHand`/`SetDiscard`, `SpawnEnemyFromSave`).

State is persisted mid-encounter (`PersistLiveState()`) after every mutation: fresh encounter setup, end of player turn, card plays, end-of-turn transitions, and enemy deaths — so a quit/continue resumes battle-mid-turn exactly as left. Continuing mid-encounter restores enemies from save and skips the first-turn shuffle/reset (`resumedEncounter` path in `GameManager.SetupRun`).

## Card node lifecycle fix

Previously `Player` held raw `new Card()` native templates in `Deck`/`DiscardPile`, and `_ExitTree` force-freed live children during scene teardown — resulting in the `gchandle.is_released()` FATAL / "caller thread can't call propagate_notification" crash when GC finalized already-freed nodes. Now `Deck`/`DiscardPile` are pure `List<CardSaveData>`, only `Hand` holds live scene-instantiated cards, discard uses `QueueFree()`, and teardown does data resets only.

## Files

- New: `scripts/Run/*` (PathGenerator, PathNode, NodeType, RunData, RunManager, SaveManager, RunStats, CardSaveData, EnemySaveData), `scripts/PathMap.cs`, `Scenes/PathMap.tscn`, `scripts/Resources/ActConfig.cs`, `Resources/Acts/Act1.tres`
- Modified: `scripts/GameManager.cs`, `scripts/Player.cs`, `scripts/MainMenu.cs`, `scripts/Enemy.cs`, `scripts/Resources/EnemyResource.cs`, `Themes/Theme.tres`, `Scenes/Encounter.tscn`, `Scenes/MainMenu.tscn`

## Verification

No automated tests (per project convention). Compile sanity: `dotnet build` passes. Manual checklist: new run → path selection adjacency → encounter win/loss → rest heal → boss win → Continue mid-encounter resume → run history written to `runs.json`.

---

# Commit message

```text
feat(run): add level-path runs, acts, enemy intents, save/load

Run flow: menu -> path map (±1 column adjacency) -> encounter -> repeat
until boss row; win/lose ends run back to menu.

- Path: 9 step rows (3-4 nodes) + boss row; rest nodes at rows 2,5 heal 25%
- Enemy scaling: health x(1 + 0.15*row), boss x1.5, act intent multiplier;
  attack/defend grow per step via EnemyResource growth fields
- Save: autosave user://savegame.json (RunData JSON, mid-encounter exact
  state) + run history user://runs.json; serialize plain data, not nodes
- Continue: resume mid-encounter (enemies restored from save) or path map
- Cards: Deck/Discard as List<CardSaveData> data, Hand as live nodes;
  discard QueueFrees nodes (fixes gchandle double-free FATAL on teardown)
```