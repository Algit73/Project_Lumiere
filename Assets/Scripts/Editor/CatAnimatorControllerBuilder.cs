using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates a Cat_Movement Animator Controller wired to the Cat_01 animation clips.
/// Run via: Tools > Create Cat Animator Controller
/// </summary>
public static class CatAnimatorControllerBuilder
{
    private const string ControllerSavePath =
        "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Cat_Movement.controller";

    private const string IdleClipPath  = "Assets/ithappy/City_Characters/Animations/Animals/Cat_01/Cat_Idle.anim";
    private const string WalkClipPath  = "Assets/ithappy/City_Characters/Animations/Animals/Cat_01/Cat_Walk.anim";
    private const string RunClipPath   = "Assets/ithappy/City_Characters/Animations/Animals/Cat_01/Cat_Run.anim";

    [MenuItem("Tools/Create Cat Animator Controller")]
    public static void Build()
    {
        // Load animation clips
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        var run  = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunClipPath);

        if (idle == null || walk == null || run == null)
        {
            Debug.LogError("[CatAnimatorControllerBuilder] Could not load one or more cat anim clips. " +
                           "Check paths:\n" + IdleClipPath + "\n" + WalkClipPath + "\n" + RunClipPath);
            return;
        }

        // Enable Loop Time on all three clips so walk/idle/run cycle continuously
        EnableLoop(idle);
        EnableLoop(walk);
        EnableLoop(run);

        // Delete existing controller so we can recreate it cleanly
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerSavePath) != null)
            AssetDatabase.DeleteAsset(ControllerSavePath);

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerSavePath);

        // Add 'Vert' float parameter (NPCWander / NPCPatrol drives this)
        controller.AddParameter("Vert", AnimatorControllerParameterType.Float);

        var rootSM = controller.layers[0].stateMachine;

        // ── States ──────────────────────────────────────────────────────────────
        var idleState = rootSM.AddState("Idle", new Vector3(250, 0));
        idleState.motion = idle;

        var walkState = rootSM.AddState("Walk", new Vector3(250, 80));
        walkState.motion = walk;

        var runState  = rootSM.AddState("Run",  new Vector3(250, 160));
        runState.motion  = run;

        // Default state is Idle
        rootSM.defaultState = idleState;

        // ── Transitions ─────────────────────────────────────────────────────────
        // Idle → Walk  (Vert > 0.1)
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Vert");
        idleToWalk.duration = 0.15f;
        idleToWalk.hasExitTime = false;

        // Walk → Idle  (Vert < 0.05)
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Vert");
        walkToIdle.duration = 0.15f;
        walkToIdle.hasExitTime = false;

        // Walk → Run   (Vert > 0.7)
        var walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.7f, "Vert");
        walkToRun.duration = 0.1f;
        walkToRun.hasExitTime = false;

        // Run → Walk   (Vert < 0.65)
        var runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(AnimatorConditionMode.Less, 0.65f, "Vert");
        runToWalk.duration = 0.1f;
        runToWalk.hasExitTime = false;

        // ── Save ────────────────────────────────────────────────────────────────
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CatAnimatorControllerBuilder] Created: " + ControllerSavePath);

        // Ping asset in Project window
        EditorGUIUtility.PingObject(controller);
    }

    private static void EnableLoop(AnimationClip clip)
    {
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        if (!s.loopTime)
        {
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[CatAnimatorControllerBuilder] Enabled Loop Time on '{clip.name}'");
        }
    }
}
