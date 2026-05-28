using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Moves the master skeleton (rootBone) from a modular character child directly
/// under the character ROOT so that a single Animator on the ROOT with
/// Basic_Characters_PumpedAvatar can find all bones by their expected paths.
///
/// Usage:
///   1. Select the ROOT GameObject of the character (e.g. "280") in the Hierarchy.
///   2. Menu: Tools → Flatten Skeleton to Root
///   3. Confirm. Done.
///   4. Add ONE Animator to the ROOT with:
///        Controller = Pumped_Movement
///        Avatar     = Basic_Characters_PumpedAvatar
///   5. Remove any Animator components from children.
/// </summary>
public static class SkeletonFlattener
{
    private const string PumpedControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Pumped_Movement.controller";
    private const string PumpedAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Pumped.fbx";
    private const string AdultControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Character_Movement.controller";
    private const string AdultAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Character_Adult.fbx";
    private const string SeniorControllerPath = "Assets/ithappy/City_Characters/Animations/Animation_Controllers/Senior_Movement.controller";
    private const string SeniorAvatarAssetPath = "Assets/ithappy/City_Characters/Meshes/Basic_Characters_Senior.fbx";

    [MenuItem("Tools/Flatten Skeleton to Root")]
    public static void Flatten()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Flatten Skeleton", "Select the character ROOT in the Hierarchy first.", "OK");
            return;
        }

        var allSMRs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (allSMRs.Length == 0)
        {
            EditorUtility.DisplayDialog("Flatten Skeleton", "No SkinnedMeshRenderers found under '" + root.name + "'.", "OK");
            return;
        }

        FlattenRoot(root, showDialogs: true);
    }

    public static bool FlattenRoot(GameObject root, bool showDialogs)
    {
        if (root == null)
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("Flatten Skeleton", "Select the character ROOT in the Hierarchy first.", "OK");
            return false;
        }

        var allSMRs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (allSMRs.Length == 0)
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("Flatten Skeleton", "No SkinnedMeshRenderers found under '" + root.name + "'.", "OK");
            return false;
        }

        // ── 1. Find master SMR (prefer name contains "Body", then most bones) ─────
        SkinnedMeshRenderer master = null;
        int maxBones = -1;
        foreach (var smr in allSMRs)
        {
            int boneCount = smr.gameObject.GetComponentsInChildren<Transform>(true).Length;
            bool isBody = smr.gameObject.name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool masterIsBody = master != null &&
                master.gameObject.name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (master == null
                || (isBody && !masterIsBody)
                || (isBody == masterIsBody && boneCount > maxBones))
            {
                maxBones = boneCount;
                master = smr;
            }
        }

        // ── 2. Find the skeleton root bone ───────────────────────────────────────
        Transform skeletonRoot = master.rootBone;

        if (skeletonRoot == null && master.bones.Length > 0)
        {
            // Walk up from the first bone until we hit the master GO or the root GO
            var bone = master.bones[0];
            while (bone != null
                   && bone.parent != master.transform
                   && bone.parent != root.transform
                   && bone.parent != null)
            {
                bone = bone.parent;
            }
            skeletonRoot = bone;
        }

        if (skeletonRoot == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Flatten Skeleton",
                    "Could not find the skeleton root bone on '" + master.gameObject.name + "'.\n" +
                    "Make sure the SkinnedMeshRenderer has rootBone assigned.", "OK");
            }
            return false;
        }

        // Already directly under root — nothing to do
        if (skeletonRoot.parent == root.transform)
        {
            ConfigureRootAnimator(root, master, showDialogs: false);
            DisableChildAnimators(root);

            if (showDialogs)
            {
                EditorUtility.DisplayDialog("Flatten Skeleton",
                    "Skeleton root '" + skeletonRoot.name + "' is already a direct child of '" + root.name + "'.\n" +
                    "Root Animator was refreshed and child Animators were disabled.",
                    "OK");
            }
            return true;
        }

        bool confirmed = !showDialogs || EditorUtility.DisplayDialog("Flatten Skeleton",
            "Master mesh:    " + master.gameObject.name + "\n" +
            "Skeleton root:  " + skeletonRoot.name + "\n" +
            "Current parent: " + (skeletonRoot.parent != null ? skeletonRoot.parent.name : "none") + "\n\n" +
            "This will:\n" +
            "  1. Retarget all mesh pieces to share the master skeleton.\n" +
            "  2. Move '" + skeletonRoot.name + "' directly under '" + root.name + "'.\n" +
            "  3. Configure ONE Animator on the root.\n" +
            "  4. Disable Animator components on children.",
            "Flatten", "Cancel");

        if (!confirmed) return false;

        // ── 3. Build bone name → Transform lookup from master skeleton ───────────
        var masterBoneMap = new Dictionary<string, Transform>();
        foreach (var t in skeletonRoot.GetComponentsInChildren<Transform>(true))
            if (!masterBoneMap.ContainsKey(t.name))
                masterBoneMap[t.name] = t;

        // Also include the master SMR's own child transforms (in case skeleton is
        // stored as siblings of the mesh, not under skeletonRoot)
        foreach (var t in master.gameObject.GetComponentsInChildren<Transform>(true))
            if (!masterBoneMap.ContainsKey(t.name))
                masterBoneMap[t.name] = t;

        // ── 4. Retarget all non-master SMRs to master skeleton ────────────────────
        int retargeted = 0;
        foreach (var smr in allSMRs)
        {
            if (smr == master) continue;

            Undo.RecordObject(smr, "Flatten Skeleton - Retarget");

            // Remap bones array
            var oldBones = smr.bones;
            var newBones = new Transform[oldBones.Length];
            for (int i = 0; i < oldBones.Length; i++)
            {
                if (oldBones[i] == null) { newBones[i] = null; continue; }
                if (!masterBoneMap.TryGetValue(oldBones[i].name, out newBones[i]))
                {
                    newBones[i] = oldBones[i]; // keep original if name not found
                    Debug.LogWarning("[SkeletonFlattener] Bone not found in master skeleton: "
                        + oldBones[i].name + " (on " + smr.gameObject.name + ")");
                }
            }
            smr.bones = newBones;

            // Remap rootBone
            if (smr.rootBone != null
                && masterBoneMap.TryGetValue(smr.rootBone.name, out var newRoot))
                smr.rootBone = newRoot;

            EditorUtility.SetDirty(smr);
            retargeted++;
        }

        // ── 5. Move skeleton root directly under character root ───────────────────
        // worldPositionStays = true keeps the visual transform unchanged
        Undo.SetTransformParent(skeletonRoot, root.transform, "Flatten Skeleton - Move Skeleton");
        // Preserve world position/rotation after reparenting
        // (Undo.SetTransformParent preserves world pose by default in Unity 2021+)

        ConfigureRootAnimator(root, master, showDialogs: false);
        DisableChildAnimators(root);

        EditorUtility.SetDirty(root);

        // ── 6. Apply prefab changes ───────────────────────────────────────────────
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(root);
        if (prefabRoot != null)
        {
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            Debug.Log("[SkeletonFlattener] Prefab changes applied.");
        }

        AssetDatabase.SaveAssets();

        Debug.Log("[SkeletonFlattener] Done. " + retargeted + " meshes retargeted. " +
                  "Skeleton '" + skeletonRoot.name + "' is now a direct child of '" + root.name + "'.");

        if (showDialogs)
        {
            EditorUtility.DisplayDialog("Flatten Skeleton — Done",
                retargeted + " mesh piece(s) retargeted.\n" +
                "Skeleton '" + skeletonRoot.name + "' moved under '" + root.name + "'.\n\n" +
                "Root Animator was configured with the family-appropriate controller and shared avatar.\n" +
                "Child Animator components were disabled.",
                "OK");
        }

        return true;
    }

    private static void ConfigureRootAnimator(GameObject root, SkinnedMeshRenderer master, bool showDialogs)
    {
        ResolveAnimationSetup(master != null ? master.gameObject.name : root.name, out var controllerPath, out var avatarPath);

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        var avatar = LoadAvatarFromMasterRenderer(master) ?? LoadAvatarFromAsset(avatarPath);

        if (controller == null || avatar == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Flatten Skeleton",
                    "Could not load the character controller or shared avatar automatically.\n" +
                    "Verify these assets exist:\n" +
                    controllerPath + "\n" +
                    avatarPath,
                    "OK");
            }
            return;
        }

        var rootAnimator = root.GetComponent<Animator>();
        if (rootAnimator == null)
            rootAnimator = Undo.AddComponent<Animator>(root);

        Undo.RecordObject(rootAnimator, "Configure Root Animator");
        rootAnimator.runtimeAnimatorController = controller;
        rootAnimator.avatar = avatar;
        rootAnimator.applyRootMotion = false;
        rootAnimator.enabled = true;

        if (master != null)
            EditorUtility.SetDirty(master);
        EditorUtility.SetDirty(rootAnimator);
    }

    private static void ResolveAnimationSetup(string masterName, out string controllerPath, out string avatarPath)
    {
        if (!string.IsNullOrEmpty(masterName) && masterName.IndexOf("Senior", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            controllerPath = SeniorControllerPath;
            avatarPath = SeniorAvatarAssetPath;
            return;
        }

        if (!string.IsNullOrEmpty(masterName) && masterName.IndexOf("Adult", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            controllerPath = AdultControllerPath;
            avatarPath = AdultAvatarAssetPath;
            return;
        }

        controllerPath = PumpedControllerPath;
        avatarPath = PumpedAvatarAssetPath;
    }

    private static void DisableChildAnimators(GameObject root)
    {
        var rootAnimator = root.GetComponent<Animator>();
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator == rootAnimator)
                continue;

            Undo.RecordObject(animator, "Disable Child Animator");
            animator.enabled = false;
            EditorUtility.SetDirty(animator);
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

    private static Avatar LoadAvatarFromMasterRenderer(SkinnedMeshRenderer master)
    {
        if (master == null || master.sharedMesh == null)
            return null;

        var meshAssetPath = AssetDatabase.GetAssetPath(master.sharedMesh);
        if (string.IsNullOrEmpty(meshAssetPath))
            return null;

        return LoadAvatarFromAsset(meshAssetPath);
    }
}
