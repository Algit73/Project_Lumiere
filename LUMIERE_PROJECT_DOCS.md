# Lumiere Android — Project Documentation
> Branch: `xr-android` | Repo: `Algit73/Project_Lumiere`
> Last updated: May 2026

---

## 1. Project Overview

**Lumiere** is an AR language-learning app for Android built with Unity (URP + AR Foundation). The player sees the real world through the phone camera. NPC characters (humans, animals) are placed in the AR scene. The teacher/therapist presses the **Lumiere button** to open a task menu, selects an activity type, and the app guides a structured interaction.

---

## 2. Scene Architecture

| Scene name | Purpose |
|---|---|
| `XR_Home` | Main AR scene. Live camera feed, placed AR content, Lumiere button |
| `Farm` | Pure-3D dialogue scene loaded for the Find Object task |
| `Main Menu` | App entry point (loads via `MainMenu.cs`) |

**Scene transition flow (Find Object task):**
```
XR_Home  →[SceneTransitionManager.GoTo("Farm")]→  Farm  →[ReturnHome()]→  XR_Home
```
`SceneTransitionManager` is DontDestroyOnLoad and carries: pending task type + return-scene name + saved AR anchor pose.

---

## 3. Script Inventory — `Assets/Scripts/`

### 3.1 Core / Entry Points

| File | Role |
|---|---|
| `Lumier_MainController.cs` | Central controller. Holds refs to TaskMenu, FindObjectTask, HUD button, legacy scene controllers. Wires the Lumiere glow-button → opens TaskMenuController. |
| `UIManager.cs` | Simple helper with `BackToMenu()`. |
| `MainMenu.cs` | `LoadScene(string)` helper used by the main-menu scene. |
| `SceneTransitionManager.cs` | DontDestroyOnLoad singleton. Transfers task type + AR anchor pose across scenes. |
| `ARSceneAnchorController.cs` | Placed on the AR content root in `XR_Home`. Saves/restores the placed-object world pose when returning from a task scene. |

### 3.2 Task Menu — `Assets/Scripts/UI/`

| File | Role |
|---|---|
| `TaskMenuController.cs` | Controls the **LumiereMenuPanel**. 6 buttons: Find, Name, Story, Manipulate, Sort, Match. Scale-in/out animation. Fires `OnTaskSelected(TaskType)` event. |

**UI hierarchy (auto-created by `Tools > Build Lumiere Task Menu`):**
```
Canvas
 └── LumiereMenuOverlay        ← dim background; TaskMenuController lives here
       ├── LumiereMenuPanel    ← centered square card (scale-animated)
       │     ├── Title (TMP)
       │     ├── CloseButton
       │     └── ButtonGrid    ← GridLayoutGroup (2 cols)
       │           ├── Btn_Find
       │           ├── Btn_Name
       │           ├── Btn_Story
       │           ├── Btn_Manipulate
       │           ├── Btn_Sort
       │           └── Btn_Match
```

### 3.3 Find Object Task — `Assets/Scripts/Tasks/`

**Entry point:** `Lumier_MainController.HandleTaskSelected(TaskType.Find)`
→ saves AR anchor pose → calls `SceneTransitionManager.GoTo("Farm", TaskType.Find, …)`
→ Farm scene starts → `FindObjectTask.Start()` detects pending task → calls `Begin()`.

| File | Role |
|---|---|
| `FindObjectTask.cs` | Orchestrates the full Find-Object dialogue sequence (see §4). |
| `CharacterAgent.cs` | MonoBehaviour on every NPC. Exposes `characterID`, `eligibleTaskTypes`, `iconAnchor`, `CurrentTask`. |
| `TaskRandomizer.cs` | Finds all `CharacterAgent`s in the scene, picks N random ones, assigns random `TaskInstance`s. |
| `TaskInstance.cs` | Runtime data for an assigned task (type, difficulty, status). |
| `TaskTemplateSO.cs` | ScriptableObject template for task configurations. |
| `TaskEnums.cs` | `TaskType` enum: Find, Name, Story, Manipulate, Sort, Match. |
| `FindObjectDialogueData.cs` | ScriptableObject / fallback for dialogue lines used in Find task. |
| `DialogueLine.cs` | Data struct: `speakerName` + `text`. |
| `NPCWander.cs` | NavMeshAgent-based wandering. Disabled during dialogue. |
| `NPCRouteWander.cs` | Route-based wander. `Pause()` / `Resume()` called during dialogue. |
| `NPCPatrol.cs` | Waypoint patrol. `Pause()` / `Resume()` called during dialogue. |
| `SuccessCriteria.cs` | Evaluates whether a task condition has been met. |
| `FeedbackConfig.cs` | Config for feedback responses (correct/incorrect). |

