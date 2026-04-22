# Still Waters — Redesigned Script Architecture

This folder contains a full architectural overhaul of the original Still Waters scripts.
The original scripts in `Assets/Scripts/` are **untouched** — open the project normally
and nothing breaks. Migrate scene references at your own pace.

---

## Why the original needed redesigning

The original codebase had two root problems:

1. **Nine singletons calling into each other** — `FishingMinigame` called
   `PlayerManager.instance.StopAllCoroutines()`, `FishingManager` read
   `PlayerManager.instance.transform.position.x`, `DialogManager` called
   `GameManager.instance.PlayNextEvent()`. Every system knew about every other
   system. Changing one broke three others.

2. **Data hardcoded in C#** — fish arrays, upgrade costs, upgrade tiers, dialog
   strings, and scene build indices (1–8) all lived as literals in `.cs` files.
   Tweaking balance or reordering scenes required a code change and recompile.

---

## Folder structure

```
Redesigned/
├── Data/               ScriptableObject definitions (pure data, no logic)
├── Events/             Ryan Hipple's event channel pattern
├── Core/               Persistence — the one legitimate singleton
├── Player/             Movement and fishing state machine
├── Fishing/            Catch logic, minigame, catch UI
├── Game/               Scene flow, day/night, shop
├── Story/              Centralised story progression
├── UI/                 Dialog typewriter, blood screen
└── Camera/             Camera follow
```

---

## Layer 1 — Data (ScriptableObjects)

ScriptableObjects are Unity assets that live in the Project panel. They hold
data and are editable in the Inspector without touching any C# file.

### `FishDataSO`
Replaces the original `FishData` MonoBehaviour.

| Field | Type | Purpose |
|---|---|---|
| `title` | string | Display name |
| `description` | string | Flavor text |
| `sprite` | Sprite | Catch card image |
| `difficulty` | int (1–3) | Minigame difficulty |
| `location` | FishLocation | Near / Far catch zone |
| `sellingPrice` | int | Cash value |
| `rodRequirement` | int | Min rod upgrade level |
| `baitRequirement` | int | Min bait upgrade level |
| `type` | FishType | Fish / Junk / BodyPart |
| `bodyPartId` | BodyPartId | Which body part (if any) |

The `BodyPartId` enum (`None`, `SeveredHand`, `Arm`, `Leg`, `OrganNecklace`,
`MaleHead`, `WifeTorso`) replaces string name comparisons like
`caughtFish.name.Equals("Arm")` that were scattered through `FishingManager.CloseUI()`.

**To add a new fish:** right-click in Project → Create → Still Waters → Fish Data.
Fill in the Inspector. Zero code changes required.

### `UpgradeConfigSO`
One asset holds all balance data previously hardcoded in `StatsStorage`:

```
moveSpeeds      float[]   { 1, 2, 3 }
stringLengths   float[]   { 1.75, 2.25, 3.25 }
meanWaitTimes   int[]     { 17, 13, 8 }
inventorySpaces int[]     { 3, 5, 8 }
engineCosts     int[]     { 60, 70 }
rodCosts        int[]     { 75, 80 }
baitCosts       int[]     { 50, 75 }
inventoryCosts  int[]     { 20, 70 }
```

Change any value in the Inspector and it takes effect immediately in Play mode.

### `DialogSequenceSO`
A `string[]` of dialog lines as a project asset. Writers edit dialog without
opening a C# file. Previously, lines like `"What the... hell is this?"` were
hardcoded inside `FishingManager.CloseUI()`.

**To add a new dialog sequence:** Create → Still Waters → Dialog Sequence.

### `SceneFlowConfigSO`
Maps story beats to Unity scene names:

```
introScene, tutorialScene, mainGameScene,
firstNightScene, horrorEventScene, postHorrorScene,
penultimateScene, endScene
```

`GameFlowController` reads these names. Reordering scenes in Build Settings no
longer silently breaks the entire story.

---

## Layer 2 — Events (Ryan Hipple's pattern)

Ryan Hipple's Unite Austin 2017 talk introduced ScriptableObject event channels.
Each event is a `.asset` file in the Project panel.

**How it works:**

```
Publisher                Event Asset (.asset)         Subscriber
────────────             ─────────────────────        ──────────────
myEvent.Raise()    →    GameEvent                →   GameEventListener
                         (ScriptableObject)            .response.Invoke()
```

Systems communicate without knowing about each other. The Inspector wiring
diagram is the only coupling.

### `GameEvent` / `GameEventListener`
Void event channel. Drop `GameEventListener` on any GameObject, drag a
`GameEvent` asset into it, wire a `UnityEvent` response — no code needed.

### `FishCaughtEvent` / `FishCaughtEventListener`
Typed event channel that carries a `FishDataSO` payload. Used to notify the
catch UI when a fish is reeled in.

