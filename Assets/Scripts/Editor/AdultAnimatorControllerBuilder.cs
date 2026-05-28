using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates an Animator Controller for Pumped-type characters (e.g. character 280 / Alex)
/// wired to the Pumped animation clips and driven by CharacterMover's parameters:
///   Hor   (float) — horizontal input axis
///   Vert  (float) — forward/back input axis
///   State (float) — 0 = walk, 1 = run
///   IsJump (bool) — jump trigger
///
/// Run via: Tools > Create Pumped Animator Controller
/// Then assign it to the Animator component on character 280 (Alex).
/// </summary>
public static class AdultAnimatorControllerBuilder
{
    private const string SavePath =
        "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Pumped_Movement.controller";

    // Clip asset paths — Pumped rig (character 280 / Alex)
    private const string IdlePath     = "Assets/ithappy/City_Characters/Animations/Pumped/Pumped_Idle_Look_Around.anim";
    private const string WalkPath     = "Assets/ithappy/City_Characters/Animations/Pumped/Pumped_Walk.anim";
    private const string WalkFastPath = "Assets/ithappy/City_Characters/Animations/Pumped/Pumped_Walk_Fast.anim";
    private const string RunPath      = "Assets/ithappy/City_Characters/Animations/Pumped/Pumped_Run.anim";

    [MenuItem("Tools/Create Pumped Animator Controller")]
    public static void Build()
    {
        var idle     = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        var walk     = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkPath);
        var walkFast = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkFastPath);
        var run      = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunPath);

        if (idle == null || walk == null || walkFast == null || run == null)
        {
            Debug.LogError("[AdultAnimatorControllerBuilder] Could not load one or more clips. Check paths:\n"
                + IdlePath + "\n" + WalkPath + "\n" + WalkFastPath + "\n" + RunPath);
            return;
        }

        EnableLoop(idle);
        EnableLoop(walk);
        EnableLoop(walkFast);
        EnableLoop(run);

        // Remove old controller if it exists
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SavePath) != null)
            AssetDatabase.DeleteAsset(SavePath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(SavePath);

        // Parameters that CharacterMover writes to
        controller.AddParameter("Hor",    AnimatorControllerParameterType.Float);
        controller.AddParameter("Vert",   AnimatorControllerParameterType.Float);
        controller.AddParameter("State",  AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJump", AnimatorControllerParameterType.Bool);

        var rootSM = controller.layers[0].stateMachine;

        // ── Idle state ─────────────────────────────────────────────────────────
        var idleState = rootSM.AddState("Idle");
        idleState.motion = idle;
        rootSM.defaultState = idleState;

        // ── Movement blend tree (idle → walk → walkFast → run) ───────────────
        // CreateBlendTreeInController properly embeds the BlendTree asset —
        // avoids the UnityEditor.Graphs.Edge.WakeUp NullReferenceException
        // caused by manually newing a BlendTree and calling AddObjectToAsset.
        BlendTree blendTree;
        var moveState = controller.CreateBlendTreeInController("Movement", out blendTree, 0);
        // Ensure the BlendTree is a private sub-asset (HideInHierarchy).
        // Without this the YAML gets m_ObjectHideFlags: 0, which causes
        // UnityEditor.Graphs.Edge.WakeUp NullReferenceException.
        blendTree.hideFlags      = HideFlags.HideInHierarchy;
        blendTree.blendType      = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Vert";

        blendTree.AddChild(idle,     0.0f);
        blendTree.AddChild(walk,     0.3f);
        blendTree.AddChild(walkFast, 0.6f);
        blendTree.AddChild(run,      1.0f);

        // ── Transitions ────────────────────────────────────────────────────────
        // Idle → Movement when moving
        var toMove = idleState.AddTransition(moveState);
        toMove.hasExitTime = false;
        toMove.duration    = 0.15f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Vert");

        // Movement → Idle when stopped
        var toIdle = moveState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Vert");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PumpedAnimatorControllerBuilder] Created: {SavePath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(SavePath);
    }

    private static void EnableLoop(AnimationClip clip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }
}
