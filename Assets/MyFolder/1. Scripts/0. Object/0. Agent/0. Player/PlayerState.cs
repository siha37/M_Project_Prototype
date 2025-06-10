using UnityEngine;

public class PlayerState : AgentState
{
    public const int reviveCount = 3;
    public const float reviveDelay = 2f;
    public const float reviveRange = 5f;
    public int reviveCurrentCount;

    protected override void Start()
    {
        base.Start();
        reviveCurrentCount = reviveCount;
    }
}