### 3.4 UI Components — `Assets/Scripts/UI/`

| File | Role |
|---|---|
| `SpeechBubbleUI.cs` | **World-space** speech bubble above NPC. Auto-builds Canvas hierarchy at runtime. Contains: speaker label, dialogue text, 🔊 replay button, speech-speed slider (0.5–2.0×). |
| `DialogueNavigationUI.cs` | Screen-space ◀ ▶ nav arrows. Auto-builds its own Canvas. Wired by FindObjectTask. |
| `ARDialogueMode.cs` | Handles AR↔3D transition: disables ARCameraBackground + TrackedPoseDriver, tweens camera to NPC framing pose, manages screen-fade overlay. Singleton. |
| `RoundedCorners.cs` | Component that feeds `cornerRadius` to the `Custom/RoundedUI` shader for panel aesthetics. |
| `DynamicSizeAdjuster.cs` | Adjusts UI element sizes at runtime (e.g. safe-area insets). |
| `TaskMenuBuilder.cs` (Editor) | `Tools > Build Lumiere Task Menu` — creates the full Canvas/LumiereMenuPanel hierarchy in the current scene. |

### 3.5 TTS / Speech

| File | Role |
|---|---|
| `TTSManager.cs` | Singleton (DontDestroyOnLoad). Routes `Speak(text)` and `SetSpeechSpeed(float)` calls to the active TTS provider. |
| `OpenAITTSManager.cs` | OpenAI TTS API integration. `SetSpeed(float)` applies playback-rate changes. |
| `tts_pc.cs` | PC/editor TTS fallback (Windows SAPI). |

### 3.6 Interaction System — `Assets/Scripts/Interaction/`

| File | Role |
|---|---|
| `Interactive.cs` | Base component for tappable/clickable objects. |
| `InteractionData.cs` | Runtime interaction payload (label, description, etc.). |
| `InteractiveDataSO.cs` | ScriptableObject version of interaction data. |
| `CommandsCenter.cs` | Routes tap/click events to the correct handler. |
| `JustShowInCard.cs` | Shows a card UI for the tapped object. |
| `Prohibited_Objects.cs` | Marks objects that should not be interactable. |

### 3.7 Characters — `Assets/Scripts/Human and Animals/`

| File | Role |
|---|---|
| `HumanStateManager.cs` | Animator state machine wrapper for human NPCs. |
| `CatStateManager.cs` | Animator state machine for cat characters. |
| `CatController.cs` | Cat-specific movement/interaction logic. |
| `AnimalStateManager.cs` | Generic animal state machine. |
| `Points.cs` | Manages score/point tracking for characters. |

### 3.8 Visual Effects — `Assets/Scripts/Visual Effects/`

| File | Role |
|---|---|
| `SinusoidalGlowEffect.cs` | Sinusoidal pulse glow on the Lumiere main button. |
| `SinusoidalGlowEffect_Auto.cs` | Auto-starts the glow on Awake. |
| `ButtonGlowEffect.cs` | Glow effect specifically for UI buttons. |
| `GlowEffect.cs` | Generic outline/glow on 3D objects. |
| `OutlineController.cs` | Manages outline shader parameters on selected objects. |

### 3.9 Other

| File | Role |
|---|---|
| `MobileCameraController.cs` | Touch/tilt camera control on mobile. |
| `ZoomCamera.cs` | Pinch-to-zoom for the AR camera. |
| `PlaneSelection.cs` | AR plane tap-to-place logic. |
| `DataHandler.cs` | Local persistence (PlayerPrefs / JSON). |
| `DelayUtility.cs` | Coroutine-free delay helper. |
| `SyncText.cs` | Syncs TMP text fields. |

---

