# Changelog

All notable changes to this project should be documented in this file.

Format:
- New entries go at the top.
- Each release or working milestone should include `Added`, `Changed`, `Fixed`, and `Notes` when relevant.
- Dates use `YYYY-MM-DD`.

## [2026-08-04] Fix Build Errors + Filler Bank Content + Character Registry Asset Tooling

### Fixed

- **`FindObjectTask.cs` compile errors**: missing `using OpenAI;` meant `Role` (used in `new Message(Role.Assistant, json)`) could not be resolved. Added the import.
- **`SpeechBubbleUI.cs` missing members**: an earlier edit that added `ShowFiller()`, `ShowHintButton()`, `HideHintButton()`, and the TTS-skip flag on `Show()` silently failed to apply (a prior multi-replace reported success without actually writing the change). Re-applied correctly — `FindObjectTask` now compiles against real methods instead of ones that only existed in comments/changelog text.

### Added

- **`Assets/Scripts/Editor/CharacterRegistryTools.cs`**: `Tools > Lumiere > Create Character Registry Asset` creates `Assets/ScriptableObjects/CharacterRegistry.asset` (the actual `CharacterProfileSO` instance — previously only the C# class existed, no asset had been created). Auto-populates one entry per `CharacterAgent.characterID` found in the currently open scene (`displayName`/`bio`/`lastUpdate` left blank for you to fill in). `Tools > Lumiere > Refresh Character Registry From Scene` adds newly-found characterIDs without touching existing entries. ScriptableObject assets can't be safely hand-authored as raw `.asset` YAML (they need a Unity-assigned script GUID), so this is implemented as a proper editor tool rather than a generated file.
- **`Assets/Scripts/Editor/FillerLineBankTools.cs`**: `Tools > Lumiere > Create Filler Line Bank Asset` creates `Assets/ScriptableObjects/FillerLineBank.asset` pre-populated with 16 wrong-guess and 10 hint-request filler texts (written to be warm/patient and non-repetitive — no audio yet). `Tools > Lumiere > Export Filler Texts To JSON` writes `Assets/Scripts/OpenAI/Tools/filler_lines.json` so narration always matches the displayed text. `Tools > Lumiere > Auto-Assign Filler Audio Clips` matches generated `.mp3` files back onto the bank entries by filename order.
- **`Assets/Scripts/OpenAI/Tools/generate_filler_audio.py`**: offline (non-runtime) Python script that synthesizes the filler audio clips via the OpenAI TTS API (`tts-1` / voice `alloy`), reusing the existing `OPENAI_KEY.txt`. Reads `filler_lines.json`, writes `Assets/Audio/Fillers/Wrong/wrong_NN.mp3` and `Assets/Audio/Fillers/Hint/hint_NN.mp3`. Requires `pip install requests`.

### Notes

- Full filler-audio pipeline: (1) `Create Filler Line Bank Asset` → (2) `Export Filler Texts To JSON` → (3) run `generate_filler_audio.py` → (4) `Auto-Assign Filler Audio Clips`.
- After creating the registry and filler bank assets, assign them: `Lumier_MainController.findGameRegistry` + `FindObjectTask.characterRegistry` → `CharacterRegistry.asset`; `FindObjectTask.fillerBank` → `FillerLineBank.asset`.
- `LUMIERE_PROJECT_DOCS.md` updated: §3.3 file table now lists `CharacterProfileSO`, `FindObjectPrewarm`, `FindObjectTapDetector`, `FillerLineBankSO`; §4 flow diagram rewritten for the multi-line story → guessing phase → correct/wrong/hint conversation; §8 documents the new editor tools + Python audio pipeline.

## [2026-08-04] Find Object — Multi-Line Story, Tap Detection, Wrong-Guess/Hint Conversation

### Added

- **`FindObjectTapDetector`** (`Assets/Scripts/Tasks/FindObjectTapDetector.cs`): new lightweight component that raises `event Action<Interactive> OnItemSelected` when the player taps a scene object during the "guessing" phase. Deliberately separate from `CursorHandler` (which is wired to the card-game flow — wrong semantics here). Raycasts from `Camera.main` on the `"Clickable"` layer on mouse-down/touch-began. `Listening` can be toggled without destroying the component. Ray-source lookup (`TryGetPointerRay`) is isolated so a future XR build can swap in an XR ray interactor's ray or forward `XRBaseInteractable.selectEntered` through the same event contract.
- **`FillerLineBankSO` / `FillerEntry`** (`Assets/Scripts/Tasks/FillerLineBankSO.cs`): ScriptableObject holding hardcoded filler lines **with pre-recorded/baked `AudioClip`s** (not live TTS) — played instantly on a wrong tap or hint request to mask the OpenAI round-trip latency. Two lists: `wrongGuessFillers`, `hintRequestFillers`. Create via **Assets › Create › Lumiere › Filler Line Bank**.
- **`SpeechBubbleUI` hint button**: new glowing "💡" button (top-left corner, gold, uses `SinusoidalGlowEffect` for an eye-catching idle pulse) — hidden by default. `ShowHintButton()` / `HideHintButton()` control visibility; `OnHintClicked` event fires on tap.
- **`SpeechBubbleUI.ShowFiller(FillerEntry)`**: shows filler text instantly and plays its `AudioClip` via a dedicated `AudioSource` — bypasses the live TTS pipeline entirely (no latency).
- **`OpenAIBasics.ContinueChatAsync(List<Message>)`**: sends a full multi-turn conversation (system + prior assistant/user turns) and returns the assistant's reply — powers the new conversation-based Find-Object flow instead of stateless one-shot prompts.

### Changed

- **`KitchenFindGame`** (`Assets/Scripts/OpenAI/KitchenFindGame.cs`) — replaced the single-line `ClueResponse`/`BuildPrompt`/`RequestClue` API with a conversation-based contract:
  - `ClueLinesResponse { List<string> Lines, string CharacterLastUpdate }` — response JSON is now `{"lines":[...],"character_last_update":"..."}`. We never ask the model to echo `item_name`/`item_position`/`character_name` back — we already know them locally.
  - `GetItemDescriptionFromScene(key)` — new: looks up `Interactive.Meta["description"]` from the live scene GameObject matching a KitchenItems key. Only works once the Farm scene is loaded.
  - `BuildInitialMessages(item, character, itemDescription)` — seeds the conversation. **Only known facts are sent as input** (character bio, item location/neighbors, item's scene description) — the AI itself invents the character's reason for wanting the item as part of the story; no "reason" field is ever sent by us.
  - `BuildWrongGuessMessage(targetItem, wrongKey)` — appends a user turn describing the wrong tap (using the wrong item's own known location) so the model can craft an in-character, encouraging spatial nudge without a new stateless prompt.
  - `BuildHintMessage(targetItem, hintLevel)` — escalating hint request (level 1 = vaguer, 2 = mentions a neighbor, 3+ = very direct).
  - `BuildSuccessMessage(targetItem)` — follow-up turn for a correct tap; the model may now say the item's name.
  - `ParseLinesResponse(json)` / `SendAsync(conversation)` replace `ParseResponse`/`RequestClue`.
  - `DescribeLocation()` / `DescribeNeighbors()` — extracted as reusable static helpers.

- **`FindObjectTask`** (`Assets/Scripts/Tasks/FindObjectTask.cs`) — full flow rework:
  - Maintains one running `List<Message> _conversation` per session instead of stateless prompts.
  - `Begin()` now builds the initial story conversation (including the live item description from the Farm scene) and fires it concurrently with the camera tween, same latency-hiding pattern as before — just moved one scene later since the description requires the Farm scene's GameObjects.
  - The story response's `Lines` feed the existing multi-line paging (`◀ ▶` arrows) + auto-TTS-per-line system — no UI rework needed for the story itself.
  - On the last story line, `OnNext()` now calls `EnterGuessingPhase()` (hides nav arrows, enables `FindObjectTapDetector.Listening`) instead of ending the session.
  - `OnItemTapped(Interactive)` routes to `HandleCorrectTap()` or `HandleWrongTap(wrongKey)` by comparing `tapped.gameObject.name` to `TargetItemKey`.
  - **Wrong tap**: increments `_wrongAttempts`, instantly shows a random filler (`fillerBank.GetRandomWrongGuessFiller()`) via `SpeechBubbleUI.ShowFiller()`, then appends `BuildWrongGuessMessage()` to the conversation and plays the AI's real reaction once it arrives (`RunFollowupAsync` → `PlaySequentialLines`). Shows the glowing hint button after `wrongAttemptsForHint` (Inspector field, default 3) wrong taps.
  - **Hint request**: `OnHintRequested()` (wired to `SpeechBubbleUI.OnHintClicked`) increments `_hintLevel`, shows a hint filler instantly, then appends an escalating `BuildHintMessage()` turn.
  - **Correct tap**: stops listening for further taps, hides the hint button, appends `BuildSuccessMessage()`, plays the AI's celebration lines, then calls `FinishSession()`.
  - `PlaySequentialLines()` / `SequentialLinesRoutine()` — new coroutine that shows each follow-up line one at a time in the bubble, auto-advancing once `TTSManager.Manager.IsPlaying` goes false (no manual arrow presses needed for these short reactive bursts).
  - `EnsureTapDetector()` — lazily adds `FindObjectTapDetector` alongside `ARDialogueMode`/`DialogueNavigationUI`; starts with `Listening = false`.
  - New Inspector fields: `fillerBank` (`FillerLineBankSO`), `wrongAttemptsForHint` (int, default 3).
  - `FinishSession()` now also stops the sequential-lines coroutine, disables the tap detector, and clears `_conversation`/`_wrongAttempts`/`_hintLevel`.

### Notes

- `CharacterProfile.bio` (already existed) doubles as "the line of description for the NPC" requested — no new field was needed.
- Multi-item tasks (e.g. gathering several ingredients for a recipe) are intentionally out of scope for this iteration — planned as a **separate future task type** built on top of this one, since it requires scanning/tracking multiple scene items instead of a single random pick.
- `FillerLineBankSO` entries need real `AudioClip` assets assigned manually (recorded or otherwise) — the bank ships empty; `GetRandomWrongGuessFiller()`/`GetRandomHintFiller()` return `null` gracefully if empty, and `HandleWrongTap`/`OnHintRequested` simply skip the instant filler in that case.
- The wrong-tap/hint prompts intentionally instruct the model to stay warm and patient — never sound frustrated or judgmental — per aphasia-friendly design requirements.

## [2026-07-16] Find Object Pre-warm — Item + Clue Ready Before User Taps

### Added

- **`FindObjectPrewarm`** (`Assets/Scripts/Tasks/FindObjectPrewarm.cs`): static cache that pre-warms the Find-Object session at scene startup.
  - `Execute(CharacterProfileSO registry)` — picks a random `CharacterProfile` from the registry, picks a random `KitchenGameItem`, fires `KitchenFindGame.RequestClue()` in the background. Safe to call multiple times (each call replaces the previous cache).
  - `IsReady` — true when both an item and an in-flight API task are cached.
  - `Clear()` — consumed by `FindObjectTask.Begin()` after the data is taken.

### Changed

- **`Lumier_MainController`** (`Assets/Scripts/Lumier_MainController.cs`):
  - Added `[SerializeField] CharacterProfileSO findGameRegistry` Inspector field.
  - `Start()` now calls `FindObjectPrewarm.Execute(findGameRegistry)` immediately, so the API call is already in-flight while the user is on the main screen.

- **`FindObjectTask.Begin()`** (`Assets/Scripts/Tasks/FindObjectTask.cs`):
  - **Pre-warm path** (normal): when `FindObjectPrewarm.IsReady`, consumes the cache — uses the pre-picked item, profile, and already-running API task. Calls `FindNPCByCharacterID()` to prefer the NPC that matches the pre-warmed profile; falls back to any random eligible NPC.
  - **Cold path** (fallback): if pre-warm is not available, picks fresh (same behaviour as before).
  - Added `FindNPCByCharacterID(string id)` private helper — finds an active `CharacterAgent` in the scene by `characterID` (case-insensitive).

### Notes

- `findGameRegistry` in `Lumier_MainController` should be the same asset assigned to `FindObjectTask.characterRegistry`.
- When `SceneTransitionManager` reloads XR_Home on return from Farm, `Start()` fires again and a new pre-warm is triggered automatically.

## [2026-07-16] Character Registry + Expanded ClueResponse JSON

### Added

- **`CharacterProfileSO`** (`Assets/Scripts/Tasks/CharacterProfileSO.cs`): new ScriptableObject acting as the unified registry of all NPC characters.
  - Each `CharacterProfile` entry has: `characterID` (link key matching `CharacterAgent.characterID`), `displayName`, `bio` (brief personality context fed to the AI), and `lastUpdate` (AI-generated narrative state updated after each session).
  - `GetProfile(string characterID)` — looks up a profile by ID (case-insensitive).
  - `SetLastUpdate(string characterID, string value)` — writes the AI's narrative update back in-memory during Play.
  - Create via **Assets › Create › Lumiere › Character Registry**.

### Changed

- **`KitchenFindGame.ClueResponse`** (`Assets/Scripts/OpenAI/KitchenFindGame.cs`): expanded from 2 fields to 5 to match the new JSON contract:

  | JSON key | C# property | Source |
  |---|---|---|
  | `item_name` | `ItemName` | model echoes item |
  | `item_position` | `ItemPosition` | model echoes location |
  | `character_name` | `CharacterName` | model echoes character |
  | `character_intro` | `CharacterIntro` | model generates clue in character's voice |
  | `character_last_update` | `CharacterLastUpdate` | model generates narrative update |
  | *(local only)* | `ItemKey` | always set from our own pick |

- **`KitchenFindGame.BuildPrompt()`**: now accepts an optional `CharacterProfile` parameter. When provided, injects character name, bio, and last known situation into the prompt so the model can write the clue in the character's voice. Falls back to a generic "Lumiere" persona when `null`.
- **`KitchenFindGame.RequestClue()`** / **`ParseResponse()`**: both accept the optional `CharacterProfile` and propagate it to fallback values.
- **`FindObjectTask`** (`Assets/Scripts/Tasks/FindObjectTask.cs`):
  - Added `[SerializeField] CharacterProfileSO characterRegistry` Inspector field (assign the registry asset in the scene).
  - After picking the NPC, resolves `CharacterProfile` via `characterRegistry.GetProfile(_chosenAgent.characterID)` and stores it as `CurrentProfile`.
  - Passes `CurrentProfile` to `RequestClue()` and `ParseResponse()`.
  - After a successful API response, writes `CharacterLastUpdate` back to the registry via `SetLastUpdate()` so the next session sees the updated narrative state.
  - Speech bubble `speakerName` now comes from `clue.CharacterName` (falls back to profile display name, then "Lumiere").
  - `CurrentProfile` is exposed as a public read-only property and cleared in `FinishSession()`.

### Notes

- `characterRegistry` is optional — if left unassigned, all sessions fall back to the generic "Lumiere" persona with no narrative continuity.
- `CharacterProfile.lastUpdate` changes are in-memory only during Play; persist them (e.g. to JSON / PlayerPrefs) when that feature is needed.

## [2026-07-13] FindObjectTask — Kitchen Item Selection + OpenAI Clue Integration

### Changed

- **`FindObjectTask`** (`Assets/Scripts/Tasks/FindObjectTask.cs`): integrated `KitchenFindGame` into the existing Find Object flow.
  - `Begin()` now immediately calls `KitchenFindGame.PickRandomItem()` and fires `RequestClue()` concurrently with the AR camera tween, minimising perceived wait time.
  - The `EnterDialogueMode` callback is now `async` — it awaits the in-flight API task, parses the JSON response, and replaces the dialogue lines with the AI-generated clue before the bubble appears.
  - If the API call fails, the bubble falls back to a "Let me think…" placeholder line (no crash).
  - Exposes `CurrentGameItem` (`KitchenFindGame.KitchenGameItem`) and `TargetItemKey` (`string`) as public read-only properties for the tap-verification step (next phase).
  - `FinishSession()` now also clears `CurrentGameItem` and `TargetItemKey`.
  - Added `BuildLoadingLines()` fallback helper (single placeholder line).
  - Added `using System.Threading.Tasks` and `using Newtonsoft.Json`.

- **`KitchenFindGame`** (`Assets/Scripts/OpenAI/KitchenFindGame.cs`):
  - `BuildPrompt()` updated to explicitly instruct the model to return `{"description":"…","item":"…"}` JSON only — no extra commentary.
  - Added `ClueResponse` class (`Description`, `Item`, `ItemKey` properties) with `[JsonProperty]` annotations.
  - Added `static ParseResponse(string json, KitchenGameItem item)` — deserialises the JSON, strips markdown code-fences if present, falls back to raw text on parse error, and always stamps `ItemKey` from the authoritative local pick.

### Notes

- Tap/click verification (checking `TargetItemKey` against the tapped object) is not yet implemented — `CurrentGameItem` and `TargetItemKey` are exposed and ready for that next step.

## [2026-07-13] KitchenFindGame — Random Item Picker + OpenAI Clue Generator

### Added

- **`KitchenFindGame`** (`Assets/Scripts/OpenAI/KitchenFindGame.cs`): self-contained class that implements the core mechanism for a "Find the Object" kitchen mini-game.
  - `PickRandomItem()` — randomly selects an item from the full kitchen map and returns a `KitchenGameItem` with its display name and resolved `ItemLocation` (section, subsection, row, position, all row-neighbors).
  - `BuildPrompt(item)` — composes an OpenAI system prompt that describes the item's kitchen area and same-row neighbors **without revealing the item name**, instructing Lumiere to generate a 2–3 sentence clue suitable for aphasia therapy.
  - `RequestClue(item)` — calls `OpenAIBasics.llm_do_task()` with the built prompt and returns the API response string.
  - Static `_map` — encodes the full kitchen layout from `house_items.txt` (Fridge/Freezer, Level-1 through Level-3, Stove top, Kitchen bar, Kitchen table) as `Dictionary<string, ItemLocation>`; only mapped items can be selected, ensuring every picked item has valid neighbor context.

### Notes

- `KitchenFindGame` has no MonoBehaviour dependency; it can be instantiated from any script.
- The click/selection verification step (player taps the correct object) is not yet implemented — `RequestClue` only prepares the clue to display.
- Next step: wire `KitchenFindGame` into the existing `Finding_Items` / `FindObjectTask` flow and add the tap-verification handler.

## [2026-07-13] NPCFloatingIcon Glow Fix — Remove GlowEffect Dependency

### Fixed

- **Icon root GameObject visible at `(0,0,0)`**: `GlowEffect.Activate()` was called during `BuildModel()` (at `Awake()` time) and immediately accessed `glowMaterial.renderQueue` — but `glowMaterial` is only initialized in `GlowEffect.Start()`, which had not run yet. The resulting `NullReferenceException` aborted `BuildModel()` before `_iconRoot = root.transform`, so `_iconRoot` stayed `null`, the icon root was never hidden, and `LateUpdate()` returned immediately every frame due to the null check.

### Changed

- **`NPCFloatingIcon`** (`Assets/Scripts/Tasks/NPCFloatingIcon.cs`): removed dependency on `GlowEffect` component entirely. `GlowEffect` used `GetComponent<Renderer>()` on the GLB root, which is always `null` for a multi-child GLB hierarchy. Glow is now driven directly: `BuildModel()` collects all material instances from child renderers via `GetComponentsInChildren`, enables `_EMISSION` on each, and caches them in `_glowMaterials`. `LateUpdate()` calls `mat.SetColor("_EmissionColor", ...)` on those cached instances — identical to what `GlowEffect` did internally, but without any `Start()` timing dependency and correctly applied across all child renderers.
- Removed now-unused `glowMaterialIndex` field from `NPCFloatingIcon`.

### Notes

- The `GlowEffect` component is no longer added to or referenced by `NPCFloatingIcon`. Any scene objects that previously relied on `GlowEffect` for icon glow should be verified.

## [2026-06-07] Execution-Order Fix for NPCFloatingIcon + CharacterAgent

### Fixed

- **`NPCFloatingIcon` icons not showing via `LumiereSceneDirector`**: icon geometry was built in `Start()`, so `_iconRoot` was still `null` when `LumiereSceneDirector.Start()` called `ShowIcon()` — Unity does not guarantee `Start()` order across objects. Fixed by moving all initialization (`BuildModel` / `BuildCanvas`, `_iconRoot` hide) from `Start()` to `Awake()`. All `Awake()` calls complete before any `Start()`, so `_iconRoot` is always ready.
- **`CharacterAgent._floatingIcon` was null during `LumiereSceneDirector.Start()`**: same race — `_floatingIcon = GetComponent<NPCFloatingIcon>()` was in `Start()`. Moved to `Awake()` so the reference is cached before the director iterates agents.

### Notes

- The guaranteed Unity lifecycle order is now: `Awake()` on all objects → `Start()` on all objects. Both `NPCFloatingIcon` and `CharacterAgent` initialise in `Awake()`; `LumiereSceneDirector` calls `ShowIcon()` / `HideIcon()` in `Start()` — order is deterministic.



### Added

- **`LumiereSceneDirector`** (`Assets/Scripts/LumiereSceneDirector.cs`): central scene-setup controller. Reads `SceneSetupConfigSO` on `Start()` and rolls per-NPC icon visibility. Works identically in all scenes (XR_Home, Farm, …). Exposes `SetupScene()` and `RefreshIcons()` (also available via Inspector context-menu for demo re-rolls).
- **`SceneSetupConfigSO`** (`Assets/Scripts/SceneSetupConfigSO.cs`): ScriptableObject holding scene configuration. Current field: `iconShowChance [0–1]`. Stub comments mark where API-driven fields will be added. Create via `Assets › Create › Lumiere › Scene Setup Config`.
- **`NPCFloatingIcon`** (`Assets/Scripts/Tasks/NPCFloatingIcon.cs`): floating 3D model or text-badge icon above NPC heads. Features: GLB model prefab support, Y-axis-only billboard, bob animation, live `modelScale` adjustment, `modelRotationOffset` for per-model orientation correction, humanoid head-bone position detection.

### Changed

- **`CharacterAgent`**: now caches an optional `NPCFloatingIcon` reference (`GetComponent` in `Awake()`). `ShowIcon(TaskType)` routes through it when the component is present; silently no-ops otherwise. Legacy `iconPrefab` spawn path retained as fallback. `HideIcon()` also delegates to `NPCFloatingIcon.HideIcon()`.
- **`NPCFloatingIcon`**: starts hidden by default — `CharacterAgent` (or `LumiereSceneDirector`) controls visibility. `startVisible` Inspector toggle available for isolated testing only.

### Fixed

- **NPCFloatingIcon position at feet**: root pivot of imported humanoid models is often at mid-torso. Fixed by using `Animator.GetBoneTransform(HumanBodyBones.Head)` as the primary head-Y source, falling back to renderer bounds → collider → +1.8 m estimate.
- **NPCFloatingIcon invisible after scale fix**: preserving the GLB import scale (`localScale` left as-is) made cm-scale models ~1 cm tall (invisible). Fixed: `instance.localScale = Vector3.one` normalises import scale; `modelScale` on the root parent is the only world-size multiplier.
- **NPCFloatingIcon corner pointing up**: `instance.localRotation = Quaternion.identity` was overriding the GLB's baked-in orientation fix. Fixed by setting `localRotation = modelPrefab.localRotation * Quaternion.Euler(modelRotationOffset)`.
- **NPCFloatingIcon wrong facing direction**: `_iconRoot.forward = Camera.main.transform.forward` made the icon face away from the camera. Fixed with Y-axis-only billboard (`toCamera.y = 0` before `LookRotation`), enforcing X-rotation = 0 at all times.
- **`modelScale` not updating at runtime**: scale was set only in `Start()`. Fixed by applying `_iconRoot.localScale = Vector3.one * modelScale` every `LateUpdate()`.

### Notes

- `LumiereSceneDirector` is intentionally simple for the demo. When API integration is added, replace `RollNPCIcons()` with a handler that reads server data; the `SetupScene()` call-site does not need to change.



## [2026-05-27] Teen Character Setup, Animation Loop Fix, And Global Idle-Vibration Fix

### Added

- Added `Teen_Movement.controller` — a dual-state Animator Controller (Idle + Movement BlendTree) wired to four Teen animation clips: `Teen_IdleLookAround`, `Teen_Walk`, `Teen_WalkFast`, `Teen_Run`.
- Added Teen family recognition to `CharacterRootSetupTool`: detects "Teen" in child names, assigns `Teen_Movement.controller` and `Basic_Characters_Teen.fbx` avatar, and sets BodyAnimator control mode.
- Added Teen family recognition to `CharacterMover.ResolveAnimatorControlMode()` so Teen characters are always driven in BodyAnimator mode.
- Added `CharacterAnimationDiagnostic` editor tool under `Tools → Diagnose Character Animation` for reporting all Animators, SkinnedMeshRenderers, root bones, and recommended actions on a selected character root.

### Changed

- Updated `CharacterRootSetupTool` so Senior characters resolve to BodyAnimator mode instead of SyncedChildren, and `MultiAnimatorSync` is removed during setup if present.

### Fixed

- Fixed all four Teen animation clips (`Teen_IdleLookAround`, `Teen_Walk`, `Teen_WalkFast`, `Teen_Run`) — `m_LoopTime` and `m_LoopBlend` were `0`; set to `1` so clips loop correctly. Without this fix the walk animation played once and froze.
- Fixed a global idle-vibration bug in `CharacterMover.AnimationHandler.Animate()` affecting all character families (Teen, Pumped/Charles, Adult/Victoria). The original code used `(axis - m_FlowAxis).normalized` and `Mathf.Sign(state - m_FlowState)` which always stepped by a fixed distance regardless of how close `FlowAxis`/`FlowState` were to their targets. At rest, this caused permanent ±overshoot oscillation around zero. At lower frame rates (more NPCs in scene = higher CPU load = larger `deltaTime`), the oscillation amplitude grew large enough to cross the `0.05` Vert threshold every frame, triggering continuous state-machine transitions and visible vibration. Fixed by replacing both with `Vector2.MoveTowards` / `Mathf.MoveTowards`, which stop exactly at the target and never overshoot.

### Notes

- Character `228` / Sophie (Senior) remains unresolved and was not worked on in this session.
- The Adult/Victoria structural skeleton jitter noted in the previous entry may be partially improved by the `MoveTowards` oscillation fix, but the underlying modular rig duplication issue is still open future work.

## [2026-05-25] Senior Character Investigation And Structural Tooling Update

### Added

- Added a dedicated `Senior_Movement.controller` wired to Senior idle, walk, walk-fast, and run clips.
- Added Senior family recognition to the editor repair workflow so Senior characters are no longer treated as Pumped by default.

### Changed

- Updated `CharacterMover` so modular character animator ownership can distinguish between root-driven, body-driven, and synchronized-child modes.
- Updated `CharacterRootSetupTool` so Senior characters resolve to Senior controller and avatar assets instead of falling back to Pumped or Adult assumptions.
- Updated `BoneRetargeter` to recognize Senior characters and use Senior-specific controller and avatar paths.
- Updated `SkeletonFlattener` to recognize Senior characters and prefer the master renderer's actual avatar before falling back to the family shared avatar.

### Fixed

- Restored Charles / `280` after regressions introduced while trying to generalize the runtime animator-selection path for Adult and Senior characters.
- Fixed the structural tools so Senior family assets are no longer routed through Pumped-specific controller/avatar assumptions.

### Notes

- Senior character `228` / Sophie is still not resolved.
- The current visible failure remains: Sophie stays in T-pose and modular parts drift out of alignment in Play Mode.
- This means the latest Senior controller assignment and Senior-aware structural tooling were not sufficient by themselves.
- A previous attempt to revisit `MultiAnimatorSync` did not provide a stable fix and should not be treated as the primary path forward.
- The most likely remaining causes are now narrower and should be investigated in this order:
	1. Senior avatar binding mismatch on the actual body/master renderer versus the shared Senior avatar asset.
	2. Senior modular parts were never structurally retargeted/flattened on the specific Sophie prefab or scene instance.
	3. Scene-instance overrides on Sophie may still preserve an older invalid animator/bone setup even after prefab-side fixes.
	4. Senior clips or avatar import settings may differ from Pumped/Adult in a way that prevents valid humanoid binding at runtime.

## [2026-05-18] NPC Setup Tool And Adult Character Follow-Up

### Added

- Added a new editor utility under `Tools` to set up selected NPC character roots automatically.
- The setup tool adds or refreshes the standard root components used by the project:
	- `CharacterController`
	- `CharacterMover`
	- `NPCRouteWander`
	- `Animator`
- The setup tool also assigns the correct shared controller and avatar for Adult vs Pumped character families and disables child animators.

### Changed

- Expanded the modular character repair workflow to support Adult characters as well as Pumped characters.
- Updated editor repair tools so Adult modular characters can use the same root-level animator workflow used for NPC setup.
- Added Adult support to the skeleton flattening workflow for cases where a simple retarget is not enough.
- Improved waypoint NPC movement behavior with obstacle avoidance, side-stepping, turn-in-place handling, and stuck recovery.
- Tuned movement animation behavior so walking and running no longer use the same overly strong blend values.

### Fixed

- Fixed the original `280` / Charles modular animation failure and T-pose issue.
- Fixed runtime animator selection so the movement system no longer blindly uses an invalid animator candidate.
- Fixed multiple-child-animator conflicts in modular characters by normalizing the hierarchy at runtime.
- Fixed Adult retarget warnings caused by incomplete master-bone lookup during modular retargeting.
- Fixed a play mode issue where a manually added root animator on `284` could be disabled by the runtime selection logic.

### Notes

- Character `284` now moves and animates, but it still has visible vibration / jitter in the modular rig.
- This Adult vibration issue is not resolved yet and should be treated as future work.
- The most likely remaining cause is structural duplication or instability inside the imported Adult modular skeleton hierarchy rather than the basic movement scripts.
- If this issue needs to be revisited later, start from the Adult modular rig structure and skeleton flattening path rather than repeating the earlier controller-only fixes.

## [2026-05-18] Modular Character Animation Fix

### Issue

The NPC character `280` in the House scene was stuck in a T-pose even though movement scripts were present and the project had a valid animator controller and avatar.

The character is a modular humanoid:
- each body part exists as a separate child object
- multiple child objects carried their own `Animator` and `Skeleton_Pumped`
- the root object carried movement scripts such as `CharacterController`, `CharacterMover`, and `NPCRouteWander`

This created a mismatch between:
- the object responsible for movement
- the object actually owning the playable humanoid rig
- the animator Unity should use at runtime

### Root Cause

The runtime could select or preserve the wrong `Animator` inside the modular hierarchy.

That caused one or more of these failures:
- the root-level animator was preferred even when the real rig lived on the body child
- extra animators stayed enabled in the hierarchy and conflicted with the playable body animator
- the correct body animator was not guaranteed to be the only active animator driving the character

Result:
- `CharacterMover` was executing
- the correct movement parameters were being generated
- but the wrong animation target could remain active, leaving the model in T-pose

### Fixed

- Updated `CharacterMover` to resolve the best animator candidate from the full hierarchy instead of blindly preferring the root object.
- Prioritized the real body animator for modular characters such as `Pumped_Male_Body_02`.
- Added runtime hierarchy normalization so the selected animator becomes the active animator.
- Disabled all non-selected animators in the same character hierarchy at runtime.
- Preserved or copied a valid controller and avatar onto the selected animator when needed.
- Corrected the editor repair workflow so retargeting no longer pushes modular characters toward an invalid root-animator setup.

### Validation

Validated by runtime log output confirming:

`[CharacterMover] Animator found: 'Pumped_Male_Body_02'`

After the fix:
- the character animates correctly
- movement and animation are synchronized
- the House scene NPC setup for `280` works as expected

### Files Involved

- `Assets/ithappy/City_Characters/Scripts/Character_Controller/CharacterMover.cs`
- `Assets/Scripts/Editor/BoneRetargeter.cs`

### Notes For Future Character Work

- For modular humanoid characters, keep movement scripts on the root object.
- Ensure only the correct playable animator remains active at runtime.
- Be careful with imported characters that include repeated skeletons or repeated animator components across body parts.
- If a character enters T-pose while movement code is still running, first verify which animator is actually being selected and enabled at runtime.