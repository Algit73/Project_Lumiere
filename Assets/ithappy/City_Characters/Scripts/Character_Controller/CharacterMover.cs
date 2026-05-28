using System;
using UnityEngine;

namespace Controller
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class CharacterMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float m_WalkSpeed = 1.5f;
        [SerializeField]
        private float m_RunSpeed = 4f;
        [SerializeField, Range(0f, 360f)]
        private float m_RotateSpeed = 90f;
        [SerializeField]
        private Space m_Space = Space.Self;
        [SerializeField]
        private float m_JumpHeight = 5f;

        [Header("Capsule")]
        [Tooltip("Enable for humanoid characters whose pivot is at their feet. Disable for animals/non-humanoids whose pivot is at body center.")]
        [SerializeField]
        private bool m_AutoCorrectCapsuleCenter = true;

        [Header("Animator")]
        [SerializeField]
        private string m_HorizontalID = "Hor";
        [SerializeField]
        private string m_VerticalID = "Vert";
        [SerializeField]
        private string m_StateID = "State";
        [SerializeField]
        private string m_JumpID = "IsJump";
        [SerializeField]
        private AnimatorControlMode m_AnimatorControlMode = AnimatorControlMode.Auto;
        [SerializeField, Range(0f, 1f)]
        private float m_WalkAnimScale = 0.45f;
        [SerializeField, Range(0f, 1f)]
        private float m_RunAnimScale = 1f;
        [SerializeField]
        private LookWeight m_LookWeight = new(1f, 0.3f, 0.7f, 1f);

        private Transform m_Transform;
        private CharacterController m_Controller;
        private Animator m_Animator;

        private MovementHandler m_Movement;
        private AnimationHandler m_Animation;

        private Vector2 m_Axis;
        private Vector3 m_Target;
        private bool m_IsRun;
        private bool m_IsJump;

        private bool m_IsMoving;

        public Vector2 Axis => m_Axis;
        public Vector3 Target => m_Target;
        public bool IsRun => m_IsRun;

        private void OnValidate()
        {
            m_WalkSpeed = Mathf.Max(m_WalkSpeed, 0f);
            m_RunSpeed = Mathf.Max(m_RunSpeed, m_WalkSpeed);
            m_WalkAnimScale = Mathf.Clamp01(m_WalkAnimScale);
            m_RunAnimScale = Mathf.Clamp01(m_RunAnimScale);

            m_Movement?.SetStats(m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);

            // Clamp stepOffset so Unity's CharacterController constraint is satisfied
            // even when the parent GameObject is scaled down (e.g. to 0.005 for XR scenes).
            // OnValidate fires before Play mode starts, so this prevents the log error.
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 s = transform.lossyScale;
                float scaledHeight = cc.height * s.y;
                float scaledRadius = cc.radius * Mathf.Max(s.x, s.z);
                float maxStep = scaledHeight + scaledRadius * 2f;
                if (cc.stepOffset > maxStep)
                    cc.stepOffset = Mathf.Max(0f, maxStep * 0.25f);
            }
        }

        private void Awake()
        {
            m_Transform = transform;
            m_Controller = GetComponent<CharacterController>();
            AnimatorControlMode animatorControlMode = ResolveAnimatorControlMode();
            m_Animator = ResolveAnimator(animatorControlMode);
            if (m_Animator == null)
                Debug.LogWarning($"[CharacterMover] No Animator found on '{name}' or any child. " +
                                 "Add an Animator component to each child mesh piece and assign Pumped_Movement controller.", this);
            else
            {
                NormalizeAnimatorHierarchy(m_Animator, animatorControlMode);
                Debug.Log($"[CharacterMover] Animator found: '{m_Animator.gameObject.name}'", this);
            }

            // Root cause fix: if center.y is 0 the capsule is centered at the pivot,
            // so Unity sits the capsule BOTTOM on the ground and the pivot (character
            // feet) floats at height/2 above the floor.
            // Only applies to humanoid characters (pivot at feet). Disable for animals.
            if (m_AutoCorrectCapsuleCenter && Mathf.Approximately(m_Controller.center.y, 0f))
            {
                Vector3 c = m_Controller.center;
                c.y = m_Controller.height * 0.5f;
                m_Controller.center = c;
            }

            // Clamp stepOffset to satisfy Unity's constraint:
            // stepOffset <= scaledHeight + scaledRadius * 2
            // (stepOffset is world-space; the error message confirms "scaled" values)
            {
                Vector3 s = transform.lossyScale;
                float scaledHeight = m_Controller.height * s.y;
                float scaledRadius = m_Controller.radius * Mathf.Max(s.x, s.z);
                float maxStep = scaledHeight + scaledRadius * 2f;
                if (m_Controller.stepOffset > maxStep)
                    m_Controller.stepOffset = maxStep * 0.25f;
            }

            m_Movement = new MovementHandler(m_Controller, m_Transform, m_WalkSpeed, m_RunSpeed, m_RotateSpeed, m_JumpHeight, m_Space);
            m_Animation = new AnimationHandler(m_Animator, m_HorizontalID,  m_VerticalID, m_StateID, m_JumpID);
        }

        private Animator ResolveAnimator(AnimatorControlMode controlMode)
        {
            var animators = GetComponentsInChildren<Animator>(true);
            if (animators == null || animators.Length == 0)
                return null;

            if (controlMode == AnimatorControlMode.RootAnimator)
            {
                Animator rootAnimator = GetComponent<Animator>();
                if (rootAnimator != null)
                    return rootAnimator;
            }

            Animator best = null;
            int bestScore = int.MinValue;

            foreach (var animator in animators)
            {
                int score = ScoreAnimator(animator, controlMode);
                if (score > bestScore)
                {
                    best = animator;
                    bestScore = score;
                }
            }

            return best;
        }

        private int ScoreAnimator(Animator animator, AnimatorControlMode controlMode)
        {
            if (animator == null)
                return int.MinValue;

            int score = 0;
            bool isRootAnimator = animator.transform == transform;
            bool hasDirectSkin = animator.GetComponent<SkinnedMeshRenderer>() != null;
            bool hasBodyName = animator.gameObject.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0;

            if (controlMode == AnimatorControlMode.BodyAnimator && isRootAnimator)
                score -= 2000;

            if (controlMode == AnimatorControlMode.RootAnimator && isRootAnimator)
                score += 2000;
            else if (isRootAnimator)
                score += 400;
            else
                score += 100;

            if (hasBodyName)
                score += 1000;

            if (hasDirectSkin)
                score += 500;

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

        private void NormalizeAnimatorHierarchy(Animator selectedAnimator, AnimatorControlMode controlMode)
        {
            bool useAnimatorSync = controlMode == AnimatorControlMode.SyncedChildren || GetComponent<MultiAnimatorSync>() != null;
            Animator sourceAnimator = ResolveAnimationSourceAnimator(selectedAnimator);
            RuntimeAnimatorController fallbackController = sourceAnimator != null ? sourceAnimator.runtimeAnimatorController : selectedAnimator.runtimeAnimatorController;
            Avatar fallbackAvatar = sourceAnimator != null ? sourceAnimator.avatar : selectedAnimator.avatar;

            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                if (fallbackController == null && animator.runtimeAnimatorController != null)
                    fallbackController = animator.runtimeAnimatorController;

                if (fallbackAvatar == null && animator.avatar != null)
                    fallbackAvatar = animator.avatar;
            }

            if (ShouldApplySourceController(selectedAnimator, sourceAnimator) && fallbackController != null)
                selectedAnimator.runtimeAnimatorController = fallbackController;
            else if (selectedAnimator.runtimeAnimatorController == null && fallbackController != null)
                selectedAnimator.runtimeAnimatorController = fallbackController;

            if (ShouldApplySourceAvatar(selectedAnimator, sourceAnimator) && fallbackAvatar != null)
                selectedAnimator.avatar = fallbackAvatar;
            else if (selectedAnimator.avatar == null && fallbackAvatar != null)
                selectedAnimator.avatar = fallbackAvatar;

            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null && fallbackController != null)
                    animator.runtimeAnimatorController = fallbackController;

                if (animator.avatar == null && fallbackAvatar != null)
                    animator.avatar = fallbackAvatar;

                animator.applyRootMotion = false;

                if (useAnimatorSync)
                {
                    animator.enabled = true;
                    continue;
                }

                animator.enabled = animator == selectedAnimator;
            }
        }

        private Animator ResolveAnimationSourceAnimator(Animator selectedAnimator)
        {
            Animator best = null;
            int bestScore = int.MinValue;

            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                int score = 0;

                if (animator == selectedAnimator)
                    score += 50;

                if (animator.gameObject.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1000;

                if (animator.GetComponent<SkinnedMeshRenderer>() != null)
                    score += 600;

                if (animator.avatar != null)
                    score += 200;

                if (animator.runtimeAnimatorController != null)
                    score += 200;

                if (animator.transform == transform)
                    score -= 500;

                if (score > bestScore)
                {
                    best = animator;
                    bestScore = score;
                }
            }

            return best;
        }

        private AnimatorControlMode ResolveAnimatorControlMode()
        {
            if (m_AnimatorControlMode != AnimatorControlMode.Auto)
            {
                if (m_AnimatorControlMode == AnimatorControlMode.RootAnimator && RequiresSynchronizedChildAnimators())
                    return AnimatorControlMode.SyncedChildren;

                return m_AnimatorControlMode;
            }

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                string childName = child.name;
                if (childName.IndexOf("Senior", StringComparison.OrdinalIgnoreCase) >= 0)
                    return RequiresSynchronizedChildAnimators() ? AnimatorControlMode.SyncedChildren : AnimatorControlMode.BodyAnimator;

                if (childName.IndexOf("Adult", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AnimatorControlMode.RootAnimator;

                if (childName.IndexOf("Pumped", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AnimatorControlMode.BodyAnimator;

                if (childName.IndexOf("Teen", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AnimatorControlMode.BodyAnimator;
            }

            return AnimatorControlMode.BodyAnimator;
        }

        private bool RequiresSynchronizedChildAnimators()
        {
            int animatedChildCount = 0;
            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                if (animator == null || animator.transform == transform)
                    continue;

                if (animator.GetComponent<SkinnedMeshRenderer>() == null)
                    continue;

                animatedChildCount++;
                if (animatedChildCount > 1)
                    return true;
            }

            return false;
        }

        private static bool ShouldApplySourceController(Animator selectedAnimator, Animator sourceAnimator)
        {
            return sourceAnimator != null
                && sourceAnimator != selectedAnimator
                && selectedAnimator.transform.parent == null
                && selectedAnimator.GetComponent<SkinnedMeshRenderer>() == null;
        }

        private static bool ShouldApplySourceAvatar(Animator selectedAnimator, Animator sourceAnimator)
        {
            return sourceAnimator != null
                && sourceAnimator != selectedAnimator
                && selectedAnimator.transform.parent == null
                && selectedAnimator.GetComponent<SkinnedMeshRenderer>() == null;
        }

        private void Update()
        {
            m_Movement.Move(Time.deltaTime, in m_Axis, in m_Target, m_IsRun, m_IsJump, m_IsMoving,
                m_IsRun ? m_RunAnimScale : m_WalkAnimScale,
                out var animAxis, out var isAir);
            if (m_Animator != null)
                m_Animation.Animate(in animAxis, m_IsRun? 1f : 0f, isAir, Time.deltaTime);

        }

        private void OnAnimatorIK()
        {
            m_Animation.AnimateIK(in m_Target, m_LookWeight);
        }

        public void SetInput(in Vector2 axis, in Vector3 target, in bool isRun, in bool isJump)
        {
            m_Axis = axis;
            m_Target = target;
            m_IsRun = isRun;
            m_IsJump = isJump;

            if (m_Axis.sqrMagnitude < Mathf.Epsilon)
            {
                m_Axis = Vector2.zero;
                m_IsMoving = false;
            }
            else
            {
                m_Axis = Vector3.ClampMagnitude(m_Axis, 1f);
                m_IsMoving = true;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.normal.y > m_Controller.stepOffset)
            {
                m_Movement.SetSurface(hit.normal);
            }
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight;
            public float body;
            public float head;
            public float eyes;

            public LookWeight(float weight, float body, float head, float eyes)
            {
                this.weight = weight;
                this.body = body;
                this.head = head;
                this.eyes = eyes;
            }
        }

        private enum AnimatorControlMode
        {
            Auto,
            RootAnimator,
            BodyAnimator,
            SyncedChildren
        }

        #region Handlers
        private class MovementHandler
        {
            private readonly CharacterController m_Controller;
            private readonly Transform m_Transform;

            private float m_WalkSpeed;
            private float m_RunSpeed;
            private float m_RotateSpeed;
            private float m_JumpHeight;

            private Space m_Space;

            private readonly float m_Luft = 5f;
            private readonly float m_JumpReload = 1f;

            private float m_TargetAngle;
            private bool m_IsRotating = false;

            private Vector3 m_Normal;
            private Vector3 m_GravityAcelleration = Physics.gravity;

            private float m_jumpTimer;

            public MovementHandler(CharacterController controller, Transform transform, float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_Controller = controller;
                m_Transform = transform;

                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_JumpHeight = jumpHeight;

                m_Space = space;
            }

            public void SetStats(float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_JumpHeight = jumpHeight;

                m_Space = space;
            }

            public void SetSurface(in Vector3 normal)
            {
                m_Normal = normal;
            }

            public void Move(float deltaTime, in Vector2 axis, in Vector3 target, bool isRun, bool isJump, bool isMoving,
                float animScale, out Vector2 animAxis, out bool isAir)
            {
                var targetForward = Vector3.Normalize(target - m_Transform.position);

                ConvertMovement(in axis, in targetForward, out var movement);
                CaculateGravity(isJump, deltaTime, out isAir);
                Displace(deltaTime, in movement, isRun);
                Turn(in targetForward, isMoving);
                UpdateRotation(deltaTime);

                GenAnimationAxis(in movement, animScale, out animAxis);
            }

            private void ConvertMovement(in Vector2 axis, in Vector3 targetForward, out Vector3 movement)
            {
                Vector3 forward;
                Vector3 right;

                if (m_Space == Space.Self)
                {
                    forward = new Vector3(targetForward.x, 0f, targetForward.z).normalized;
                    right = Vector3.Cross(Vector3.up, forward).normalized;
                }
                else
                {
                    forward = Vector3.forward;
                    right = Vector3.right;
                }

                movement = axis.x * right + axis.y * forward;
                movement = Vector3.ProjectOnPlane(movement, m_Normal);
            }

            private void Displace(float deltaTime, in Vector3 movement, bool isRun)
            {
                Vector3 displacement = (isRun ? m_RunSpeed : m_WalkSpeed) * movement;
                displacement += m_GravityAcelleration;
                displacement *= deltaTime;

                m_Controller.Move(displacement);
            }

            private void CaculateGravity(bool isJump, float deltaTime, out bool isAir)
            {
                m_jumpTimer = Mathf.Max(m_jumpTimer - deltaTime, 0f);

                if (m_Controller.isGrounded)
                {
                    if (isJump && m_jumpTimer <= 0)
                    {
                        var gravity = Physics.gravity;
                        var length = gravity.magnitude;

                        m_GravityAcelleration += -(gravity / length) * Mathf.Sqrt(m_JumpHeight * 6f * length);
                        m_jumpTimer = m_JumpReload;
                        isAir = true;

                        return;
                    }

                    m_GravityAcelleration = Physics.gravity;
                    isAir = false;

                    return;
                }

                isAir = true;

                m_GravityAcelleration += Physics.gravity * deltaTime;
                return;
            }

            private void GenAnimationAxis(in Vector3 movement, float animScale, out Vector2 animAxis)
            {
                if(m_Space == Space.Self)
                {
                    animAxis = new Vector2(Vector3.Dot(movement, m_Transform.right), Vector3.Dot(movement, m_Transform.forward));
                }
                else
                {
                    animAxis = new Vector2(Vector3.Dot(movement, Vector3.right), Vector3.Dot(movement, Vector3.forward));
                }

                animAxis *= Mathf.Clamp01(animScale);
            }

            private void Turn(in Vector3 targetForward, bool isMoving)
            {
                var angle = Vector3.SignedAngle(m_Transform.forward, Vector3.ProjectOnPlane(targetForward, Vector3.up), Vector3.up);

                if (!m_IsRotating)
                {
                    if (!isMoving && Mathf.Abs(angle) < m_Luft)
                    {
                        m_IsRotating = false;
                        return;
                    }

                    m_IsRotating = true;
                }

                m_TargetAngle = angle;
            }

            private void UpdateRotation(float deltaTime)
            {
                if(!m_IsRotating)
                {
                    return;
                }

                var rotDelta = m_RotateSpeed * deltaTime;
                if (rotDelta + Mathf.PI * 2f + Mathf.Epsilon >= Mathf.Abs(m_TargetAngle))
                {
                    rotDelta = m_TargetAngle;
                    m_IsRotating = false;
                }
                else
                {
                    rotDelta *= Mathf.Sign(m_TargetAngle);
                }

                m_Transform.Rotate(Vector3.up, rotDelta);
            }
        }

        private class AnimationHandler
        {
            private readonly Animator m_Animator;

            private readonly string m_HorizontalID;
            private readonly string m_VerticalID;
            private readonly string m_StateID;
            private readonly string m_JumpID;

            private readonly float k_InputFlow = 4.5f;

            private float m_FlowState;
            private Vector2 m_FlowAxis;

            public AnimationHandler(Animator animator, string horizontalID, string verticalID, string stateID, string jumpID)
            {
                m_Animator = animator;

                m_HorizontalID = horizontalID;
                m_VerticalID = verticalID;
                m_StateID = stateID;
                m_JumpID = jumpID;
            }

            public void Animate(in Vector2 axis, float state, bool isJump, float deltaTime)
            {

                m_Animator.SetFloat(m_HorizontalID, m_FlowAxis.x);
                m_Animator.SetFloat(m_VerticalID, m_FlowAxis.y);

                m_Animator.SetFloat(m_StateID, Mathf.Clamp01(m_FlowState));
                m_Animator.SetBool(m_JumpID, isJump);

                m_FlowAxis = Vector2.ClampMagnitude(Vector2.MoveTowards(m_FlowAxis, axis, k_InputFlow * deltaTime), 1f);
                m_FlowState = Mathf.MoveTowards(m_FlowState, Mathf.Clamp01(state), k_InputFlow * deltaTime);
            }

            public void AnimateIK(in Vector3 target, in LookWeight lookWeight)
            {
                m_Animator.SetLookAtPosition(target);
                m_Animator.SetLookAtWeight(lookWeight.weight, lookWeight.body, lookWeight.head, lookWeight.eyes);
            }
        }
        #endregion
    }
}