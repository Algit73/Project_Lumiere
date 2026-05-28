using UnityEngine;

/// <summary>
/// DROP THIS on the cat's root GameObject.
/// It finds the Animator anywhere in the hierarchy, then cycles through
/// the cat animation clips one by one so you can confirm which child
/// has a working Animator and which clips play.
///
/// In Play mode, watch the Console — it will print exactly which Animator
/// it found and on which GameObject.
/// Press Space to switch to the next clip.
/// </summary>
public class CatAnimTest : MonoBehaviour
{
    [Header("Drag cat clips here manually")]
    public AnimationClip[] clips;

    [Header("Status (read-only in Play mode)")]
    [SerializeField] private string _foundAnimatorOn = "searching...";
    [SerializeField] private string _currentClip     = "none";

    private Animator   _anim;
    private int        _clipIndex;

    private void Start()
    {
        // Search the entire hierarchy
        Animator[] all = GetComponentsInChildren<Animator>(includeInactive: true);

        if (all.Length == 0)
        {
            Debug.LogError($"[CatAnimTest] No Animator found anywhere under '{name}'. " +
                           "The cat has no Animator component at all.");
            _foundAnimatorOn = "NONE FOUND";
            enabled = false;
            return;
        }

        // Pick the one with an Avatar (body rig), or just the first one
        _anim = System.Array.Find(all, a => a.avatar != null) ?? all[0];
        _foundAnimatorOn = _anim.gameObject.name;

        Debug.Log($"[CatAnimTest] Found Animator on child: '{_foundAnimatorOn}'" +
                  $" | Avatar: {(_anim.avatar != null ? _anim.avatar.name : "NULL")}" +
                  $" | Controller: {(_anim.runtimeAnimatorController != null ? _anim.runtimeAnimatorController.name : "NULL — assign one!")}");

        // Play the first clip if clips are assigned
        PlayCurrentClip();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _clipIndex = (_clipIndex + 1) % Mathf.Max(clips.Length, 1);
            PlayCurrentClip();
        }

        // Also move the cat forward while a non-idle clip is active so
        // you can see if movement works independently of CharacterMover.
        if (_anim != null && clips.Length > 0 && _clipIndex > 0)
            transform.position += transform.forward * 1.5f * Time.deltaTime;
    }

    private void PlayCurrentClip()
    {
        if (_anim == null) return;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[CatAnimTest] No clips assigned. Drag Cat_Idle / Cat_Walk / Cat_Run into the Clips array.");
            return;
        }

        var clip = clips[_clipIndex];
        if (clip == null) return;

        _currentClip = clip.name;
        _anim.Play(clip.name, 0, 0f);
        Debug.Log($"[CatAnimTest] Playing clip: {clip.name}");
    }
}
