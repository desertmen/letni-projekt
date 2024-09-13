using System.Collections;
using UnityEngine;

public class PlayerStateJump : PlayerState
{
    private Rigidbody2D body;
    private bool jumped = true;

    public PlayerStateJump(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        body = stateMachine.transform.GetComponent<Rigidbody2D>();
    }

    public override void enter()
    {
        jumped = false;
        tryJump();
        stateMachine.changeState(stateMachine.idleState);
    }

    public override void exit()
    {
        return;
    }

    public override void update(float deltaTime, float direction)
    {
        return;
    }

    public override void onCollisionEnter(Collision2D collision)
    {
        return;
    }

    private void jump()
    {
            body.velocity = new Vector2(body.velocity.x, stateMachine.getJumpVelocity());
    }

    private void tryJump()
    {
        if (stateMachine.isGrounded())
        {
            jump();
            jumped = true;
        }
        else
        {
            stateMachine.StartCoroutine(bufferJump(stateMachine.getJumpBufferTime()));
        }
    }

    IEnumerator bufferJump(float bufferTime)
    {
        float t = 0;
        while(t < bufferTime && !jumped)
        {
            if (stateMachine.isGrounded())
            {
                jump();
                jumped = true;
            }
            t += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
}