## 4. Find Object Task — Detailed Flow

```
[User taps Lumiere Button]
        ↓
TaskMenuController.Open()  →  User selects "Find"
        ↓
Lumier_MainController.HandleTaskSelected(TaskType.Find)
  • Saves AR anchor pose
  • SceneTransitionManager.GoTo("Farm", TaskType.Find, "XR_Home", anchorPose)
        ↓
[Farm scene loads]
FindObjectTask.Start()  →  ConsumePendingTask → Begin()
        ↓
1. PickRandomNPC()          — finds all active CharacterAgent components
2. PrepareDialogueLines()   — from FindObjectDialogueData asset or fallback
3. PauseNPC()               — disables NPCWander/NPCRouteWander/NPCPatrol
4. ComputeCameraPose()      — frames NPC upper half at 60% screen fill
5. ARDialogueMode.EnterDialogueMode(camPos, camRot, callback)
   ├── Fades to black
   ├── Disables ARCameraBackground + TrackedPoseDriver
   ├── Tweens camera to framing pose
   └── Fades back in → fires callback
        ↓ (in callback)
6. SpawnBubble()            — creates SpeechBubbleUI at world-space anchor above NPC head
7. DialogueNavigationUI.Show()  — shows ◀ ▶ arrows
8. ShowLine(0)              — displays first DialogueLine, triggers TTS
        ↓
[User taps ◀ / ▶]
  OnPrev / OnNext → ShowLine(index) → SpeechBubbleUI.Show(line) → TTSManager.Speak()
        ↓
[User taps ▶ on last line]
FinishSession()
  • Hides bubble + nav
  • ResumeNPC()
  • ARDialogueMode.ExitDialogueMode() → fade + re-enable AR + fade
  • SceneTransitionManager.ReturnHome() → loads XR_Home
```

---

## 5. SpeechBubbleUI — World-Space Canvas

**File:** `Assets/Scripts/UI/SpeechBubbleUI.cs`

The bubble is a **World Space Canvas** auto-built entirely in code (no prefab).

### Key Inspector fields
| Field | Default | Notes |
|---|---|---|
| `bubbleWidth` | `0.55f` | World units wide |
| `bubbleHeight` | `0.30f` | World units tall |
| `panelColor` | Dark blue-black | Background fill |
| `cornerRadius` | `18f` | Pixels, fed to RoundedUI shader |
| `verticalOffset` | `0.1f` | Extra upward nudge of the panel |

### Auto-built hierarchy
```
SpeechBubble_<id>  [Canvas — WorldSpace, scale 1/200]
 └── BubblePanel   [Image + RoundedCorners, 110×60 canvas px]
       ├── SpeakerLabel   [TMP, 14pt bold, light-blue]
       ├── BodyText       [TMP, 13pt white, word-wrap]
       └── ControlsRow    [HorizontalLayoutGroup]
             ├── ReplayButton   [Button "🔊", 34×34 px]
             └── SpeedSlider    [Slider 0.5–2.0, 140×20 px]
             └── SpeedLabel     ["Speed", 10pt grey]
```

### ⚠️ Scale rule (important!)
The bubble **must NOT be parented to an NPC transform**.  
Reason: Unity WorldSpace canvas uses `localScale` to map canvas-pixels → world-units. If the NPC has `lossyScale ≠ (1,1,1)` (common on imported humanoid models in cm), the canvas inherits that scale and appears enormous.  
`FindObjectTask.SpawnBubble()` creates the bubble at world root — no `SetParent()`.

---

## 6. ARDialogueMode — Camera Transition

**File:** `Assets/Scripts/UI/ARDialogueMode.cs`  
Singleton (`ARDialogueMode.Instance`).

```
EnterDialogueMode(targetPos, targetRot, onReady):
  fade black → disable ARCameraBackground + TrackedPoseDriver
            → tween Camera.main to (targetPos, targetRot) over moveDuration
            → fade back in → onReady()

ExitDialogueMode(onDone):
  fade black → snap Camera.main back to _savedPose
            → re-enable ARCameraBackground + TrackedPoseDriver
            → fade back in → onDone()
```

Inspector fields: `fadeDuration` (0.35s), `moveDuration` (0.8s), `fadeColor` (black), `disableARBackground` (false for AR scene, true for pure-3D scene).

