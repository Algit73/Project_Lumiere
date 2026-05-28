using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Retargets all SkinnedMeshRenderers on a modular character so they share
/// the skeleton of one "master" SkinnedMeshRenderer.
///
/// Usage:
///   1. Select the ROOT GameObject of the character (e.g. "280") in the Hierarchy.
///   2. Menu: Tools → Retarget Character Bones
///   3. A dialog asks you to pick the master mesh (the child that has the Animator,
///      e.g. "Pumped_Male_Body_02").  Type its name and click OK.
///   4. All other SkinnedMeshRenderers are remapped to that skeleton.
///   5. The ROOT keeps the runtime Animator, CharacterMover,
///      CharacterController, and task scripts.
/// </summary>
public static class BoneRetargeter
{
    private const string PumpedControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Pumped_Movement.controller";
    private const string PumpedAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Pumped.fbx";
    private const string AdultControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Character_Movement.controller";
    private const string AdultAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Character_Adult.fbx";
    private const string SeniorControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Senior_Movement.controller";
    private const string SeniorAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Senior.fbx";

    [MenuItem("Tools/Retarget Character Bones")]
    public static void Retarget()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Retarget Bones", "Select the character ROOT in the Hierarchy first.", "OK");
            return;
        }

        var allSMRs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (allSMRs.Length < 2)
        {
            EditorUtility.DisplayDialog("Retarget Bones", "Need at least 2 SkinnedMeshRenderers. Nothing to do.", "OK");
            return;
        }

        // List them so the user can identify the master
        var names = new System.Text.StringBuilder("SkinnedMeshRenderers found:\n");
        foreach (var s in allSMRs) names.AppendLine("  • " + s.gameObject.name);
        Debug.Log("[BoneRetargeter] " + names);

        // Auto-pick: prefer a child whose name contains "Body" (most reliable humanoid skeleton);
        // fall back to the child with the most bone transforms.
        SkinnedMeshRenderer master = null;
        int maxBones = 0;
        foreach (var smr in allSMRs)
        {
            int boneCount = smr.gameObject.GetComponentsInChildren<Transform>(true).Length;
            bool isBody = smr.gameObject.name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0;
            // Prefer Body-named mesh; among those, pick the one with most bones.
            if (master == null
                || (isBody && (master.gameObject.name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) < 0))
                || (isBody == (master.gameObject.name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0) && boneCount > maxBones))
            {
                maxBones = boneCount;
                master = smr;
            }
        }

        if (master == null)
        {
            EditorUtility.DisplayDialog("Retarget Bones", "Could not auto-detect master mesh.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Retarget Bones",
            $"Auto-detected master skeleton: '{master.gameObject.name}' ({maxBones} transforms).\n\nRetarget all other meshes to this skeleton?",
            "Yes", "Cancel");

        if (!confirmed) return;

        // Build a lookup from the actual master skeleton root, not just the mesh object.
        var masterBones = BuildMasterBoneLookup(root, master);

        int retargeted = 0;
        foreach (var smr in allSMRs)
        {
            if (smr == master) continue;

            Undo.RecordObject(smr, "Retarget Bones");

            var oldBones  = smr.bones;
            var newBones  = new Transform[oldBones.Length];
            bool allFound = true;

            for (int i = 0; i < oldBones.Length; i++)
            {
                if (oldBones[i] == null) { newBones[i] = null; continue; }
                if (masterBones.TryGetValue(oldBones[i].name, out var nb))
                    newBones[i] = nb;
                else
                {
                    Debug.LogWarning($"[BoneRetargeter] '{smr.gameObject.name}': bone '{oldBones[i].name}' not found in master skeleton.");
                    newBones[i] = oldBones[i];
                    allFound = false;
                }
            }

            // Retarget root bone too
            if (smr.rootBone != null && masterBones.TryGetValue(smr.rootBone.name, out var rootBone))
                smr.rootBone = rootBone;

            smr.bones = newBones;
            PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
            EditorUtility.SetDirty(smr);
            retargeted++;

            Debug.Log($"[BoneRetargeter] '{smr.gameObject.name}' retargeted to '{master.gameObject.name}'. All bones found: {allFound}");
        }

        ConfigureMasterAnimator(root, master);

        // Apply both the bone remap and the animator setup as one prefab change.
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(root);
        if (prefabRoot != null)
        {
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            Debug.Log("[BoneRetargeter] Changes applied to prefab asset.");
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[BoneRetargeter] Done. {retargeted} meshes retargeted to '{master.gameObject.name}'.");
        EditorUtility.DisplayDialog("Retarget Bones",
            $"Done! {retargeted} meshes retargeted to '{master.gameObject.name}'.\n\n" +
            $"Animator setup was corrected for modular characters:\n" +
            $"1. '{root.name}' keeps the active ROOT Animator\n" +
            $"2. Other child Animators were disabled\n" +
            $"3. The ROOT Animator is the runtime animation driver\n\n" +
            $"Keep CharacterMover + CharacterController + NPCRouteWander on the character ROOT only.",
            "OK");
    }

    private static Dictionary<string, Transform> BuildMasterBoneLookup(GameObject root, SkinnedMeshRenderer master)
    {
        var masterBones = new Dictionary<string, Transform>();
        var searchRoot = ResolveMasterSearchRoot(root.transform, master);

        foreach (var t in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!masterBones.ContainsKey(t.name))
                masterBones[t.name] = t;
        }

        return masterBones;
    }

    private static Transform ResolveMasterSearchRoot(Transform characterRoot, SkinnedMeshRenderer master)
    {
        if (master.rootBone == null)
            return master.transform;

        var searchRoot = master.rootBone;
        while (searchRoot.parent != null && searchRoot.parent != characterRoot)
            searchRoot = searchRoot.parent;

        return searchRoot;
    }

    private static void ConfigureMasterAnimator(GameObject root, SkinnedMeshRenderer master)
    {
        var rootAnimator = root.GetComponent<Animator>();
        var masterAnimator = master.GetComponent<Animator>();
        ResolveAnimationSetup(master.gameObject.name, out var fallbackControllerPath, out var fallbackAvatarPath);
        var masterAvatar = LoadAvatarFromMasterRenderer(master);

        RuntimeAnimatorController controller = masterAnimator != null && masterAnimator.runtimeAnimatorController != null
            ? masterAnimator.runtimeAnimatorController
            : rootAnimator != null && rootAnimator.runtimeAnimatorController != null
            ? rootAnimator.runtimeAnimatorController
            : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(fallbackControllerPath);
        Avatar avatar = masterAvatar != null
            ? masterAvatar
            : masterAnimator != null && masterAnimator.avatar != null
            ? masterAnimator.avatar
            : rootAnimator != null && rootAnimator.avatar != null
            ? rootAnimator.avatar
            : LoadAvatarFromAsset(fallbackAvatarPath);

        if (rootAnimator == null)
            rootAnimator = Undo.AddComponent<Animator>(root);

        var sync = root.GetComponent<Controller.MultiAnimatorSync>();
        if (sync != null)
            Undo.DestroyObjectImmediate(sync);

        Undo.RecordObject(rootAnimator, "Configure Root Animator");
        if (controller != null)
            rootAnimator.runtimeAnimatorController = controller;
        if (avatar != null)
            rootAnimator.avatar = avatar;
        rootAnimator.enabled = true;
        rootAnimator.applyRootMotion = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rootAnimator);
        EditorUtility.SetDirty(rootAnimator);

        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator == rootAnimator)
                continue;

            Undo.RecordObject(animator, "Disable Extra Animator");
            animator.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
        }
    }

    private static void ResolveAnimationSetup(string masterName, out string controllerPath, out string avatarAssetPath)
    {
        if (!string.IsNullOrEmpty(masterName) && masterName.IndexOf("Senior", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            controllerPath = SeniorControllerPath;
            avatarAssetPath = SeniorAvatarAssetPath;
            return;
        }

        if (!string.IsNullOrEmpty(masterName) && masterName.IndexOf("Adult", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            controllerPath = AdultControllerPath;
            avatarAssetPath = AdultAvatarAssetPath;
            return;
        }

        controllerPath = PumpedControllerPath;
        avatarAssetPath = PumpedAvatarAssetPath;
    }

    private static Avatar LoadAvatarFromMasterRenderer(SkinnedMeshRenderer master)
    {
        if (master == null || master.sharedMesh == null)
            return null;

        var meshAssetPath = AssetDatabase.GetAssetPath(master.sharedMesh);
        if (string.IsNullOrEmpty(meshAssetPath))
            return null;

        return LoadAvatarFromAsset(meshAssetPath);
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
}
