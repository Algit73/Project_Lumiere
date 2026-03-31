# Lumiere — Tasks & NPC Movement System

All files live in `Assets/Scripts/Tasks/`.

---

## Block Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  SCENE SETUP (Inspector / Editor)                                               │
│                                                                                 │
│   TaskTemplateSO ──────────────────────────────────────────────────────────┐   │
│   (ScriptableObject asset)                                                  │   │
│    • taskID, taskType, difficulty                                           │   │
│    • objectPool [ InteractiveDataSO, ... ]                                  │   │
│    • promptTemplates [ string, ... ]                                        │   │
│    • cueHierarchy  [ CueType, ... ]   ──────► CueType (enum)               │   │
│    • successCriteria ────────────────────────► SuccessCriteria              │   │
│    • timeLimit                                  • type (SuccessCriteriaType)│   │
│    • feedbackConfig ─────────────────────────► FeedbackConfig               │   │
│                                                 • successAudio / failAudio  │   │
│                                                 • successVFX / failVFX      │   │
│                                                 • success/failColor         │   │
└─────────────────────────────────────────────────┬───────────────────────────┘   │
                                                  │ assigned to                   │
                                                  ▼                               │
┌─────────────────────────────────────────────────────────────────────────────────┤
│  RUNTIME ORCHESTRATION                                                          │
│                                                                                 │
│  ┌─────────────────┐   SpawnTask()   ┌──────────────────────┐                  │
│  │ TaskRandomizer  │ ──────────────► │    TaskManager        │  (Singleton)     │
│  │                 │                 │                        │                  │
│  │ • Finds all     │  TriggerTask()  │  _activeTasks{}       │                  │
│  │   CharacterAgent│ ◄─── player ──► │  character → instance │                  │
│  │   in scene      │                 │                        │                  │
│  │ • Picks N (2–4) │  EvaluateInter-│  Ticks all tasks every │                  │
│  │ • Filters by    │  action()       │  frame (time limits)   │                  │
│  │   eligibility   │ ◄─── player ──► │                        │                  │
│  │ • Weights by    │                 └──────────┬─────────────┘                  │
│  │   inverse usage │                            │ creates/drives                 │
│  │ • Shows icon    │                            ▼                               │
│  └────────┬────────┘                 ┌──────────────────────┐                  │
│           │ assigns to               │    TaskInstance       │                  │
│           ▼                          │  (runtime object)     │                  │
│  ┌─────────────────┐                 │                        │                  │
│  │ CharacterAgent  │◄────────────────│  • State machine:      │                  │
│  │                 │  CurrentTask    │    Idle→Running→       │                  │
│  │ • characterID   │                 │    Evaluating→         │                  │
│  │ • eligibleTypes │                 │    Complete/Failed      │                  │
│  │ • min/maxDiff   │                 │                        │                  │
│  │ • iconAnchor    │                 │  Events fired:         │                  │
│  │ • ShowIcon()    │                 │    OnStarted           │                  │
│  │ • HideIcon()    │                 │    OnEvaluated(bool)   │                  │
│  └─────────────────┘                 │    OnCompleted         │                  │
│                                      │    OnFailed            │                  │
│                                      │    OnCueAdvanced       │                  │
│                                      └────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────────────────────┤
│  NPC MOVEMENT  (on the same NPC GameObject)                                     │
│                                                                                 │
│   CharacterMover (ithappy)                                                      │
│        ▲               ▲                ▲                                       │
│        │               │                │                                       │
│  NPCPatrol       NPCRouteWander     NPCWander                                   │
│  (waypoint       (random subpath    (NavMesh-based                               │
│   loop,          of circular loop,  free wander —                               │
│   fixed order)   no NavMesh)        kept but unused                             │
│                                     on most NPCs)                               │
│                                                                                 │
│  All three call:  CharacterMover.SetInput(axis, lookTarget, isRun, isJump)      │
│  All three check: CharacterAgent.CurrentTask → pause if Running/Evaluating      │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Components Reference

### 1. `TaskEnums.cs` — Shared Enumerations

Pure definitions, no MonoBehaviour.

