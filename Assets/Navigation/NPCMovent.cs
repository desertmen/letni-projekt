using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent (typeof(CircleCollider2D))]
public class NPCMovent : Movement
{
    [SerializeField] private float _Acceleration;
    [SerializeField] private float _Deceleration;
    [SerializeField] private float _MaxRunSpeed;
    [SerializeField] private Vector2 _MaxJump;

    [SerializeField][Range(0, 89)] private float _MaxAngle;

    private Rigidbody2D body;
    private CircleCollider2D circleCollider;
    private State state;
    private Action<Polygon> onLanding = null;
    private Action<Polygon> onGrounded = null;
    private Vector2 size;
    private Vector2 lastVelocity = Vector2.zero;
    private Polygon currPolygon;

    // Start is called before the first frame update
    void Start()
    {
        state = State.JUMPING;
        body = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        size = GetComponent<SpriteRenderer>().bounds.size;
    }

    // Update is called once per frame
    void Update()
    {
        if(state == State.JUMPING && body.velocity.y == 0 && lastVelocity.y == 0)
        {
            tryChangeState(State.IDLE);
        }
        lastVelocity = body.velocity;

        move();
        //debugChangeCollor();
    }

    private void debugChangeCollor()
    {
        switch (state)
        {
            case State.IDLE:
                GetComponent<SpriteRenderer>().color = Color.white; break;
            case State.STOPPING:
                GetComponent<SpriteRenderer>().color = Color.red; break;
            case State.MOVING_LEFT:
                GetComponent<SpriteRenderer>().color = Color.blue; break;
            case State.MOVING_RIGHT:
                GetComponent<SpriteRenderer>().color = Color.green; break;
            case State.JUMPING:
                GetComponent<SpriteRenderer>().color = Color.yellow; break;
        }
    }

    private Vector2 getSlopeNormal()
    {
        if(currPolygon == null)
        {
            return Vector2.up;
        }
        Vector2 pos = circleCollider.bounds.center;
        Vector2 extents = circleCollider.bounds.extents;
        float minDist = float.MaxValue;
        Vector2 closest = Vector2.positiveInfinity;
        for (int i = 0; i < currPolygon.points.Count; i++)
        {
            Vector2 edge1 = currPolygon.points[i];
            Vector2 edge2 = currPolygon.points[(i + 1) % currPolygon.points.Count];

            if(MyUtils.Math.intervalsOverlap((edge1.x, edge2.x), (pos.x - extents.x, pos.x + extents.x)))
            {
                Vector2 projection = MyUtils.Math.projectPointOnLine(edge1, edge2, pos);
                float dist = Vector2.Distance(projection, pos);
                if (dist >= extents.x && projection.y <= pos.y && dist < minDist)
                {
                    minDist = dist;
                    closest = projection; ;
                }
            }
        }
        if (closest.x == float.PositiveInfinity)
            return Vector2.up;

        return (pos - closest).normalized;
    }

    private Vector2 getSlopeDirRight()
    {
        Vector2 normal = getSlopeNormal();
        return new Vector2(normal.y, -normal.x);
    }

    private Vector2 getSlopeDirLeft()
    {
        Vector2 normal = getSlopeNormal();
        return new Vector2(-normal.y, normal.x);
    }

    private Vector2 getSlopeDirDown()
    {
        Vector2 normal = getSlopeNormal();
        if(normal.x > 0)
        {
            return new Vector2(normal.y, -normal.x);
        }
        else
        {
            return new Vector2(-normal.y, normal.x);
        }
    }

    private void move()
    { 
        float time = Time.deltaTime;
        float speed, speedChange;
        switch(state)
        {
            case State.STOPPING:
                speedChange = _Deceleration * time;
                speed = speedChange > body.velocity.magnitude ? 0 : body.velocity.magnitude - speedChange * Mathf.Sign(body.velocity.x);
                body.velocity = speed * body.velocity.normalized;
                break;
            case State.MOVING_RIGHT:
                speedChange = body.velocity.x < 0 ? _Deceleration * time : _Acceleration * time;
                Vector2 velocityR = body.velocity + getSlopeDirRight() * speedChange;
                velocityR = velocityR.magnitude > _MaxRunSpeed ? velocityR.normalized * _MaxRunSpeed : velocityR;
                body.velocity = velocityR;
                break;
            case State.MOVING_LEFT:
                speedChange = body.velocity.x > 0 ? _Deceleration * time : _Acceleration * time;
                Vector2 velocityL = body.velocity + getSlopeDirLeft() * speedChange;
                velocityL = velocityL.magnitude > _MaxRunSpeed ? velocityL.normalized * _MaxRunSpeed : velocityL;
                body.velocity = velocityL;
                break;
            case State.SLIDING:
                speedChange = _Acceleration * time;
                Vector2 velocityD = body.velocity + getSlopeDirDown() * speedChange;
                velocityD = velocityD.magnitude > _MaxRunSpeed ? velocityD.normalized * _MaxRunSpeed : velocityD;
                body.velocity = velocityD;
                break;  
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!(collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform) &&
              collision.transform.TryGetComponent<PolygonReference>(out PolygonReference polygonReference)))
            return;

        currPolygon = polygonReference.polygon;
        Vector2 pos = circleCollider.bounds.center;
        int mask = 1 << MyUtils.Constants.Layers.Platform;
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, circleCollider.bounds.size.y * 2, mask);

        // TODO hit.collider != null instead of just hit
        if (hit && hit.transform == collision.transform)
        {
            tryChangeState(State.IDLE);
            if(onLanding != null)
            {
                onLanding(polygonReference.polygon);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!(collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform) &&
            collision.transform.TryGetComponent<PolygonReference>(out PolygonReference polygonReference)) ||
            state == State.JUMPING)
            return;
        if(onGrounded != null)
            onGrounded(polygonReference.polygon);
    }

    public float getMaxAngle()
    {
        return _MaxAngle;
    }

    public Vector2 getSize()
    {
        return size;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            currPolygon = null;
            tryChangeState(State.JUMPING);
        }
    }

    public override void jump(Vector2 jumpVelocity)
    {
        if (tryChangeState(State.JUMPING))
        {
            body.velocity = jumpVelocity;
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
        tryChangeState(State.STOPPING);
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

    public void setActionOnGrounded(Action<Polygon> action)
    {
        onGrounded = action;
    }

    
    public Vector2 getMaxJump() { return _MaxJump; }
    public float getMaxRunningSpeed() { return _MaxRunSpeed; }
}
