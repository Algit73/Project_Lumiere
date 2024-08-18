using System.Collections;
using UnityEngine;

public class Interactive_Anim : Interactive
{
    [SerializeField] private Animator animator;
    [SerializeField] private string defaultAnimation;

    protected Animator ObjectAnimator => animator;

    private void PlayAnim()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(DataStringValue))
        {
            animator.Play(DataStringValue);
            StartCoroutine(CheckAnimation(DataStringValue));
        }
    }
    private void TriggerAnim()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(DataStringValue))
        {
            animator.Play(DataStringValue);
            IsWorking = false;
        }
    }

    public override void Action()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(defaultAnimation))
        {
            animator.Play(defaultAnimation);
            StartCoroutine(CheckAnimation(defaultAnimation));
        }
    }
    public override void ResetObject()
    {
        animator.Play(defaultAnimation);
    }

    private IEnumerator CheckAnimation(string stateName)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) yield return null;

        IsWorking = false;
    }
}