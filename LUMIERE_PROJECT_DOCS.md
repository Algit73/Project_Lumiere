# Lumiere Android — Project Documentation
> Branch: `xr-android` | Repo: `Algit73/Project_Lumiere`
> Last updated: June 2026

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
| `SceneTransitionManager.cs` | DontDestroyOnLoad singleton. Transfers task type + AR anchor pose across scenes. Saves `Camera.main` pose before GoTo() so it can be restored on return. |
| `ARSceneAnchorController.cs` | Placed on the AR content root in `XR_Home`. Saves/restores the placed-object world pose when returning from a task scene. Also restores `Camera.main` pose via `SceneTransitionManager.SavedCameraPose`. |
| `LumiereSceneDirector.cs` | Central scene director. Reads `SceneSetupConfigSO` and applies scene setup on `Start()`. Handles NPC icon visibility (chance-based for demo; API-driven in future). Place one instance in every scene. |

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
| `FindObjectTask.cs` | Orchestrates the full Find-Object sequence (see §4): multi-line AI story → guessing phase → correct/wrong/hint handling → exit. Freezes/hides other NPCs while one is selected; restores them on exit. Wires `SpeechBubbleUI.OnExitClicked`/`OnHintClicked`. |
| `CharacterAgent.cs` | MonoBehaviour on every NPC. Exposes `characterID`, `eligibleTaskTypes`, `iconAnchor`, `CurrentTask`. Controls optional `NPCFloatingIcon` — `ShowIcon(TaskType)` / `HideIcon()` delegate to it; silently no-op when component absent. |
| `NPCFloatingIcon.cs` | Floating icon above NPC head. Supports a 3D model prefab (e.g. GLB) or a text-badge fallback. Starts hidden; `CharacterAgent` controls visibility. Billboard uses Y-axis-only rotation. Head position auto-detected via humanoid head bone → renderer bounds → collider fallback. |
| `CharacterProfileSO.cs` / `CharacterProfile` | Unified NPC registry ScriptableObject. Each entry: `characterID` (must match `CharacterAgent.characterID`), `displayName`, `bio` (personality/description fed to the AI prompt), `lastUpdate` (AI-written narrative continuity, refreshed after each session). Create via `Tools > Lumiere > Create Character Registry Asset` (see §8). |
| `FindObjectPrewarm.cs` | Static cache. Pre-picks a random kitchen item + character profile in `XR_Home` (`Lumier_MainController.Start()`) so they're ready the instant "Find Object" is tapped. The OpenAI story call itself cannot fire this early — it needs the item's live `Interactive.Meta["description"]`, which only exists once Farm's GameObjects are loaded — so that call fires at the top of `FindObjectTask.Begin()` instead. |
| `FindObjectTapDetector.cs` | Raycasts from `Camera.main` on the `"Clickable"` layer on tap/click; raises `OnItemSelected(Interactive)`. Only listens during the post-story "guessing" phase. Kept separate from `CursorHandler` (wired to the unrelated card-game flow). Ray-source lookup is isolated so a future XR build can swap in an XR ray interactor. |
| `FillerLineBankSO.cs` / `FillerEntry` | ScriptableObject bank of hardcoded filler lines **with pre-recorded `AudioClip`s** (not live TTS) shown/played instantly on a wrong tap or hint request, to mask OpenAI latency. Create + populate via the `Tools > Lumiere > … Filler Line Bank …` menu items and `generate_filler_audio.py` (see §8). |
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
| `SpeechBubbleUI.cs` | **World-space** speech bubble above NPC. Auto-builds Canvas hierarchy at runtime. Contains: speaker label, dialogue text, 🔊 replay button, speech-speed slider (0.5–2.0×), ✕ exit button (top-right). Fires `OnExitClicked` action. |
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
Lumier_MainController.Start()
  • FindObjectPrewarm.Execute(findGameRegistry) — pre-picks a random kitchen item + character profile
        ↓ (user is still on the main screen)
