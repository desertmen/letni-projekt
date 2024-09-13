using UnityEngine;

public abstract class PlayerState
{
    protected PlayerStateMachine stateMachine;
    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
    public abstract void enter();
    public abstract void exit();
    public abstract void update(float deltaTime, float direction);
    public abstract void onCollisionEnter(Collision2D collision);
}
