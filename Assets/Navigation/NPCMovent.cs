using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NPCNavigator))]
[RequireComponent (typeof(BoxCollider2D))]
public class NPCMovent : Movement
{
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;

    private Rigidbody2D body;
    private State state;
    private float maxRunSpeed;
    private Vector2 size;
    private Action<Polygon> onLanding = null;
    private Vector2 slope = Vector2.right;
    private Polygon currPolygon = null;

    // TODO- landing timer for undetected landing check
    private float falseJumpingTime = 0;
    private float maxFalseJumpingTime = 0.2f;

    private Vector3 lastPosition;
    private float falseStandingTime = 0;
    private float maxFalseStandingTime = 0.2f;
    private float slopeOffsetAngle = 0;

    // Start is called before the first frame update
    void Start()
    {
        state = State.STOPPED;
        body = GetComponent<Rigidbody2D>();
        size = GetComponent<BoxCollider2D>().bounds.size;
        lastPosition = transform.position;
        maxRunSpeed = GetComponent<NPCNavigator>().getMaxRunningSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        if (body.velocity.y == 0 && state == State.JUMPING)
        {
            falseJumpingTime += Time.deltaTime;
        }
        else
        {
            falseJumpingTime = 0;
        }
        if(falseJumpingTime > maxFalseJumpingTime && state == State.JUMPING)
        {
            tryChangeState(State.IDLE);
        }

        if (lastPosition.x == transform.position.x && (state == State.MOVING_RIGHT || state == State.MOVING_LEFT))
        {
            falseStandingTime += Time.deltaTime;
        }
        else
        {
            falseStandingTime = 0;
        }
        if (falseStandingTime > maxFalseStandingTime && (state == State.MOVING_RIGHT || state == State.MOVING_LEFT))
        {
            slopeOffsetAngle += 1;
        }

        updateSlope(currPolygon);
        move();
        lastPosition = transform.position;
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

        if (contactsToDraw != null)
        {
            Gizmos.color = Color.red;
            foreach(var contact in contactsToDraw)
            {
                Gizmos.DrawSphere(contact.point, 0.05f);
            }
        }
    }

    private void move()
    { 
        // TODO - make npc walk on slopes
        float time = Time.deltaTime;
        float xSpeed, speedChange;
        switch(state)
        {
            case State.STOPPED:
                speedChange = deceleration * time;
                xSpeed = speedChange > Mathf.Abs(body.velocity.x) ? 0 : body.velocity.x - speedChange * Mathf.Sign(body.velocity.x);
                body.velocity = new Vector2(xSpeed, body.velocity.y);
                break;
            case State.MOVING_RIGHT:
                speedChange = body.velocity.x < 0 ? deceleration * time : acceleration * time;
                xSpeed = Mathf.Min(maxRunSpeed, body.velocity.x + speedChange);
                Vector2 dirR = slope.y >= 0 ? slope : Vector2.right;
                dirR = MyUtils.Math.rotateVec2AroundOrigin(dirR, slopeOffsetAngle * Mathf.Deg2Rad);
                // rotate counter clockwise by slopeOffsetAngle
                body.velocity = xSpeed * dirR;
                break;
            case State.MOVING_LEFT:
                speedChange = body.velocity.x > 0 ? deceleration * time : acceleration * time;
                xSpeed = Mathf.Max(-maxRunSpeed, body.velocity.x - speedChange);
                Vector2 dirL = slope.y <= 0 ? slope : Vector2.right;
                dirL = MyUtils.Math.rotateVec2AroundOrigin(dirL, -slopeOffsetAngle * Mathf.Deg2Rad);
                // rotate clockwise by slopeOffsetAngle
                body.velocity = xSpeed * dirL;
                break;
            case State.JUMPING: 
                break;
            case State.IDLE:
                break;
        }
    }

    private List<ContactPoint2D> contactsToDraw = null;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.platform) && 
           collision.transform.TryGetComponent<PolygonReference>(out PolygonReference polygonReference))
        {
            List<ContactPoint2D> contacts = new List<ContactPoint2D>();
            collision.GetContacts(contacts);

            slopeOffsetAngle = 0;
            contactsToDraw = contacts;
            
            bool landedOnEdge = true;
            foreach(ContactPoint2D contact in contacts)
            {
                // separation is negative - overlap before fixing positions of colliders
                if (contact.point.y + contact.separation > transform.position.y - size.y / 2f)
                {
                    // TODO - dont allow collision with vertical wall
                    landedOnEdge = false;
                    break;
                }
            }

            if(landedOnEdge)
            {
                currPolygon = polygonReference.polygon;
                tryChangeState(State.IDLE);
                if(onLanding != null)
                {
                    onLanding(polygonReference.polygon);
                }
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

    private void updateSlope(Polygon polygon)
    {
        if (polygon == null)
            return;

        Vector2 BLcorner = (Vector2)transform.position - size/2;
        Vector2 BRcorner = (Vector2)transform.position + new Vector2(size.x/2, -size.y/2);

        List<Edge> edgesUnder = new List<Edge>();

        foreach (Edge edge in polygon.edges) {
            if(polygon.getEdgeNormal(edge).y <= 0)
            {
                continue;
            }
            Vector2[] edgePoints = polygon.getEdgePoints(edge);
            if (MyUtils.Math.intervalsOverlap((edgePoints[0].x, edgePoints[1].x), (BLcorner.x, BRcorner.x)))
            {
                edgesUnder.Add(edge);
            }
        }
        // shouldnt happen
        if (edgesUnder.Count == 0)
        {
            return;
        }
        // only possibly touching one edge -> slope is the edge slope
        else if(edgesUnder.Count == 1)
        {
            Vector2[] edgePoints = polygon.getEdgePoints(edgesUnder[0]);
            slope = (edgePoints[1] - edgePoints[0]).normalized;
        }
        // more edges under - check slope of moving on peak or in valley
        else
        {
            Vector2[] firstEdgePoints = polygon.getEdgePoints(edgesUnder[0]);
            Vector2[] lastEdgePoints = polygon.getEdgePoints(edgesUnder[edgesUnder.Count - 1]);

            // left corner is touching an edge - slope = edgeDir
            if (Vector2.Distance(MyUtils.Math.projectPointOnLine(firstEdgePoints[0], firstEdgePoints[1], BLcorner), BLcorner) < 0.001)
            {
                slope = (firstEdgePoints[1] - firstEdgePoints[0]).normalized;
            }
            // right corner is touching an edge - slope = edgeDir
            else if (Vector2.Distance(MyUtils.Math.projectPointOnLine(lastEdgePoints[0], lastEdgePoints[1], BRcorner), BLcorner) < 0.001)
            {
                slope = (lastEdgePoints[0] - lastEdgePoints[1]).normalized;
            }
            // corners are not touching edge, box is standing on peak
            else
            {
                slope = Vector2.right;
            }
        }
        slope = Mathf.Sign(slope.x) * slope;
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

        if (state == State.JUMPING && nextState != State.IDLE || state == nextState)
            return false;
        
        state = nextState;
        Debug.Log($"NPC movement - new state: {state.ToString()}");
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
}