---

## 7. TaskMenuController — Button Grid

**File:** `Assets/Scripts/UI/TaskMenuController.cs`

6 task buttons in a 2-column GridLayoutGroup. Buttons are assigned in Inspector order: Find, Name, Story, Manipulate, Sort, Match.

- `Open()` → scale-in animation + show overlay
- `Close()` → scale-out + hide overlay, fires `OnMenuClosed`
- Button press → fires `OnTaskSelected(TaskType)` + calls `Close()`

`Lumier_MainController` subscribes to `OnTaskSelected` and routes to the correct sub-system.

---

## 8. Editor Tools

| Menu item | Script | Purpose |
|---|---|---|
| `Tools > Build Lumiere Task Menu` | `TaskMenuBuilder.cs` | Creates the full Canvas/LumiereMenuPanel/ButtonGrid in the active scene. Assign the resulting `LumiereMenuOverlay` to `Lumier_MainController.taskMenu`. |
| `Tools > Create Sample Tasks` | `SampleTaskCreator.cs` | Creates sample `TaskTemplateSO` assets for testing. |
| `Tools > Fix Scene Materials (URP)` | `FixSceneMaterials.cs` | Upgrades legacy Standard materials to URP Lit. |
| `Tools > Material to URP Upgrader` | `MaterialToURPUpgrader.cs` | Batch-upgrades material assets. |
| `Tools > Android Debug Setup` | `AndroidDebugSetup.cs` | Configures Build Settings for Android ADB debugging. |

---

## 9. Known Issues & Decisions

| Issue | Fix / Decision |
|---|---|
| SpeechBubbleUI appeared bigger than the screen | **Root cause:** bubble was parented to NPC with non-unit world scale. **Fix (May 2026):** `FindObjectTask.SpawnBubble()` no longer calls `SetParent()` — bubble lives at world root. Defaults also reduced to `0.55 × 0.30` world units. |
| NPC facing: camera always looks at the character's face | Uses vector from NPC to current Camera.main position (not `NPC.transform.forward`) to stay on the user's side. |
| ARDialogueMode approach | "Pin" approach (disable AR background + TrackedPoseDriver) was chosen over a full AR↔3D scene switch to avoid losing the AR anchor. |

---

## 10. Adding a New Task Type

1. Add the new enum value to `TaskType` in `TaskEnums.cs`.
2. Add a button (`Btn_<Name>`) to the `LumiereMenuPanel > ButtonGrid` (or re-run `Build Lumiere Task Menu`).
3. Assign the button in `TaskMenuController`'s Inspector and wire it in `WireButton()`.
4. Add a `case TaskType.<New>:` in `Lumier_MainController.HandleTaskSelected()`.
5. Create a new task script (follow `FindObjectTask.cs` as the pattern).
6. Update `TaskRandomizer.cs` if the new task should participate in random assignment.

---

## 11. TTS Integration

`TTSManager` (singleton, DontDestroyOnLoad) is the single entry point for all speech:
- `TTSManager.Manager.Speak(string text)` — synthesizes and plays audio
- `TTSManager.Manager.SetSpeechSpeed(float)` — clamps [0.25, 4.0], forwards to `OpenAITTSManager.SetSpeed()`
- `TTSManager.Manager.IsPlaying` — bool, true while audio is active

The speed slider in `SpeechBubbleUI` (range 0.5–2.0) calls `SetSpeechSpeed()` in real-time via `onValueChanged`.

---

## 12. Project Conventions

- All major UI panels are **code-built at runtime** (no prefab required). Each UI class has a `BuildHierarchy()` / `BuildCanvas()` private method that creates its children.
- **Inspector serialization** is used for all tunable parameters; defaults are chosen to be sensible but should be adjusted per scene scale.
- NPC movement components (`NPCWander`, `NPCRouteWander`, `NPCPatrol`) share a `Pause()` / `Resume()` API.
- AR-specific code is conditionally compiled with `#if UNITY_AR_FOUNDATION` / `#if UNITY_INPUT_SYSTEM`.
- Dialogue data is held in `FindObjectDialogueData` ScriptableObjects. If none is assigned, a built-in fallback list is used.