| Enum | Values | Purpose |
|------|--------|---------|
| `TaskType` | Find, Story, Manipulate, Sort, Match, Phono, Name | Category of the cognitive/language task |
| `CueType` | NoCue, VisualHighlight, AudioCue, SemanticHint, PhonemicCue | Progressive hint levels, ordered from least to most supportive |
| `SuccessCriteriaType` | SelectedObjectIsTarget, AllObjectsSorted, AllObjectsMatched, CountReached, Custom | How success is measured at runtime |
| `TaskState` | Idle, Running, Evaluating, Complete, Failed | Lifecycle state of one active task |

---

### 2. `FeedbackConfig.cs` — Feedback Data Struct

```csharp
[System.Serializable]
public struct FeedbackConfig { ... }
```

A plain serializable struct embedded inside `TaskTemplateSO`. No logic — just holds references:

| Field | Type | Purpose |
|-------|------|---------|
| `successAudio` | AudioClip | Played on correct answer |
| `failAudio` | AudioClip | Played on wrong answer or timeout |
| `successVFX` | GameObject | Particle prefab spawned on success |
| `failVFX` | GameObject | Particle prefab spawned on failure |
| `successColor` | Color | UI color for correct feedback |
| `failColor` | Color | UI color for wrong feedback |

---

### 3. `SuccessCriteria.cs` — Success Rule

```csharp
[System.Serializable]
public class SuccessCriteria { ... }
```

Also embedded inside `TaskTemplateSO`. Defines the winning condition evaluated at runtime by `TaskInstance`.

| Field | Purpose |
|-------|---------|
| `type` | Which evaluation rule to apply (`SuccessCriteriaType`) |
| `targetObjectID` | Name/ID the interacted object must match (Find / Name tasks) |
| `requiredCount` | How many correct interactions needed (Sort / Match / CountReached) |
| `targetCategory` | Category tag the object must belong to (Sort tasks) |

---

### 4. `TaskTemplateSO.cs` — Task Blueprint (ScriptableObject)

```
Right-click in Project → Create → Tasks → Task Template
```

The **design-time** definition of a task. One `.asset` file per task. Multiple characters can be assigned the same template.

| Field | Purpose |
|-------|---------|
| `taskID` | Unique string identifier (e.g. `"task_find_apple_01"`) |
| `taskType` | Category (`TaskType` enum) |
| `difficulty` | 1–5 scale used by `TaskRandomizer` |
| `objectPool` | List of `InteractiveDataSO` profiles eligible to appear in the task |
| `promptTemplates` | Localized strings shown to the player; `{target}` is replaced at runtime |
| `cueHierarchy` | Ordered list of `CueType` hints; advances each time the player struggles |
| `successCriteria` | Embedded `SuccessCriteria` struct |
| `timeLimit` | Seconds before auto-fail (0 = unlimited) |
| `feedbackConfig` | Embedded `FeedbackConfig` struct |

---

### 5. `TaskInstance.cs` — Runtime Task Object

A plain C# class (not a MonoBehaviour). Created at runtime by `TaskManager.SpawnTask()`. One instance per active character.

#### State Machine

```
  Idle ──StartTask()──► Running ──Evaluate()──► Evaluating
                           │                        │
                           │ (wrong answer)          │ (success criteria met)
                           ◄────────────────────────┤
                           │                        │
                      Tick() expires           Complete ──► removed from TaskManager
                           │
                         Failed ──► removed from TaskManager
```

#### Key Members

| Member | Type | Description |
|--------|------|-------------|
| `Template` | `TaskTemplateSO` | The blueprint this instance was created from |
| `AssignedTo` | `GameObject` | The NPC this task belongs to |
| `State` | `TaskState` | Current lifecycle position |
| `ActivePrompt` | `string` | Randomised prompt string shown to player |
| `CueLevel` | `int` | Current index into `cueHierarchy` |
| `CorrectCount` | `int` | Correct interactions accumulated |
| `ElapsedTime` | `float` | Time since task started |
| `StartTask()` | method | Idle → Running |
| `Tick(dt)` | method | Called every frame by TaskManager; handles timeout |
| `Evaluate(Interactive)` | method | Checks if the player's interaction satisfies the criteria |
| `AdvanceCue()` | method | Moves to the next hint level |

