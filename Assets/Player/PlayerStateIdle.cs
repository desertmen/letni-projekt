using UnityEngine;

public class PlayerStateIdle : PlayerState
{
    private float movementTime = 0;
    private float maxDeccelerationT;
    
    private Rigidbody2D body;

    public PlayerStateIdle(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        body = stateMachine.transform.GetComponent<Rigidbody2D>();
    }

    public override void enter()
    {
        maxDeccelerationT = stateMachine.getMaxDeccelerationT();
        // find where on decceleration curve player is and set it as time
        float speedPerc = Mathf.Abs(body.velocity.x) / stateMachine.getMaxVelocity();
        movementTime = -maxDeccelerationT + stateMachine.getDecelerationTime(speedPerc);
    }

    public override void exit()
    {
        return;
    }

    public override void update(float deltaTime, float direction)
    {
        if(direction != 0)
        {
            stateMachine.changeState(stateMachine.movingState);
            return;
        }

        stop();
    }
    public override void onCollisionEnter(Collision2D collision)
    {
        return;
    }

    private void stop()
    {
        movementTime += Time.deltaTime * stateMachine.getMovementTimeMult();

        float Xvelocity;
        if (movementTime < 0)
        {
            Xvelocity = stateMachine.getDecelerationValue(maxDeccelerationT + movementTime) * Mathf.Sign(body.velocity.x) * stateMachine.getMaxVelocity();
        }
        else
        {
            Xvelocity = 0;
        }
        body.velocity = new Vector2(Xvelocity, body.velocity.y);
    }
}
