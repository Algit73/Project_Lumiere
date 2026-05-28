# Changelog

All notable changes to this project should be documented in this file.

Format:
- New entries go at the top.
- Each release or working milestone should include `Added`, `Changed`, `Fixed`, and `Notes` when relevant.
- Dates use `YYYY-MM-DD`.

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