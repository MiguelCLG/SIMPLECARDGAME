# SpaceOdyssey

A deck-builder card battler with a level-path run. Built with Godot 4.7 (C#/.NET 9), mobile renderer, portrait 360x718. Launch scene: `Scenes/MainMenu.tscn`.

Run flow: Main Menu → Path Map (pick a node per step, ±1 column adjacency) → Encounter → back to the map until the boss row, where winning or losing ends the run.

## Running Tests

Tests use [gdUnit4Net](https://github.com/MikeSchulze/gdUnit4Net) and run through the VSTest adapter with `dotnet test`.

### Prerequisites

- .NET SDK 9
- Godot **4.7.1** (.NET edition) binary

> The gdUnit4 adapter executes tests inside a Godot host, so `GODOT_BIN` must point to a Godot .NET executable even for the pure-logic suites.

### Commands

```bash
dotnet build                                   # compile sanity check

export GODOT_BIN=/path/to/Godot_v4.7.1          # e.g. .../Godot_v4.7.1-stable_mono_linux.x86_64

dotnet test --settings .runsettings            # run the full suite
```

`TestResults/test-results.trx` (TRX) is produced in the repo root after each run.

### Filtering

Runs use VSTest filter syntax:

```bash
# Only path/run-rule tests
dotnet test --settings .runsettings --filter "FullyQualifiedName~PathGenerator"

# Only Godot-runtime integration tests
dotnet test --settings .runsettings --filter "FullyQualifiedName~SpaceOdyssey.Tests.Integration"

# Only logic tests (no engine features, still spawned through the Godot host)
dotnet test --settings .runsettings --filter "FullyQualifiedName~SpaceOdyssey.Tests.Logic"
```

### What's covered

- `tests/logic/` — fast suites: path generation rules, path-node adjacency (`RunManager.IsSelectable`), enemy scaling math (`EnemyScaler`), and `RunData` save/load JSON round-trip.
- `tests/integration/` — engine-backed suites marked `[RequireGodotRuntime]`: card save data capture/restore, save manager round-trips and run-history, and an `Encounter` scene smoke test.

### CI

A GitHub Actions workflow (`.github/workflows/tests.yml`) runs the suite on every pull request and push to `main`: sets up the .NET 9 SDK and Godot 4.7.1, runs `dotnet test`, and uploads the TRX report. The job fails on any failing test.

### Troubleshooting

- **`GODOT_BIN is not set or empty`** → tests aren't discovered/run. Set `GODOT_BIN` to the Godot .NET binary path.
- **`You must install or update .NET to run this application`** → the project targets net9.0 but no .NET 9 runtime is installed locally (e.g. only a newer SDK is present). Install the .NET 9 runtime, or run with roll-forward: `DOTNET_ROLL_FORWARD=LatestMajor dotnet test --settings .runsettings`.