#### Events

| Event | When fired |
|-------|-----------|
| `OnStarted` | Task enters Running state |
| `OnEvaluated(bool)` | After every player interaction; bool = was it correct |
| `OnCompleted` | Criteria satisfied |
| `OnFailed` | Timeout reached |
| `OnCueAdvanced(CueType)` | Hint level incremented |

---

### 6. `TaskManager.cs` — Central Coordinator (Singleton MonoBehaviour)

Attach to a **persistent** `GameManager` GameObject. Persists across scenes via `DontDestroyOnLoad`.

Internally holds `Dictionary<GameObject, TaskInstance>` mapping each NPC to its active task.

#### Public API

| Method | When to call |
|--------|-------------|
| `SpawnTask(template, character)` | Level setup / dialogue trigger — creates and registers a task |
| `TriggerTask(character)` | Player taps the character's icon (Idle → Running) |
| `EvaluateInteraction(character, interactive)` | Player interacts with an object while a task is running |
| `HasActiveTask(character)` | Check before spawning a second task |
| `GetTask(character)` | Retrieve the current `TaskInstance` for any purpose |

`TaskManager` also handles:
- Ticking all active tasks every frame (time limits)
- Playing `FeedbackConfig` audio / spawning VFX
- Removing completed/failed tasks from the dictionary

---

### 7. `CharacterAgent.cs` — NPC Identity & Eligibility

Attach to every NPC that can receive tasks. No movement logic here — purely identity.

| Field | Purpose |
|-------|---------|
| `characterID` | Unique string (e.g. `"npc_cat"`) |
| `eligibleTaskTypes` | Which `TaskType` values this character accepts. Empty = accept all. |
| `minDifficulty / maxDifficulty` | Difficulty range this character will be assigned |
| `iconAnchor` | Transform above the character's head where the icon spawns |
| `iconPrefab` | The icon GameObject prefab |
| `CurrentTask` | Property → queries `TaskManager.GetTask(gameObject)` |
| `ShowIcon(taskType)` | Spawns the icon prefab at `iconAnchor` |
| `HideIcon()` | Destroys the active icon |

---

### 8. `TaskRandomizer.cs` — Automatic Task Assignment at Scene Start

Attach on the same `GameManager` as `TaskManager`. Runs once in `Start()`.

#### Algorithm

1. Find **all** `CharacterAgent` components in the scene.
2. Randomly pick **N** (between `minCharacters` and `maxCharacters`) of them.
3. For each picked character:
   - Filter `taskCatalogue` by `eligibleTaskTypes` and difficulty range.
   - Remove templates already used in this pass.
   - Pick from the remaining list using **inverse usage weighting** (least-used template wins more often).
   - Call `TaskManager.SpawnTask()`.
   - Wire `OnCompleted` / `OnFailed` → `HideIcon()`.
   - Call `CharacterAgent.ShowIcon()`.

| Inspector Field | Purpose |
|-----------------|---------|
| `minCharacters / maxCharacters` | 2–4 range of NPCs to activate |
| `taskCatalogue` | All `TaskTemplateSO` assets in the project (drag them all in) |

---

### 9. `NPCPatrol.cs` — Fixed-Order Waypoint Patrol

Drives `CharacterMover` through a list of `Transform` waypoints in a **fixed** sequence.

| Mode | Behaviour |
|------|-----------|
| `PingPong` | A→B→C→B→A→… |
| `Loop` | A→B→C→A→B→… |
| `Once` | A→B→C (stop) |

Key inspector fields: `waypoints`, `loopMode`, `runBetweenWaypoints`, `arrivalThreshold`, `waitAtWaypoint`.

Pauses automatically (stands idle) when `CharacterAgent.CurrentTask` is Running/Evaluating.

---

### 10. `NPCRouteWander.cs` — Random Subpath Walker *(recommended for cats / free NPCs)*

Treats the waypoint list as a **circular loop**. On each trip it picks two random indices and walks through **all points in between in order**, wrapping at the end of the list.

#### Example (6 points A–F, stored as 0–5)

