using UnityEngine;

public class StoryTimeDriver : MonoBehaviour
{
    public DayNightCycle2D dayNight;

    // Example: speed up time during a chase
    public void StartChase()
    {
        dayNight.autoAdvanceInGame = true;
        dayNight.timeScale = 3f;
    }

    // Example: freeze time during a boss intro
    public void BossIntro()
    {
        dayNight.autoAdvanceInGame = false;
    }

    // Example: jump time forward after completing an objective
    public void ObjectiveCompleteJumpToEvening()
    {
        dayNight.SetTime01(0.55f); // evening-ish
    }

    // Example: force it to night and end the day
    public void EndDayNow()
    {
        dayNight.LockAtNightNow(); // clamps to nightLockTime01 and stops auto-advance
    }
}