**Create new event assets:** Create → Still Waters → Events → …

---

## Layer 3 — Core persistence

### `SaveData`
Plain C# class (`[System.Serializable]`, no MonoBehaviour). Holds all runtime
state that needs to survive scene loads:

- Economy: `cash`
- World: `dayNumber`
- Upgrade levels: `engineLevel`, `rodLevel`, `baitLevel`, `inventoryLevel`
- Story flags: `severedHandEventPassed`, `armFound`, `legFound`, `organNecklaceFound`, `maleHeadFound`, `partsFound`
- Inventory: `caughtCount`, `inventoryTitles[]`, `inventoryPrices[]`

Because it is a plain class, it can be serialized to disk for save-file support
without any structural change.

### `SaveManager`
**The one legitimate singleton.** Uses `DontDestroyOnLoad` because it must
survive scene transitions. Every other system that needs save data receives a
`[SerializeField] SaveManager saveManager` reference via the Inspector — they
do not access `SaveManager.instance` directly.

Exposes computed properties so callers never index into arrays themselves:

```csharp
public float MoveSpeed     => upgradeConfig.moveSpeeds[data.engineLevel];
public float StringLength  => upgradeConfig.stringLengths[data.rodLevel];
public int   MeanWaitTime  => upgradeConfig.meanWaitTimes[data.baitLevel];
public int   InventorySpace => upgradeConfig.inventorySpaces[data.inventoryLevel];
public bool  IsInventoryFull => data.caughtCount >= InventorySpace;
public bool  IsFullyUpgraded => …
```

---

## Layer 4 — Player

### `PlayerController`
Handles movement, input gating, and player-facing UI (thoughts, inventory text).
**Does not know fishing outcomes or story state.**

State machine:

```
UIDisplay  ←─── input fully blocked (shop open, dialog active)
Idle       ←─── standing still, can press Jump to fish
Moving     ←─── horizontal input held
```

Subscribes to `FishingController` C# events:

```csharp
fishingController.onFishCaught += HandleFishCaught;
fishingController.onFishLost   += HandleFishLost;
```

On `HandleFishCaught`: plays audio, updates inventory text.
On `HandleFishLost`: shows a random lost-fish thought.

Thoughts (`lateDayThoughts`, `inventoryFullThoughts`, `lostFishThoughts`,
`goodCatchThoughts`, `okCatchThoughts`) are `DialogSequenceSO` assets — swap
text in the Inspector.

### `FishingController`
Owns the full fishing state machine. **Raises C# events outward; never calls
into any other system directly.**

State machine:

```
Idle → Casting → FishBit → Minigame → ReelingIn → (back to Idle)
                                    ↘ FishCaught → ReelingIn → (back to Idle)
```

Key events:

```csharp
public event Action<FishDataSO> onFishCaught;  // fish successfully reeled in
public event Action             onFishLost;    // fish escaped
```

`FishingMinigame` raises `onWin` / `onLoss` C# events that `FishingController`
subscribes to — **the minigame no longer calls `PlayerManager.instance.StopAllCoroutines()`.**

---

## Layer 5 — Fishing

### `FishingLogic`
Pure catch-selection. `PickFish(float playerX)` receives player position as a
parameter — **it does not read `PlayerManager.instance.transform`.**

Pool-building logic (mirrors original weighting):

- Before horror event: fish × 3, junk × 1
- After horror event: fish × 2, bodyParts × 2 (× 4 if fully upgraded)
- Filtered by: rod level, bait level, Near/Far zone, already-found body parts

Body part filtering uses `BodyPartId` enum — no string comparisons.

### `FishingMinigame`
The quick-time event. Difficulty 1–3 controls bar count, bar width, and speed.

Raises C# events only:

```csharp
public event Action onWin;
public event Action onLoss;
```

`FishingController` subscribes and transitions state. **Zero reference to
`PlayerController`.**

### `CatchUIController`
Shows the catch card UI. Subscribes to `FishingController.onFishCaught` in
`Start()`. When the player closes the card, routes to `StoryManager.HandleCatch`.

---

## Layer 6 — Game

### `GameFlowController`
Scene transitions only. All scene names come from `SceneFlowConfigSO` — no
build indices.

`PlayNextEvent(bool isScared)` is called by:
- `DialogController.EndDialog()` when `switchScene = true`
- `DayNightCycle.EndDay()` via the End Day button

Story branching logic (which scene loads next) lives here, not scattered
across three classes.

### `DayNightCycle`
Visual-only fade and night audio. Exposes `IsNightTime` as a read-only
property. **Does not call `GameManager.instance`** — calls `gameFlow.PlayNextEvent`
via a direct Inspector reference.

