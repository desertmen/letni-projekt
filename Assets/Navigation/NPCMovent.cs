using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent (typeof(BoxCollider2D))]
public class NPCMovent : Movement
{
    [SerializeField] private float _Acceleration;
    [SerializeField] private float _Deceleration;
    [SerializeField] private float _MaxRunSpeed;
    [SerializeField] private Vector2 _MaxJump;

    [SerializeField][Range(0, 89)] private float _MaxAngle;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private State state;
    private Vector2 size;
    private Action<Polygon> onLanding = null;
    private Vector2 slope = Vector2.right;
    private Polygon currPolygon = null;

    // TODO- landing timer for undetected landing check
    private float falseJumpingTime = 0;
    private float maxFalseJumpingTime = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        state = State.JUMPING;
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();   
        size = boxCollider.bounds.size;
    }

    // Update is called once per frame
    void Update()
    {
        tryToEndJump();

        move();

        switch (state)
        {
            case State.IDLE:
                GetComponent<SpriteRenderer>().color = Color.white; break;
            case State.STOPPED:
                GetComponent<SpriteRenderer>().color = Color.red; break;
            case State.MOVING_LEFT:
                GetComponent<SpriteRenderer>().color = Color.blue; break;
            case State.MOVING_RIGHT:
                GetComponent<SpriteRenderer>().color = Color.green; break;
            case State.JUMPING:
                GetComponent<SpriteRenderer>().color = Color.yellow; break;
        }
    }

    private void tryToEndJump()
    {
        if (body.velocity.y == 0 && state == State.JUMPING)
        {
            falseJumpingTime += Time.deltaTime;
        }
        else
        {
            falseJumpingTime = 0;
        }
        if (falseJumpingTime > maxFalseJumpingTime && state == State.JUMPING)
        {
            tryChangeState(State.IDLE);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + 2*Vector3.right);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + 2*slope);
        Gizmos.color = Color.red;
        if(body != null)
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + 2 * body.velocity.normalized);
    }

    private void move()
    { 
        // TODO - make npc walk on slopes
        float time = Time.deltaTime;
        float xSpeed, speedChange;

        switch(state)
        {
            case State.STOPPED:
                speedChange = _Deceleration * time;
                xSpeed = speedChange > Mathf.Abs(body.velocity.x) ? 0 : body.velocity.x - speedChange * Mathf.Sign(body.velocity.x);
                body.velocity = new Vector2(xSpeed, body.velocity.y);
                break;
            case State.MOVING_RIGHT:
                speedChange = body.velocity.x < 0 ? _Deceleration * time : _Acceleration * time;
                xSpeed = Mathf.Min(_MaxRunSpeed, body.velocity.x + speedChange);
                Vector2 dirR = slope.y >= 0 ? slope : Vector2.right;
                body.velocity = xSpeed * dirR;
                break;
            case State.MOVING_LEFT:
                speedChange = body.velocity.x > 0 ? _Deceleration * time : _Acceleration * time;
                xSpeed = Mathf.Max(-_MaxRunSpeed, body.velocity.x - speedChange);
                Vector2 dirL = slope.y <= 0 ? slope : Vector2.right;
                body.velocity = xSpeed * dirL;
                break;
            case State.JUMPING: 
                break;
            case State.IDLE:
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RaycastHit2D[] results = new RaycastHit2D[1];
        Debug.Log("HIT COUNT: " + boxCollider.Cast(Vector2.down, results, 0.1f));
        
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.platform) && 
            collision.transform.TryGetComponent<PolygonReference>(out PolygonReference polygonReference) &&
            boxCollider.Cast(Vector2.down, results, 0.2f) > 0) // polygon is touching bottom of box
        {
            currPolygon = polygonReference.polygon;
            tryChangeState(State.IDLE);
            if(onLanding != null)
            {
                onLanding(polygonReference.polygon);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.platform))
        {
            tryChangeState(State.JUMPING);
            currPolygon = null;
        }
    }

    public override void jump(Vector2 jumpVelocity)
    {
        if (tryChangeState(State.JUMPING))
        {
            body.velocity = jumpVelocity;
            Debug.Log("Jump velocity: " + jumpVelocity);
        }
    }

    public override void startGoingLeft()
    {
        tryChangeState(State.MOVING_LEFT);
    }

    public override void startGoingRight()
    {
        tryChangeState(State.MOVING_RIGHT);
    }

    public override void stopMoving()
    {
        tryChangeState(State.STOPPED);
    }

    private bool tryChangeState(State nextState)
    {
        if(state == nextState)
            return false;

        if (state == State.JUMPING && nextState != State.IDLE)
            return false;
        
        state = nextState;
        return true;
    }

    public override bool isGrounded()
    {
        return state != State.JUMPING;
    }

    public override void setActionOnLanding(Action<Polygon> action)
    {
        onLanding = action;
    }

    public Vector2 getMaxJump() { return _MaxJump; }
    public float getMaxRunningSpeed() { return _MaxRunSpeed; }
}
