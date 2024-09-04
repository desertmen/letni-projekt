using System.Collections.Generic;
using UnityEngine;

public class BoxJumpTrajectory
{
    private JumpTrajectory[] jumpTrajectories;
    private Vector2[] corners;
    public Vector2 jumpVelocity;
    private Vector2 size;
    public Vector2 jumpStart;
    private float gravity;

    public static int BOTTOM_LEFT = 0;
    public static int BOTTOM_RIGHT = 1;
    public static int TOP_RIGHT= 2;
    public static int TOP_LEFT = 3;

    public static Vector2[] getCorners(Vector2 jumpStart, Vector2 size, int direction)
    {
        Vector2 right = direction > 0 ? Vector2.zero : Vector2.right * size;
        Vector2 left = direction > 0 ? Vector2.left * size : Vector2.zero;
        Vector2 up = size * Vector2.up;
        return new Vector2[4] { left + jumpStart, right + jumpStart, right + up + jumpStart, left + up + jumpStart };
    }

    public static void drawBoundingBoxGizmo(Vector2 position, Vector2 size, int direction)
    {
        Vector2[] BboxCorners = getCorners(position, size, direction);
        for (int i = 0; i < BboxCorners.Length; i++)
        {
            Gizmos.DrawLine(BboxCorners[i], BboxCorners[(i + 1) % BboxCorners.Length]);
        }
    }

    public BoxJumpTrajectory(Vector2 jumpStart, Vector2 size, Vector2 jumpVelocity, float gravity)
    {
        this.jumpVelocity = jumpVelocity;
        this.size = size;
        this.gravity = gravity;
        this.jumpStart = jumpStart;
        
        Vector2 right = jumpVelocity.x > 0 ? Vector2.zero : Vector2.right * size;
        Vector2 left = jumpVelocity.x > 0 ? Vector2.left * size : Vector2.zero;
        Vector2 up = size * Vector2.up;
        //                         bottom left, bottom right, top right,    top left
        corners = new Vector2[4] { left,        right,        right + up,   left + up};

        jumpTrajectories = new JumpTrajectory[4];
        for(int i = 0; i < 4; i++)
        {
            corners[i] += jumpStart;
            jumpTrajectories[i] = new JumpTrajectory(corners[i], jumpVelocity, gravity);
        }
    }

    public bool collidesWithEdge(Polygon polygon, Edge edge, out List<JumpHit> hits)
    {
        Vector2[] edgePoints = polygon.getEdgePoints(edge);
        Vector2 p1 = edgePoints[0];
        Vector2 p2 = edgePoints[1];
        hits = new List<JumpHit>();

        if(p1.y < getMinPointGlobal(p1.x) && p2.y < getMinPointGlobal(p2.x))
            return false;

        float timeCollP1 = getCollisionWithPointTime(p1);
        float timeCollP2 = getCollisionWithPointTime(p2);

        Gizmos.color = Color.red;
        if(p1 != jumpStart && timeCollP1 != float.MaxValue)
        {
            hits.Add(new JumpHit(p1, jumpVelocity, getCurrentVelocity(timeCollP1), timeCollP1, polygon, edge));
        }
        if(p2 != jumpStart && timeCollP2 != float.MaxValue)
        {
            hits.Add(new JumpHit(p2, jumpVelocity, getCurrentVelocity(timeCollP2), timeCollP2, polygon, edge));
        }

        foreach (JumpTrajectory jump in jumpTrajectories)
        {
            if (jump.collidesWithEdge(p1, p2, out Vector2 collision1, out Vector2 collision2))
            {
                if(collision1.x > float.MinValue)
                {
                    if(Vector2.Distance(collision1, jump.jumpStart) > 0.001f)
                    {
                        float t1 = (collision1.x - jump.jumpStart.x) / jump.velocity.x;
                        hits.Add(new JumpHit(collision1, jumpVelocity, getCurrentVelocity(t1), t1, polygon, edge));
                    }
                    if(collision2.x > float.MinValue && Vector2.Distance(collision2, jump.jumpStart) > 0.001f)
                    {
                        float t2 = (collision2.x - jump.jumpStart.x) / jump.velocity.x;
                        hits.Add(new JumpHit(collision2, jumpVelocity, getCurrentVelocity(t2), t2, polygon, edge));
                    }
                }
            }
        }
        return hits.Count > 0;
    }

