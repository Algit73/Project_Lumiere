using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Diagnostic tool for investigating character animation setup.
/// Select a character root in the Hierarchy, then run Tools → Diagnose Character Animation.
/// Prints a full report to the Console.
/// </summary>
public static class CharacterAnimationDiagnostic
{
    [MenuItem("Tools/Diagnose Character Animation")]
    public static void Diagnose()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Diagnose Character Animation",
                "Select the character root GameObject in the Hierarchy first.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== Character Animation Diagnostic: '{root.name}' ===\n");

        // ── 1. Root components ──────────────────────────────────────────────────
        sb.AppendLine("── Root Components ──");
        sb.AppendLine($"  CharacterController : {(root.GetComponent<CharacterController>() != null ? "✓ present" : "✗ MISSING")}");
        sb.AppendLine($"  CharacterMover      : {(root.GetComponent("CharacterMover") != null ? "✓ present" : "✗ MISSING")}");
        sb.AppendLine($"  NPCRouteWander      : {(root.GetComponent("NPCRouteWander") != null ? "✓ present" : "✗ MISSING")}");
        sb.AppendLine($"  MultiAnimatorSync   : {(root.GetComponent("MultiAnimatorSync") != null ? "present (may conflict!)" : "absent")}");

        // ── 2. All Animators in hierarchy ───────────────────────────────────────
        sb.AppendLine("\n── Animators in Hierarchy ──");
        var animators = root.GetComponentsInChildren<Animator>(true);
        if (animators.Length == 0)
        {
            sb.AppendLine("  NONE found!");
        }
        else
        {
            foreach (var a in animators)
            {
                bool isRoot = a.gameObject == root;
                string controllerName = a.runtimeAnimatorController != null
                    ? a.runtimeAnimatorController.name
                    : "NONE";
                string avatarName = a.avatar != null ? a.avatar.name : "NONE";
                bool avatarValid = a.avatar != null && a.avatar.isValid;
                bool avatarHuman = a.avatar != null && a.avatar.isHuman;
                sb.AppendLine($"  [{(a.enabled ? "ON " : "OFF")}] '{a.gameObject.name}'{(isRoot ? " (ROOT)" : "")}");
                sb.AppendLine($"         Controller : {controllerName}");
                sb.AppendLine($"         Avatar     : {avatarName}  valid={avatarValid}  human={avatarHuman}");
                sb.AppendLine($"         HasTransformHierarchy: {a.hasTransformHierarchy}");

                var smr = a.GetComponent<SkinnedMeshRenderer>();
                sb.AppendLine($"         SkinnedMeshRenderer on same GO: {(smr != null ? "✓ YES" : "✗ NO")}");
            }
        }

        // ── 3. All SkinnedMeshRenderers ─────────────────────────────────────────
        sb.AppendLine("\n── SkinnedMeshRenderers ──");
        var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (smrs.Length == 0)
        {
            sb.AppendLine("  NONE found!");
        }
        else
        {
            // Collect all root-bone names to detect sharing
            var rootBoneNames = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var s in smrs)
            {
                string rb = s.rootBone != null ? $"'{s.rootBone.name}' (path: {GetPath(root.transform, s.rootBone)})" : "NULL";
                sb.AppendLine($"  '{s.gameObject.name}' → rootBone: {rb}  bones: {s.bones.Length}");

                string rbKey = s.rootBone != null ? GetPath(root.transform, s.rootBone) : "(null)";
                if (!rootBoneNames.TryAdd(rbKey, 1))
                    rootBoneNames[rbKey]++;
            }

            sb.AppendLine("\n  Root-bone sharing summary:");
            foreach (var kvp in rootBoneNames)
            {
                bool shared = kvp.Value > 1;
                sb.AppendLine($"    '{kvp.Key}' used by {kvp.Value} SMR(s) {(shared ? "← shared ✓" : "← NOT shared (modular parts will T-pose!)")}");
            }
        }

        // ── 4. Avatar bone-path check ───────────────────────────────────────────
        sb.AppendLine("\n── Avatar / Skeleton Reachability ──");
        var rootAnimator = root.GetComponent<Animator>();
        if (rootAnimator != null && rootAnimator.avatar != null && rootAnimator.avatar.isHuman)
        {
            // Check for the standard humanoid hip bone by searching children
            var hipBone = FindTransformByName(root.transform, "Hips");
            if (hipBone != null)
            {
                string hipPath = GetPath(root.transform, hipBone);
                sb.AppendLine($"  Root Animator avatar = '{rootAnimator.avatar.name}'");
                sb.AppendLine($"  'Hips' bone found at path from root: '{hipPath}'");
                if (hipPath == "Hips")
                    sb.AppendLine("  ✓ Hips is direct child of root — root Animator CAN bind skeleton.");
                else
                    sb.AppendLine("  ✗ Hips is NOT a direct child of root. Root Animator CANNOT bind the skeleton.");
                sb.AppendLine("    → The Animator must be on the body child (e.g. 'Senior_Female_Body_02'), not on the root.");
            }
            else
            {
                sb.AppendLine("  No 'Hips' transform found anywhere under root.");
            }
        }
        else
        {
            sb.AppendLine("  No Humanoid root Animator (skip skeleton reachability check).");
        }

        // ── 5. Recommended action ───────────────────────────────────────────────
        sb.AppendLine("\n── Recommended Actions ──");
        bool hasMover = root.GetComponent("CharacterMover") != null;
        bool hasCC = root.GetComponent<CharacterController>() != null;
        bool hasMultiSync = root.GetComponent("MultiAnimatorSync") != null;

        if (!hasMover || !hasCC)
            sb.AppendLine("  1. Run Tools → Setup Selected NPC Character Roots to add CharacterController + CharacterMover + NPCRouteWander.");

        if (hasMultiSync)
            sb.AppendLine("  2. MultiAnimatorSync is present — remove it. Senior characters should use BodyAnimator mode.");

        if (smrs.Length > 1)
        {
            bool allShared = true;
            string firstRootBonePath = null;
            foreach (var s in smrs)
            {
                string p = s.rootBone != null ? GetPath(root.transform, s.rootBone) : null;
                if (firstRootBonePath == null) firstRootBonePath = p;
                else if (p != firstRootBonePath) { allShared = false; break; }
            }
            if (!allShared)
                sb.AppendLine("  3. Run Tools → Retarget Character Bones to make all modular parts share one skeleton.");
        }

        sb.AppendLine("\n  After setup: enter Play Mode — CharacterMover will select the body child Animator automatically.");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Diagnose Character Animation",
            "Report written to the Console window.\n\nSee the Console for details.", "OK");
    }

    private static string GetPath(Transform root, Transform target)
    {
        if (target == null) return "(null)";
        if (target == root) return "";

        var parts = new System.Collections.Generic.List<string>();
        var current = target;
        while (current != null && current != root)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }
        return string.Join("/", parts);
    }

    private static Transform FindTransformByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }
}
