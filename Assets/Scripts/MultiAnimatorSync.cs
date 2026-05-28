using System.Collections.Generic;
using System;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Synchronizes all Animator parameters from a single "master" child Animator
    /// to every other child Animator on a modular character.
    ///
    /// Place this component on the CHARACTER ROOT (same object as CharacterMover).
    ///
    /// How it works:
    ///   - CharacterMover drives the master Animator (first child found, or the one
    ///     you explicitly assign in the Inspector).
    ///   - Every LateUpdate this script reads all float/int/bool parameters from the
    ///     master and copies them to every other child Animator.
    ///   - Each modular piece (body, pants, shirt, beard, shoes…) therefore plays
    ///     the exact same animation state without needing a separate controller script.
    /// </summary>
    [DisallowMultipleComponent]
    public class MultiAnimatorSync : MonoBehaviour
    {
        [Tooltip("The Animator that CharacterMover drives. Leave null to auto-detect " +
                 "(first child Animator found, same as CharacterMover's fallback).")]
        [SerializeField] private Animator m_Master;

        private Animator[] m_Slaves;

        private void Awake()
        {
            var all = GetComponentsInChildren<Animator>(true);

            // Prefer the body animator over a temporary root animator.
            if (m_Master == null && all.Length > 0)
                m_Master = ResolveBestAnimator(all);

            var slaves = new List<Animator>(all.Length);
            foreach (var a in all)
            {
                if (a != m_Master)
                    slaves.Add(a);
            }
            m_Slaves = slaves.ToArray();

            if (m_Master == null)
                Debug.LogWarning("[MultiAnimatorSync] No child Animator found on '" + gameObject.name + "'. " +
                                 "Add an Animator to each child mesh piece and assign the correct movement controller.");
            else
                Debug.Log($"[MultiAnimatorSync] Master: '{m_Master.gameObject.name}', Slaves: {m_Slaves.Length}");
        }

        private Animator ResolveBestAnimator(Animator[] animators)
        {
            Animator best = null;
            int bestScore = int.MinValue;

            foreach (var animator in animators)
            {
                int score = ScoreAnimator(animator);
                if (score > bestScore)
                {
                    best = animator;
                    bestScore = score;
                }
            }

            return best;
        }

        private int ScoreAnimator(Animator animator)
        {
            if (animator == null)
                return int.MinValue;

            int score = 0;

            if (animator.transform != transform)
                score += 100;

            if (animator.gameObject.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0)
                score += 1000;

            if (animator.enabled)
                score += 10;

            if (animator.runtimeAnimatorController != null)
                score += 25;

            if (animator.avatar != null)
                score += 25;

            var smr = animator.GetComponent<SkinnedMeshRenderer>() ?? animator.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null)
            {
                score += 100;

                if (smr.rootBone != null)
                    score += 50;
            }

            score += animator.GetComponentsInChildren<Transform>(true).Length;
            return score;
        }

        /// <summary>
        /// LateUpdate runs after CharacterMover.Update, so the master's parameters
        /// are already set for this frame before we copy them to the slaves.
        /// </summary>
        private void LateUpdate()
        {
            if (m_Master == null || m_Slaves == null || m_Slaves.Length == 0) return;
            if (m_Master.parameterCount == 0) return;

            foreach (var param in m_Master.parameters)
            {
                int hash = param.nameHash;
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        float fval = m_Master.GetFloat(hash);
                        foreach (var slave in m_Slaves)
                            slave.SetFloat(hash, fval);
                        break;

                    case AnimatorControllerParameterType.Int:
                        int ival = m_Master.GetInteger(hash);
                        foreach (var slave in m_Slaves)
                            slave.SetInteger(hash, ival);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        bool bval = m_Master.GetBool(hash);
                        foreach (var slave in m_Slaves)
                            slave.SetBool(hash, bval);
                        break;

                    // Triggers are one-shot and auto-consumed; they cannot be reliably
                    // mirrored via GetBool/SetTrigger. If jump-trigger support is
                    // needed, add a separate trigger-forwarding method.
                    case AnimatorControllerParameterType.Trigger:
                        break;
                }
            }
        }
    }
}
