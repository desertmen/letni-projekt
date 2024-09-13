using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateDash : PlayerState
{
    private Rigidbody2D body;
    private Vector2 velocity;
    private float duration;
    public PlayerStateDash(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        body = stateMachine.GetComponent<Rigidbody2D>();
    }

    public override void enter()
    {
        velocity = Vector2.right * stateMachine.getForwardDir() * stateMachine.getDashVelocity();
        duration = stateMachine.getDashDuration();

        body.velocity = velocity;
    }

    public override void exit()
    {
        duration = 0;
        return;
    }

    public override void onCollisionEnter(Collision2D collision)
    {
        stateMachine.changeState(stateMachine.idleState);
    }

    public override void update(float deltaTime, float direction)
    {
        if (duration <= 0)
        {
            stateMachine.changeState(stateMachine.idleState);
            return;
        }

        body.gravityScale = 0;
        duration -= deltaTime;
        body.velocity = velocity;
    }
}