    // return float.MaxValue if point doesnt collide with box trajectory
    public float getCollisionWithPointTime(Vector2 point)
    {
        float time = float.MaxValue;
        int jumpDirSideTop = jumpVelocity.x > 0 ? TOP_RIGHT : TOP_LEFT;
        int jumpDirSideBot = jumpVelocity.x > 0 ? BOTTOM_RIGHT : BOTTOM_LEFT;

        // is point behind or under bounding box
        float toleranceStart = jumpStart.x + Mathf.Sign(jumpVelocity.x) * 0.001f;
        if (Mathf.Sign(point.x - toleranceStart) != Mathf.Sign(jumpVelocity.x))
        {
            float backSide = toleranceStart - Mathf.Sign(jumpVelocity.x) * size.x;
            //  point and is behind bounding box                or              point is under bounding box
            if (Mathf.Sign(point.x - backSide) != Mathf.Sign(jumpVelocity.x) || point.y < jumpStart.y)
            {
                return time;
            }
        }

        if (point.y > getMaxPointGlobal(point.x) || point.y < getMinPointGlobal(point.x))
        {
            return time;
        }

        // is point inside starting bounding box
        if (corners[BOTTOM_LEFT].x <= point.x && point.x <= corners[BOTTOM_RIGHT].x &&
            corners[BOTTOM_LEFT].y <= point.y && point.y <= corners[TOP_RIGHT].y)
        {
            time = 0;
            return time;
        }
        // is between top and bottom jump dir corner trajectory - hitting with side edge first
        else if (jumpTrajectories[jumpDirSideBot].getJumpHeightGlobal(point.x) <= point.y && point.y <= jumpTrajectories[jumpDirSideTop].getJumpHeightGlobal(point.x))
        {
            time = (point.x - jumpStart.x) / jumpVelocity.x;
            return time;
        }
        // is between max boxTrajectory and jump side top corner trajectory - going up and hitting with top edge first
        else if (jumpTrajectories[jumpDirSideTop].getJumpHeightGlobal(point.x) <= point.y && point.y <= getMaxPointGlobal(point.x))
        {
            float D1 = (jumpVelocity.y * jumpVelocity.y) + 2 * gravity * (point.y - jumpTrajectories[jumpDirSideTop].jumpStart.y);
            float time1 = (-jumpVelocity.y + Mathf.Sqrt(D1)) / gravity;
            float time2 = (-jumpVelocity.y - Mathf.Sqrt(D1)) / gravity;

            if (time1 > 0 && time2 > 0)
            {
                time = Mathf.Min(time1, time2);
                return time;
            }
            else
            {
                time = (Mathf.Sign(time1) + 1) * 0.5f * time1 + (Mathf.Sign(time2) + 1) * 0.5f * time2;
                return time;
            }
        }
        // is between jump side bottom corner trajectory and min boxTrajectory - going down and hitting with bottom edge first
        else if (getMinPointGlobal(point.x) <= point.y && point.y <= jumpTrajectories[jumpDirSideBot].getJumpHeightGlobal(point.x))
        {
            float D1 = (jumpVelocity.y * jumpVelocity.y) + 2 * gravity * (point.y - jumpTrajectories[jumpDirSideBot].jumpStart.y);
            float time1 = (-jumpVelocity.y + Mathf.Sqrt(D1)) / gravity;
            float time2 = (-jumpVelocity.y - Mathf.Sqrt(D1)) / gravity;

            if (time1 > 0 && time2 > 0)
            {
                time = Mathf.Max(time1, time2);
                return time;
            }
            else
            {
                time = (Mathf.Sign(time1) + 1) * 0.5f * time1 + (Mathf.Sign(time2) + 1) * 0.5f * time2;
                return time;
            }
        }
        return time;
    }

    public JumpTrajectory getSingleTrajectory(int i)
    {
        if (i < 0 || i >= jumpTrajectories.Length)
            return null;
        return jumpTrajectories[i];
    }

    public Vector2 getCorner(int i)
    {
        if (i < 0 || i >= jumpTrajectories.Length)
            return Vector2.negativeInfinity;
        return jumpTrajectories[i].jumpStart;
    }

    public Vector2[] getCorners()
    {
        return corners;   
    }