| Random pick | Computed route |
|-------------|---------------|
| Start = B (1), End = E (4) | B → C → D → E |
| Start = F (5), End = C (2) | F → A → B → C (wraps) |

#### How it works

1. `Start()` → calls `PickNewRoute()`.
2. `PickNewRoute()` → chooses two distinct random indices and builds `_currentRoute` list.
3. `Update()` → steers toward `_currentRoute[_routeIndex]` using `CharacterMover.SetInput()`.
4. On arrival at an intermediate waypoint → brief `waitAtWaypoint` pause.
5. On arrival at the final destination → random `minWaitAtDestination–maxWaitAtDestination` pause, then `PickNewRoute()`.
6. Pauses (idles) if `CharacterAgent.CurrentTask` is Running/Evaluating.

**No NavMesh required.** Requires `CharacterMover` on the same GameObject.

| Inspector Field | Purpose |
|-----------------|---------|
| `waypoints` | All loop points **in order** (A, B, C, D, E, F) |
| `runBetweenWaypoints` | Walk vs run |
| `arrivalThreshold` | Distance to consider a point reached (metres) |
| `waitAtWaypoint` | Pause at intermediate points (seconds) |
| `minWaitAtDestination` | Min rest at end of each trip |
| `maxWaitAtDestination` | Max rest at end of each trip |
| `pauseDuringTask` | Freeze while task is active |

**Gizmos:** In Play mode, the full loop is drawn in grey, the active sub-route in yellow, and the current target in green.

---

### 11. `NPCWander.cs` — NavMesh Random Wander *(kept but not recommended for Cat_03)*

Earlier system. Uses `NavMeshAgent` for path planning and `CharacterMover` for actual movement (hybrid). Picks random points within `wanderRadius`, walks to them, waits, repeats.

Requires a **baked NavMesh** in the scene and a `NavMeshAgent` component.
Stuck detection: if the character moves less than `stuckDistanceThreshold` metres over `stuckCheckInterval` seconds, a new destination is chosen.

---

## How the Systems Connect at Runtime

```
Scene Start
    │
    ▼
TaskRandomizer.Start()
    ├── finds CharacterAgents (includes the cat, teachers, etc.)
    ├── picks N of them at random
    └── for each:
          TaskManager.SpawnTask(template, npc)  ──► creates TaskInstance (Idle)
          CharacterAgent.ShowIcon()              ──► icon appears above head

While playing:
    NPCRouteWander.Update()  ──► CharacterMover.SetInput()  ──► character walks
                                 (pauses if task is Running)

Player taps icon:
    TaskManager.TriggerTask(npc)  ──► TaskInstance: Idle → Running
                                      TaskInstance.OnStarted fired
                                      (UI shows prompt)

Player interacts with object:
    TaskManager.EvaluateInteraction(npc, interactive)
        ──► TaskInstance.Evaluate()
              ├── correct → CorrectCount++; OnEvaluated(true)
              │             if satisfied → Complete → TaskManager removes task
              │                                        CharacterAgent.HideIcon()
              └── wrong  → AdvanceCue(); OnEvaluated(false); back to Running

Time limit expires:
    TaskInstance.Tick() ──► Fail() ──► OnFailed
                                         TaskManager removes task
                                         CharacterAgent.HideIcon()
```

---

## Quick Setup Checklist

1. **GameManager** object in scene:
   - Add `TaskManager`
   - Add `TaskRandomizer` → populate `taskCatalogue`

2. **Each NPC** that receives tasks:
   - Add `CharacterAgent` → set `characterID`, eligibility, `iconAnchor`

3. **Each NPC** that moves:
   - Has `CharacterMover` (ithappy, already on prefab)
   - Add `NPCRouteWander` → populate `waypoints` list with ordered Transforms
   - *Remove NavMeshAgent if switching from NPCWander*

4. **Task assets**:
   - Right-click in Project → Create → Tasks → Task Template
   - Fill in fields, drag into `taskCatalogue`

5. **Cat animation**:
   - Run **Tools → Create Cat Animator Controller**
   - Assign resulting `Cat_Movement` controller to Cat_03's Animator component