[User taps Lumiere Button → selects "Find"]
        ↓
Lumier_MainController.HandleTaskSelected(TaskType.Find)
  • Saves AR anchor pose
  • SceneTransitionManager.GoTo("Farm", TaskType.Find, "XR_Home", anchorPose)
        ↓
[Farm scene loads]
FindObjectTask.Start()  →  ConsumePendingTask → Begin()
        ↓
1. Consume FindObjectPrewarm cache (item + character) or pick fresh if unavailable
2. FindNPCByCharacterID(profile.characterID) — prefer the NPC matching the pre-warmed profile,
   fall back to PickRandomNPC()
3. GetItemDescriptionFromScene(item.Key)     — reads Interactive.Meta["description"] from the
   live GameObject (only possible now that Farm's items exist)
4. BuildInitialMessages(item, profile, description) → fire SendAsync() (OpenAI) — runs
   concurrently with the camera tween below to hide the round-trip latency
5. PauseNPC()               — disables NPCWander/NPCRouteWander/NPCPatrol
6. ComputeCameraPose()      — frames NPC upper half at 60% screen fill
7. ARDialogueMode.EnterDialogueMode(camPos, camRot, async callback)
   ├── Fades to black
   ├── Disables ARCameraBackground + TrackedPoseDriver
   ├── Tweens camera to framing pose
   └── Fades back in → awaits the story task → fires callback
        ↓ (in callback)
8. ParseLinesResponse(json) → story.Lines become DialogueLines (speakerName = character's displayName)
9. SpawnBubble()            — creates SpeechBubbleUI at world-space anchor above NPC head
10. DialogueNavigationUI.Show()  — shows ◀ ▶ arrows to page through the story lines
11. ShowLine(0)              — displays first DialogueLine, triggers TTS
        ↓
[User taps ◀ / ▶ through the story]
  OnPrev / OnNext → ShowLine(index) → SpeechBubbleUI.Show(line) → TTSManager.Speak()
        ↓
[User taps ▶ on the LAST story line]
EnterGuessingPhase()
  • Hides ◀ ▶ nav arrows
  • FindObjectTapDetector.Listening = true
        ↓
[User taps a scene object]
OnItemTapped(Interactive) — compares tapped.gameObject.name to TargetItemKey
        │
        ├── CORRECT → HandleCorrectTap()
        │     • Stops listening for further taps, hides hint button
        │     • RunFollowupAsync(BuildSuccessMessage(...)) → plays celebration lines → FinishSession()
        │
        └── WRONG → HandleWrongTap(wrongKey)
              • wrongAttempts++; instantly shows a random FillerLineBankSO wrong-guess filler
                (pre-recorded audio, no API wait)
              • If wrongAttempts >= wrongAttemptsForHint (default 3) → SpeechBubbleUI.ShowHintButton()
                (glowing 💡 button via SinusoidalGlowEffect)
              • RunFollowupAsync(BuildWrongGuessMessage(...)) → plays the AI's in-character,
                encouraging spatial nudge once it arrives (PlaySequentialLines, auto-advances
                using TTSManager.Manager.IsPlaying)
        ↓
[User taps the 💡 hint button, any time after it appears]
OnHintRequested() → hintLevel++ → instant hint filler → RunFollowupAsync(BuildHintMessage(...))
  (escalates specificity each call: vaguer → mentions a neighbor → very direct)
        ↓
FinishSession()
  • Hides bubble + nav, stops sequential-lines coroutine, disables tap detector
  • ResumeNPC()
  • ARDialogueMode.ExitDialogueMode() → fade + re-enable AR + fade
  • SceneTransitionManager.ReturnHome() → loads XR_Home
```

All OpenAI turns (story, wrong-guess, hint, success) share ONE running `List<Message> _conversation` —
appended to and re-sent via `OpenAIBasics.ContinueChatAsync()` — so the model always remembers the
original story, item, and character instead of treating each turn as a stateless new prompt.

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
| `Tools > Lumiere > Create Character Registry Asset` | `CharacterRegistryTools.cs` | Creates `Assets/ScriptableObjects/CharacterRegistry.asset` (a `CharacterProfileSO`), auto-populated with one entry per `CharacterAgent.characterID` found in the open scene. Fill in `displayName`/`bio` per entry, then assign the asset to `Lumier_MainController.findGameRegistry` and `FindObjectTask.characterRegistry`. |
| `Tools > Lumiere > Refresh Character Registry From Scene` | `CharacterRegistryTools.cs` | Adds any newly-found `characterID`s to the existing registry without touching existing entries. |
| `Tools > Lumiere > Create Filler Line Bank Asset` | `FillerLineBankTools.cs` | Creates `Assets/ScriptableObjects/FillerLineBank.asset` (a `FillerLineBankSO`) pre-populated with predefined wrong-guess/hint filler texts (audio clips not yet assigned). Assign the asset to `FindObjectTask.fillerBank`. |
| `Tools > Lumiere > Export Filler Texts To JSON` | `FillerLineBankTools.cs` | Writes `Assets/Scripts/OpenAI/Tools/filler_lines.json` so the offline Python generator narrates the exact same text shown in the bubble. |
| `Tools > Lumiere > Auto-Assign Filler Audio Clips` | `FillerLineBankTools.cs` | After running `generate_filler_audio.py`, matches the generated `.mp3` files (by filename order) back onto the `FillerLineBankSO` entries. |

### Filler audio generation pipeline (offline, Python)

`Assets/Scripts/OpenAI/Tools/generate_filler_audio.py` synthesizes the filler-line audio via the
OpenAI TTS API (reuses the same `OPENAI_KEY.txt` as `OpenAIBasics.cs`). Not part of the Unity
runtime — run manually whenever filler texts change:

```
1. Unity:   Tools > Lumiere > Create Filler Line Bank Asset   (first time only)
2. Unity:   Tools > Lumiere > Export Filler Texts To JSON
3. Shell:   pip install requests
            python Assets/Scripts/OpenAI/Tools/generate_filler_audio.py
            → writes Assets/Audio/Fillers/Wrong/wrong_NN.mp3, Assets/Audio/Fillers/Hint/hint_NN.mp3
4. Unity:   Tools > Lumiere > Auto-Assign Filler Audio Clips
```

---

## 9. Known Issues & Decisions

| Issue | Fix / Decision |
|---|---|
| SpeechBubbleUI appeared bigger than the screen | **Root cause:** bubble was parented to NPC with non-unit world scale. **Fix (May 2026):** `FindObjectTask.SpawnBubble()` no longer calls `SetParent()` — bubble lives at world root. Defaults also reduced to `0.55 × 0.30` world units. |
| NPC facing: camera always looks at the character's face | Uses vector from NPC to current Camera.main position (not `NPC.transform.forward`) to stay on the user's side. |
| ARDialogueMode approach | "Pin" approach (disable AR background + TrackedPoseDriver) was chosen over a full AR↔3D scene switch to avoid losing the AR anchor. |
| NPCFloatingIcon at character feet | Root pivot of imported humanoid models is often at mid-torso or feet. Fixed by reading `Animator.GetBoneTransform(HumanBodyBones.Head)` as primary position source. |
| NPCFloatingIcon wrong size (GLB) | `localScale = Vector3.one` was stripping GLB import scale factor (e.g. 0.01). Fixed: `modelScale` on root parent is the only size multiplier; import scale is preserved by keeping child localScale = (1,1,1). |
| NPCFloatingIcon rotation (corner pointing up) | GLB export bakes a rotation on the prefab root. Fixed by preserving `modelPrefab.transform.localRotation` and only adding `modelRotationOffset` on top. Billboard changed to Y-axis-only yaw so X rotation is always 0. |

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

---

## 13. LumiereSceneDirector — Scene Setup

**Files:**
- `Assets/Scripts/LumiereSceneDirector.cs` — MonoBehaviour
- `Assets/Scripts/SceneSetupConfigSO.cs` — ScriptableObject data
- Asset instance: `Assets/ScriptableObjects/` (create via `Assets › Create › Lumiere › Scene Setup Config`)

### Purpose
`LumiereSceneDirector` is the single entry point for preparing any scene at runtime. It reads `SceneSetupConfigSO` and applies the configuration on `Start()`. The same director and the same SO asset are placed in every scene (XR_Home, Farm, etc.) for a consistent architecture.

### Current behaviour (demo)
On `Start()`, the director iterates every `CharacterAgent` in the scene and independently rolls `Random.value <= iconShowChance` for each. NPCs that pass the roll call `ShowIcon(TaskType.Find)`; others call `HideIcon()`.

### SceneSetupConfigSO fields
| Field | Type | Default | Notes |
|---|---|---|---|
| `iconShowChance` | float [0–1] | `0.5` | Probability per NPC of showing a floating icon |

### Re-rolling during demo
Right-click `LumiereSceneDirector` in the Inspector → **Refresh Icons** to re-roll all NPCs without restarting Play mode.

### Future (API integration)
When API integration is ready, replace `RollNPCIcons()` with a method that reads server data. The `SceneSetupConfigSO` will grow to include prop placements, forced icon NPCs, event tags, and task assignments. The director API (`SetupScene()`) stays the same — callers don't need to change.

---

## 14. NPCFloatingIcon — Floating Icon Component

**File:** `Assets/Scripts/Tasks/NPCFloatingIcon.cs`

### Purpose
Renders a floating icon above an NPC's head. Controlled entirely by `CharacterAgent` — starts hidden and never shows itself.

### Setup
1. Add `NPCFloatingIcon` to the NPC GameObject (alongside `CharacterAgent`).
2. Assign a GLB prefab to **Model Prefab** (optional — text badge used if empty).
3. `CharacterAgent.ShowIcon()` / `HideIcon()` control visibility.
4. `LumiereSceneDirector` calls those methods during scene setup.

### Key Inspector fields
| Field | Default | Notes |
|---|---|---|
| `modelPrefab` | none | 3D prefab (e.g. `yellow_question_box.glb`). Overrides text badge. |
| `modelScale` | `0.25` | World-space size in meters. Live — changes take effect immediately in Play mode. |
| `modelRotationOffset` | `(0,0,0)` | Euler offset added on top of the GLB's baked-in rotation. |
| `iconType` | Random | Text badge type when no model prefab. |
| `verticalOffset` | `0.4` | World-units above the detected head position. |
| `bobEnabled` | true | Gentle up/down sine animation. |
| `startVisible` | false | Set true only for isolated testing. |

### Head position detection (priority order)
1. `Animator.GetBoneTransform(HumanBodyBones.Head).position.y + 0.15 m` — most accurate, immune to collider fit
2. Union of all `Renderer.bounds.max.y` — works for non-humanoid meshes
3. `Collider.bounds.max.y` — fallback
4. `transform.position.y + 1.8 m` — last resort

### Billboard behaviour
Rotates around Y only (`toCamera.y = 0` before `LookRotation`) — X rotation is always 0.

### ⚠️ Lifecycle rule (important!)
All initialization (`BuildModel`/`BuildCanvas`, `_iconRoot` hide) runs in **`Awake()`**, not `Start()`. This guarantees `_iconRoot` is ready before `LumiereSceneDirector.Start()` calls `ShowIcon()`. Similarly, `CharacterAgent` caches `_floatingIcon` in `Awake()`. Do not move these back to `Start()` — the scene director will silently fail.
