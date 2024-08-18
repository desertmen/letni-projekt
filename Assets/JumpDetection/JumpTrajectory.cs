
using System.Drawing;
using Unity.Mathematics;
using UnityEngine;

public class JumpTrajectory
{
    public Vector2 jumpStart;
    public Vector2 velocity;
    float gravity;

    // gravity is negative
    public JumpTrajectory(Vector2 jumpStart, Vector2 velocity, float gravity)
    {
        this.jumpStart = jumpStart;
        this.velocity = velocity;
        this.gravity = gravity;
    }

    // collision = Vector2.negativeInfinity if false
    public bool collidesWithLine(Vector2 p1, Vector2 p2, out Vector2 collision1, out Vector2 collision2)
    {
        // vertical line
        if(p1.x - p2.x == 0)
        {
            collision1 = new Vector2(p1.x, getJumpHeightGlobal(p1.x));
            collision2 = Vector2.negativeInfinity;
            return true;
        }

        // line representable by funciton
        Vector2 line = getLineEquation(p1 - jumpStart, p2 - jumpStart);
        float D = (velocity.y / velocity.x - line.x) * (velocity.y / velocity.x - line.x) + (2 * gravity * line.y) / (velocity.x * velocity.x);
        if (D < 0) {
            collision1 = Vector2.negativeInfinity;
            collision2 = Vector2.negativeInfinity;
            return false;
        }

        D = Mathf.Sqrt(D);
        float x1 = (-(velocity.y / velocity.x - line.x) + D) / (gravity / (velocity.x * velocity.x));
        float x2 = (-(velocity.y / velocity.x - line.x) - D) / (gravity / (velocity.x * velocity.x));
        collision1 = new Vector2(x1, line.x * x1 + line.y) + jumpStart;
        collision2 = new Vector2(x2, line.x * x2 + line.y) + jumpStart;

        return true;
    }

    public bool collidesWithEdge(Vector2 p1, Vector2 p2, out Vector2 collision1, out Vector2 collision2)
    {
        if (!collidesWithLine(p1, p2, out collision1, out collision2))
            return false;

        bool isColl1OnEdge = isPointOnEdge(p1, p2, collision1) && Mathf.Sign(velocity.x) == Mathf.Sign(collision1.x - jumpStart.x);
        bool isColl2OnEdge = isPointOnEdge(p1, p2, collision2) && Mathf.Sign(velocity.x) == Mathf.Sign(collision2.x - jumpStart.x);

        collision1 = isColl1OnEdge ? collision1 : Vector2.negativeInfinity;
        collision2 = isColl2OnEdge ? collision2 : Vector2.negativeInfinity;

        if(isColl1OnEdge || isColl2OnEdge)
        {
            if (!isColl1OnEdge)
            {
                collision1 = collision2;
                collision2 = Vector2.negativeInfinity;
            }
            return true;
        }
        return false;
    }

    public bool collidesWithEdge(Vector2 p1, Vector2 p2)
    {
        if (!collidesWithLine(p1, p2, out Vector2 collision1, out Vector2 collision2))
            return false;

        bool isColl1OnEdge = isPointOnEdge(p1, p2, collision1) && Mathf.Sign(velocity.x) == Mathf.Sign(collision1.x - jumpStart.x);
        bool isColl2OnEdge = isPointOnEdge(p1, p2, collision2) && Mathf.Sign(velocity.x) == Mathf.Sign(collision2.x - jumpStart.x);

        if (isColl1OnEdge || isColl2OnEdge)
        {
            return true;
        }
        return false;
    }

    public Vector2 getPeak()
    {
        float peakTime = velocity.y / -gravity;
        float peakX = velocity.x * peakTime;
        float peakY = getJumpHeightRelative(peakX);
        return new Vector2(peakX, peakY) + jumpStart;

    }

    public void drawGizmo(float length, float step)
    {
        float signedStep = step * Mathf.Sign(velocity.x);
        float x1 = 0;
        float x2 = signedStep;
        for(int i = 0; i < (length / step) + 1; i++)
        {
            float y1 = getJumpHeightRelative(x1);
            float y2 = getJumpHeightRelative(x2);

            Gizmos.DrawLine(new Vector2(x1, y1) + jumpStart, new Vector2(x2, y2) + jumpStart);

            x1 = x2;
            x2 += signedStep;
        }
    }

    private bool isPointOnEdge(Vector2 edgeP1, Vector2 edgeP2, Vector2 point)
    {
        if (point == Vector2.negativeInfinity)
            return false;
        if(Mathf.Abs(edgeP1.x - edgeP2.x) <= 0.001f)
        {
            return (edgeP1.y <= point.y && point.y <= edgeP2.y) || (edgeP2.y <= point.y && point.y <= edgeP1.y);
        }
        float t1 = (point.x - edgeP1.x) / (edgeP2.x - edgeP1.x);
        float t2 = (point.x - edgeP2.x) / (edgeP1.x - edgeP2.x);
        return (t1 >= -0.001 && t1 <= 1.001) || (t2 >= -0.001 && t2 <= 1.001);
    }

    // returns (a, b) in y = ax + b which goes through p1, p2
    private Vector2 getLineEquation(Vector2 p1, Vector2 p2)
    {
        if (p2.x < p1.x)
        {
            Vector2 tmp = p1;
            p1 = p2;
            p2 = tmp;
        }
        float a = (p2.y - p1.y) / (p2.x - p1.x);
        float b = p1.y - a * p1.x;

        return new Vector2(a, b);
    }

    // return height of parabola in coordinate system with origin in jumpStart
    public float getJumpHeightRelative(float x)
    {
        return velocity.y * x / velocity.x + 0.5f * gravity * (x / velocity.x) * (x / velocity.x);
    }

    // return height of parabola in coordinate system with origin same as scene
    public float getJumpHeightGlobal(float x)
    {
        return getJumpHeightRelative(x - jumpStart.x) + jumpStart.y;
    }
}