### `ShopController`
Reads upgrade costs from `UpgradeConfigSO`. Writes upgrade levels to
`SaveManager.data`. Uses `PlayerController.CurrentState` property — **does not
call `PlayerManager.instance.SetState()`.**

---

## Layer 7 — Story

### `StoryManager`
All story progression in one place. Previously split across:

- `GameManager.PlayNextEvent()` — set story flags and loaded scenes
- `FishingManager.CloseUI()` — 6 `if (caughtFish.name.Equals(…))` branches
- `StoryEventManager.Start()` — checked upgrade conditions for the horror event

`HandleCatch(FishDataSO fish, PlayerController player)` switches on
`fish.bodyPartId` and either starts a dialog or returns control to the player:

```
SeveredHand  → isScared=true, severedHandEventPassed=true, dialog (switchScene=true)
Arm / Leg / OrganNecklace / MaleHead → save flag, partsFound++, dialog (switchScene=false)
WifeTorso    → dialog (switchScene=true)
default      → player.SetUIDisplay(false), show caught-fish thought
```

---

## Layer 8 — UI

### `DialogController`
Typewriter dialog display. Caller passes:
- `DialogSequenceSO sequence` — the lines to show
- `bool switchScene` — whether ending the dialog triggers a scene change
- `PlayerController player` — whose input to block during dialog

`EndDialog()` calls `gameFlow.PlayNextEvent(player.IsScared)` if `switchScene`,
otherwise calls `player.SetUIDisplay(false)`. **No `GameManager.instance` call
inside the dialog system.**

### `BloodScreen`
Self-contained red screen flash. No singleton. `CatchUIController` holds a
direct `[SerializeField] BloodScreen bloodScreen` reference.

---

## Layer 9 — Camera

### `CameraMovement`
Accepts a `Transform target` reference. Only tracks rightward movement
(`deltaX >= 0`). Decoupled from any specific player implementation.

---

## Dependency graph (after)

```
SaveManager (singleton)
    ↑ injected via Inspector into all systems below

PlayerController ──→ FishingController (direct ref, same GameObject)
                           │
                     onFishCaught ──→ CatchUIController
                     onFishLost   ──→ PlayerController

FishingController ──→ FishingLogic (pick fish)
                  ──→ FishingMinigame (start minigame)
                           │
                       onWin / onLoss ──→ FishingController

CatchUIController ──→ StoryManager.HandleCatch()
                           │
                     DialogController.StartDialog()
                           │
                     GameFlowController.PlayNextEvent()
                           │
                     SceneManager.LoadScene(sceneFlow.someScene)

DayNightCycle ──→ GameFlowController.PlayNextEvent()
ShopController ──→ SaveManager (read/write upgrades)
```

No cycles. Dependencies flow downward.

---

## How to migrate a scene

1. Add a `SaveManager` GameObject (with `UpgradeConfigSO` assigned) — mark it
   as the scene's persistent manager or ensure it carries over from a previous
   scene.
2. Replace `PlayerManager` with `PlayerController` + `FishingController` on the
   player GameObject. Wire Inspector references.
3. Replace `FishingManager` with `FishingLogic` + `CatchUIController`. Create
   `FishDataSO` assets for each fish/junk/body-part prefab.
4. Replace `GameManager` with `GameFlowController`. Create one
   `SceneFlowConfigSO` asset shared across all scenes.
5. Replace `DialogManager` with `DialogController`. Create `DialogSequenceSO`
   assets for each dialog sequence.
6. Replace `ShopManager` with `ShopController`. Create one `UpgradeConfigSO`
   asset (same one referenced by `SaveManager`).
7. Replace `StoryEventManager` and the story branches in `FishingManager` with
   `StoryManager`. Assign all discovery `DialogSequenceSO` assets.
8. Replace `DayNightCycle` with the new version. Wire `GameFlowController` and
   `SceneFlowConfigSO`.
9. Replace `BloodScreen` — remove the singleton, wire direct reference in
   `CatchUIController`.
10. Replace `CameraMovement` — swap `public GameObject player` for
    `[SerializeField] Transform target`.
11. Create `GameEvent` / `FishCaughtEvent` assets if you want cross-scene
    ScriptableObject events (optional — C# events already cover same-scene comms).

---

## What was deliberately kept simple

- **Parallax system** (`ParallaxCamera`, `ParallaxBackground`, `ParallaxLayer`) —
  already uses a delegate pattern with no singletons. Left unchanged.
- **FishSpawner** — fully self-contained, no cross-system calls. Left unchanged.
- **Same-scene communication** uses C# events rather than ScriptableObject
  events. SO events shine for cross-scene communication (e.g. a UI scene
  reacting to a game scene event). For components that always exist in the same
  scene, C# events with direct Inspector references are simpler and faster.
