using System;
using Controller;
using UnityEditor;
using UnityEngine;

public static class CharacterRootSetupTool
{
    private const string PumpedControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Pumped_Movement.controller";
    private const string PumpedAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Pumped.fbx";
    private const string AdultControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Character_Movement.controller";
    private const string AdultAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Character_Adult.fbx";
    private const string SeniorControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Senior_Movement.controller";
    private const string SeniorAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Senior.fbx";
    private const string TeenControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Teen_Movement.controller";
    private const string TeenAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Teen.fbx";

    [MenuItem("Tools/Setup Selected NPC Character Roots")]
    public static void SetupSelectedCharacterRoots()
    {
        var selectedRoots = Selection.gameObjects;
        if (selectedRoots == null || selectedRoots.Length == 0)
        {
            EditorUtility.DisplayDialog("Setup NPC Character Roots", "Select one or more character root objects first.", "OK");
            return;
        }

        int processed = 0;
        foreach (var root in selectedRoots)
        {
            if (root == null)
                continue;

            SetupCharacterRoot(root);
            processed++;
        }

        EditorUtility.DisplayDialog(
            "Setup NPC Character Roots",
            $"Configured {processed} selected character root(s).\n\n" +
            "Added or refreshed: CharacterController, CharacterMover, NPCRouteWander, and Animator.\n" +
            "Child Animators were disabled.",
            "OK");
    }

    [MenuItem("Tools/Setup Selected NPC Character Roots", true)]
    public static bool ValidateSetupSelectedCharacterRoots()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static void SetupCharacterRoot(GameObject root)
    {
        Undo.RegisterFullObjectHierarchyUndo(root, "Setup NPC Character Root");

        var animator = GetOrAddComponent<Animator>(root);
        GetOrAddComponent<CharacterController>(root);
        var mover = GetOrAddComponent<CharacterMover>(root);
        GetOrAddComponent<NPCRouteWander>(root);

        ResolveAnimationSetup(root, out var controllerPath, out var avatarPath);

        // Senior characters use BodyAnimator mode (same as Pumped) - NOT SyncedChildren.
        // MultiAnimatorSync was tried and flagged as unstable; remove it if present.
        var sync = root.GetComponent<MultiAnimatorSync>();
        if (sync != null)
            Undo.DestroyObjectImmediate(sync);

        bool useAnimatorSync = false;

        EnsureControllerExists(controllerPath);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        animator.avatar = ResolveAvatar(root, avatarPath);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.enabled = true;
        EditorUtility.SetDirty(animator);

        ConfigureAnimatorControlMode(mover, controllerPath);

        foreach (var childAnimator in root.GetComponentsInChildren<Animator>(true))
        {
            if (childAnimator == animator)
                continue;

            childAnimator.enabled = useAnimatorSync;
            EditorUtility.SetDirty(childAnimator);
        }

        EditorUtility.SetDirty(root);
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        var existing = gameObject.GetComponent<T>();
        if (existing != null)
            return existing;

        return Undo.AddComponent<T>(gameObject);
    }

    private static void ResolveAnimationSetup(GameObject root, out string controllerPath, out string avatarPath)
    {
        bool isSenior = false;
        bool isAdult = false;
        bool isTeen = false;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name.IndexOf("Senior", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isSenior = true;
                break;
            }

            if (transform.name.IndexOf("Teen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isTeen = true;
            }

            if (transform.name.IndexOf("Adult", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                isAdult = true;
            }
        }

        if (isSenior)
        {
            controllerPath = SeniorControllerPath;
            avatarPath = SeniorAvatarAssetPath;
            return;
        }

        if (isTeen)
        {
            controllerPath = TeenControllerPath;
            avatarPath = TeenAvatarAssetPath;
            return;
        }

        if (isAdult)
        {
            controllerPath = AdultControllerPath;
            avatarPath = AdultAvatarAssetPath;
            return;
        }

        controllerPath = PumpedControllerPath;
        avatarPath = PumpedAvatarAssetPath;
    }

    private static void EnsureControllerExists(string controllerPath)
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) != null)
            return;

        if (string.Equals(controllerPath, SeniorControllerPath, StringComparison.OrdinalIgnoreCase))
        {
            SeniorAnimatorControllerBuilder.Build();
            return;
        }

        if (string.Equals(controllerPath, PumpedControllerPath, StringComparison.OrdinalIgnoreCase))
        {
            AdultAnimatorControllerBuilder.Build();
        }
    }

    private static Avatar LoadAvatarFromAsset(string assetPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static Avatar ResolveAvatar(GameObject root, string fallbackPath)
    {
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator.gameObject == root)
                continue;

            if (animator.gameObject.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0 && animator.avatar != null)
                return animator.avatar;
        }

        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator.gameObject == root)
                continue;

            if (animator.avatar != null)
                return animator.avatar;
        }

        return LoadAvatarFromAsset(fallbackPath);
    }

    private static void ConfigureAnimatorControlMode(CharacterMover mover, string controllerPath)
    {
        const int RootAnimatorMode = 1;
        const int BodyAnimatorMode = 2;

        var serializedMover = new SerializedObject(mover);
        var animatorControlMode = serializedMover.FindProperty("m_AnimatorControlMode");
        if (animatorControlMode == null)
            return;

        // Senior and Teen use BodyAnimator (body-child Animator owns the rig, same as Pumped).
        if (string.Equals(controllerPath, SeniorControllerPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controllerPath, TeenControllerPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controllerPath, PumpedControllerPath, StringComparison.OrdinalIgnoreCase))
            animatorControlMode.enumValueIndex = BodyAnimatorMode;
        else
            animatorControlMode.enumValueIndex = RootAnimatorMode;

        serializedMover.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mover);
    }
}