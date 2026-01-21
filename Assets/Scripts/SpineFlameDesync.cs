using UnityEngine;
using Spine.Unity;
using Spine;


public class SpineFlameDesync : MonoBehaviour
{
  
    public int trackIndex = 0;
    public Vector2 timeScaleRange = new Vector2(0.95f, 1.10f);
    public bool randomizeOnEnable = true;
    public bool waitOneFrame = true;

    void OnEnable()
    {
        if (randomizeOnEnable) StartCoroutine(RandomizeNextFrame());
    }

    System.Collections.IEnumerator RandomizeNextFrame()
    {
        if (waitOneFrame) yield return null; // let other scripts set the animation first
        RandomizeNow();
    }

    [ContextMenu("Randomize Now")]
    public void RandomizeNow()
    {
        // Grab whichever Spine component is present.
        var sa = GetComponent<SkeletonAnimation>();
        var sg = GetComponent<SkeletonGraphic>();
        Spine.AnimationState state = sa ? sa.AnimationState : (sg ? sg.AnimationState : null);
        if (state == null) return;

        // If there’s a current entry on the chosen track, offset its phase and speed.
        var entry = state.GetCurrent(trackIndex);
        if (entry != null && entry.Animation != null)
        {
            float dur = Mathf.Max(0.0001f, entry.Animation.Duration);
            entry.TrackTime = Random.Range(0f, dur);                           // random phase
            entry.TimeScale = Random.Range(timeScaleRange.x, timeScaleRange.y); // per-entry speed
        }
        else
        {
            // Fallback: tweak the whole state's speed if no entry yet.
            state.TimeScale = Random.Range(timeScaleRange.x, timeScaleRange.y);
        }
    }
}