    public float getMinPointGlobal(float x)
    {
        float min = float.MaxValue;
        foreach(JumpTrajectory jump in jumpTrajectories)
        {
            float height = jump.getJumpHeightGlobal(x);
            min = min > height ? height : min;
        }
        return min;
    }

    public float getMaxPointGlobal(float x)
    {
        float peakT = jumpVelocity.y / -gravity;
        float leftPeakX = peakT * jumpVelocity.x + jumpTrajectories[TOP_LEFT].jumpStart.x;
        float rightPeakX = peakT * jumpVelocity.x + jumpTrajectories[TOP_RIGHT].jumpStart.x;
        if (leftPeakX <= x && x <= rightPeakX)
        {
            return jumpVelocity.y * peakT + 0.5f * gravity * peakT * peakT + jumpTrajectories[TOP_RIGHT].jumpStart.y;
        }

        float max = float.MinValue;
        foreach (JumpTrajectory jump in jumpTrajectories)
        {
            float height = jump.getJumpHeightGlobal(x);
            max = max < height ? height : max;
        }
        return max;
    }

    public void drawTrajectoryGizmo(float length, float step)
    {
        int direction = (int)Mathf.Sign(jumpVelocity.x);
        
        float directtedStep = step * direction;
        float x1 = jumpStart.x - direction * size.x;
        float x2 = x1 + directtedStep;

        for(int i = 0; i < size.x/step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));

            x1 = x2;
            x2 += directtedStep;
        }

        for (int i = 0; i < length / step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);
            float y1Min = getMinPointGlobal(x1);
            float y2Min = getMinPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));
            Gizmos.DrawLine(new Vector2(x1, y1Min), new Vector2(x2, y2Min));

            x1 = x2;
            x2 += directtedStep;
        }

        for (int i = 0; i < size.x / step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));

            x1 = x2;
            x2 += directtedStep;
        }
    }

    public void drawMaxTrajectoryGizmos(float length, float step)
    {
        int direction = (int)Mathf.Sign(jumpVelocity.x);

        float directtedStep = step * direction;
        float x1 = jumpStart.x - direction * size.x;
        float x2 = x1 + directtedStep;

        for (int i = 0; i < size.x / step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));

            x1 = x2;
            x2 += directtedStep;
        }

        for (int i = 0; i < length / step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));

            x1 = x2;
            x2 += directtedStep;
        }

        for (int i = 0; i < size.x / step; i++)
        {
            float y1Max = getMaxPointGlobal(x1);
            float y2Max = getMaxPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Max), new Vector2(x2, y2Max));

            x1 = x2;
            x2 += directtedStep;
        }
    }

    public void drawMinTrajectoryGizmos(float length, float step)
    {
        int direction = (int)Mathf.Sign(jumpVelocity.x);

        float directtedStep = step * direction;
        float x1 = jumpStart.x - direction * size.x;
        float x2 = x1 + directtedStep;

        for (int i = 0; i < length / step; i++)
        {
            float y1Min = getMinPointGlobal(x1);
            float y2Min = getMinPointGlobal(x2);

            Gizmos.DrawLine(new Vector2(x1, y1Min), new Vector2(x2, y2Min));

            x1 = x2;
            x2 += directtedStep;
        }
    }

    public void drawBoundingBoxGizmo(float xTraveled)
    {
        for(int i1 = 0; i1 < jumpTrajectories.Length; i1++)
        {
            int i2 = (i1 + 1) % jumpTrajectories.Length;

            Vector2 p1 = jumpTrajectories[i1].jumpStart + new Vector2(xTraveled, jumpTrajectories[i1].getJumpHeightRelative(xTraveled));
            Vector2 p2 = jumpTrajectories[i2].jumpStart + new Vector2(xTraveled, jumpTrajectories[i2].getJumpHeightRelative(xTraveled));

            Gizmos.DrawLine(p1, p2);
        }
    }

    public Vector2 getCornerPositionInTime(float time, int corner)
    {
        return jumpTrajectories[corner].jumpStart + new Vector2(time * jumpVelocity.x, jumpTrajectories[corner].getJumpHeightRelative(time * jumpVelocity.x));
    }

    public Vector2 getCurrentVelocity(float time)
    {
        return new Vector2(jumpVelocity.x, jumpVelocity.y + gravity * time);
    }
}
