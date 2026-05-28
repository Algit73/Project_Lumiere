using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates an Animator Controller for Senior-type characters wired to the Senior clips
/// and driven by CharacterMover's parameters:
///   Hor   (float)
///   Vert  (float)
///   State (float) — 0 = walk, 1 = run
///   IsJump (bool)
///
/// Run via: Tools > Create Senior Animator Controller
/// </summary>
public static class SeniorAnimatorControllerBuilder
{
    private const string SavePath =
        "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Senior_Movement.controller";

    private const string IdlePath     = "Assets/ithappy/City_Characters/Animations/Senior/Senior_WalkIdle.anim";
    private const string WalkPath     = "Assets/ithappy/City_Characters/Animations/Senior/Senior_Walk.anim";
    private const string WalkFastPath = "Assets/ithappy/City_Characters/Animations/Senior/Senior_WalkFast.anim";
    private const string RunPath      = "Assets/ithappy/City_Characters/Animations/Senior/Senior_Run.anim";

    [MenuItem("Tools/Create Senior Animator Controller")]
    public static void Build()
    {
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkPath);
        var walkFast = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkFastPath);
        var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunPath);

        if (idle == null || walk == null || walkFast == null || run == null)
        {
            Debug.LogError("[SeniorAnimatorControllerBuilder] Could not load one or more clips. Check paths:\n"
                + IdlePath + "\n" + WalkPath + "\n" + WalkFastPath + "\n" + RunPath);
            return;
        }

        EnableLoop(idle);
        EnableLoop(walk);
        EnableLoop(walkFast);
        EnableLoop(run);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SavePath) != null)
            AssetDatabase.DeleteAsset(SavePath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(SavePath);

        controller.AddParameter("Hor", AnimatorControllerParameterType.Float);
        controller.AddParameter("Vert", AnimatorControllerParameterType.Float);
        controller.AddParameter("State", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJump", AnimatorControllerParameterType.Bool);

        var rootSM = controller.layers[0].stateMachine;

        var idleState = rootSM.AddState("Idle");
        idleState.motion = idle;
        rootSM.defaultState = idleState;

        BlendTree walkRunTree;
        var moveState = controller.CreateBlendTreeInController("Movement", out walkRunTree, 0);
        walkRunTree.hideFlags = HideFlags.HideInHierarchy;
        walkRunTree.blendType = BlendTreeType.Simple1D;
        walkRunTree.blendParameter = "Vert";

        BlendTree speedTree = new BlendTree
        {
            name = "WalkRunByState",
            hideFlags = HideFlags.HideInHierarchy,
            blendType = BlendTreeType.Simple1D,
            blendParameter = "State"
        };
        AssetDatabase.AddObjectToAsset(speedTree, controller);

        speedTree.AddChild(walk, 0.0f);
        speedTree.AddChild(walkFast, 0.5f);
        speedTree.AddChild(run, 1.0f);

        walkRunTree.AddChild(idle, 0.0f);
        walkRunTree.AddChild(speedTree, 0.15f);
        walkRunTree.AddChild(speedTree, 1.0f);

        var toMove = idleState.AddTransition(moveState);
        toMove.hasExitTime = false;
        toMove.duration = 0.15f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Vert");

        var toIdle = moveState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Vert");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SeniorAnimatorControllerBuilder] Created: {SavePath}");
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