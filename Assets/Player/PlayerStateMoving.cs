using UnityEngine;

public class PlayerStateMoving : PlayerState
{
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;

    private float maxDeccelerationT = 0;
    private float maxAccelerationT = 0;
    private float movementTime;
    private float direction = 0;

    public PlayerStateMoving(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        body = stateMachine.transform.GetComponent<Rigidbody2D>();
        boxCollider = stateMachine.transform.GetComponent<BoxCollider2D>();
    }

    public override void enter()
    {
        direction = 0;
        maxDeccelerationT = stateMachine.getMaxDeccelerationT();
        maxAccelerationT = stateMachine.getMaxAccelerationT();
    }

    public override void exit()
    {
        direction = 0;
    }


    public override void update(float deltaTime, float direction)
    {
        if(direction == 0)
        {
            stateMachine.changeState(stateMachine.idleState);
            return;
        }
        
        move(direction);
        this.direction = direction;
    }
    public override void onCollisionEnter(Collision2D collision)
    {
        return;
    }

    private void move(float newDirection)
    {
        Vector2 velocity = body.velocity;
        float speedPerc = Mathf.Abs(velocity.x) / stateMachine.getMaxVelocity();

        // slide on walls
        if (Mathf.Abs(body.velocity.x) < stateMachine.getAccelerationValue(Time.deltaTime) * stateMachine.getMaxVelocity())
        {
            Vector2 dir = newDirection > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D[] hits = new RaycastHit2D[2];
            boxCollider.Cast(dir, hits, 0.05f);

            if (hits[0].distance > 0)
            {
                // dont run into wall (othwervise hovering)
                return;
            }
            else if (body.velocity.x == 0)
            {
                // move slightly up - solves getting stuck corner to corner (Cast doesnt see it)
                stateMachine.transform.position = new Vector3(stateMachine.transform.position.x, stateMachine.transform.position.y + 0.01f, stateMachine.transform.position.z);
            }
        }

        // changed to accelerating
        if (direction != newDirection && Mathf.Sign(newDirection) == Mathf.Sign(body.velocity.x))
        {
            movementTime = stateMachine.getAccelerationTime(speedPerc);
        }
        // changed to deccelerating
        else if (direction != newDirection)
        {
            movementTime = -maxDeccelerationT + stateMachine.getDecelerationTime(speedPerc);
        }
        movementTime += Time.deltaTime * stateMachine.getMovementTimeMult();

        float xSpeed;
        if (movementTime < 0)
        {
            xSpeed = stateMachine.getDecelerationValue(maxDeccelerationT + movementTime) * -Mathf.Sign(newDirection) * stateMachine.getMaxVelocity();
        }
        else if (movementTime < maxAccelerationT)
        {
            xSpeed = stateMachine.getAccelerationValue(movementTime) * Mathf.Sign(newDirection) * stateMachine.getMaxVelocity();
        }
        else
        {
            xSpeed = Mathf.Sign(newDirection) * stateMachine.getMaxVelocity();
        }
        body.velocity = new Vector2(xSpeed, body.velocity.y);
    }
}
