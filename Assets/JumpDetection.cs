using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class JumpDetection : MonoBehaviour
{
    [SerializeField] private Vector2 _MaxVelocity;
    [SerializeField] [Range(0, 89)] private float _MaxAngle;
    [SerializeField] private int _SelectedPoint;
    [SerializeField] private int _SelectedPolygon;
    [SerializeField] private int _SelectedJump;
    [SerializeField] private bool _ShowNormals;
    [SerializeField] private bool _ShowWalkable;
    [SerializeField] private JumpGenerator.Mode _Mode;

    [SerializeField] private Vector2 _BoundingBoxSize;
    private static float gravity = -10;// Physics2D.gravity.y;

    private void OnDrawGizmos()
    {
        checkSpeed();

        // draw maximal walkable slope
        Vector3 pos = new Vector3(0, 5, 0);
        Gizmos.DrawLine(pos, pos - new Vector3(Mathf.Cos(_MaxAngle * Mathf.Deg2Rad), Mathf.Sin(_MaxAngle * Mathf.Deg2Rad), 0));
        Gizmos.DrawLine(pos, pos + new Vector3(Mathf.Cos(-_MaxAngle * Mathf.Deg2Rad), Mathf.Sin(-_MaxAngle * Mathf.Deg2Rad), 0));

        List<Polygon> polygons = getChildrenPolygons();

        _SelectedPolygon = Mathf.Clamp(_SelectedPolygon, 0, polygons.Count - 1);
        _SelectedPoint = Mathf.Clamp(_SelectedPoint, 0, polygons[_SelectedPolygon].getWalkableCornerPoints(_MaxAngle).Count * 2 - 1);

        Vector2 jumpStart = polygons[_SelectedPolygon].getJumpPoints(_MaxAngle)[_SelectedPoint];
        Vector2 maxJumpVelocity;

        int jumpDirection = (2 * (_SelectedPoint % 2) - 1);
        maxJumpVelocity = new Vector2(jumpDirection * _MaxVelocity.x, _MaxVelocity.y);

        BoxJumpTrajectory maxBoxJump = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, maxJumpVelocity, gravity);
        Gizmos.color = Color.white;
        maxBoxJump.drawBoundingBoxGizmo(0);

        List<Vector2> targets = new List<Vector2>();
        Dictionary<Vector2, Tuple<Edge, Polygon>> targetEdgePolyDict = new Dictionary<Vector2, Tuple<Edge, Polygon>>();

        // find targets and create dictionary containing each targets edge and polygon
        foreach(Polygon polygon in polygons)
        {
            foreach(Vector2 point in polygon.points)
            {
                if (isPointJumpable(maxBoxJump, point) || point == jumpStart)
                {
                    targets.Add(point);
                    bool found = false;
                    foreach(Edge edge in polygon.edges)
                    {
                        Vector2[] edgePoints = polygon.getEdgePoints(edge);
                        if (edgePoints[0] == point || edgePoints[1] == point)
                        {
                            if (!found) 
                            {
                                found = true;
                                targetEdgePolyDict.Add(point, new Tuple<Edge, Polygon>(edge, polygon));
                            }
                            else
                            {
                                if(polygon.isEdgeWalkable(edge, _MaxAngle))
                                {
                                    targetEdgePolyDict[point] = new Tuple<Edge, Polygon>(edge, polygon);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        // jumpStart edge and polygon added to dictionary, not needed in targets
        targets.Remove(jumpStart);

        JumpGenerator jumpGenerator = new JumpGenerator(gravity);
        switch (_Mode)
        {
            case JumpGenerator.Mode.DIRECTED_JUMP:
                jumpGenerator.setModeDirectedJump(maxJumpVelocity); 
                break;
            case JumpGenerator.Mode.CONST_X_VARIABLE_Y:
                jumpGenerator.setModeConstXvariableYJump(maxJumpVelocity.x);
                break;
            case JumpGenerator.Mode.CONST_Y_VARIABLE_X:
                jumpGenerator.setModeConstYvariableXJump(maxJumpVelocity.y);
                break;
        }

        // test all targets 
        bool allTargetsHit = true;
        List<List<JumpHit>> landingHitsPerJump = new List<List<JumpHit>>();
        foreach (Vector2 target in targets)
        {
            foreach (Vector2 boxHitCorner in maxBoxJump.getCorners())
            {
                Vector2 velocity = jumpGenerator.getVelocityByMode(boxHitCorner, target);

                if (velocity == Vector2.negativeInfinity || velocity.y < 0)
                {
                    allTargetsHit = false;
                    BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, velocity, gravity);
                    continue;
                }

                BoxJumpTrajectory jump = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, velocity, gravity);
                List<JumpHit> jumpHits = testJump(jump, polygons);

                (Edge targetEdge, Polygon targetPolygon)= targetEdgePolyDict[target];
                
                prepareJumpHits(target, targetEdge, targetPolygon, jump, boxHitCorner, jumpHits);

                List<JumpHit> landingHits = getLandingHits(jumpHits, target, jump);

                landingHitsPerJump.Add(landingHits);
            }
        }

        // TODO - all targets bellow starting point hit instead of all targets
        if (allTargetsHit)
        {
            Debug.Log("All targets hit! mode: " + _Mode.ToString());
            List<JumpHit> hitGroup = new List<JumpHit>();
            foreach (Vector2 boxHitCorner in maxBoxJump.getCorners())
            {
                Vector2 velocity = jumpGenerator.getVelocityByMode(boxHitCorner, jumpStart);

                if (velocity == Vector2.negativeInfinity || velocity.y < 0)
                {
                    allTargetsHit = false;
                    continue;
                }

                BoxJumpTrajectory jump = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, velocity, gravity);
                List<JumpHit> jumpHits = testJump(jump, polygons);

                (Edge targetEdge, Polygon targetPolygon) = targetEdgePolyDict[jumpStart];

                prepareJumpHits(jumpStart, targetEdge, targetPolygon, jump, boxHitCorner, jumpHits);

                List<JumpHit> landingHits = getLandingHits(jumpHits, jumpStart, jump);
                landingHitsPerJump.Add(landingHits);
                hitGroup.AddRange(landingHits);
            }
        }

        // assign each jumpHit to its polygon
        Dictionary<Polygon, List<JumpHit>> jumpHitsPerPolygon = new Dictionary<Polygon, List<JumpHit>>();
        foreach (List<JumpHit> jumpHits in landingHitsPerJump)
        {
            foreach(JumpHit hit in jumpHits)
            {
                if(!jumpHitsPerPolygon.TryGetValue(hit.polygon, out List<JumpHit> polyHitList))
                {
                    jumpHitsPerPolygon.Add(hit.polygon, new List<JumpHit> { hit });
                }
                else
                {
                    polyHitList.Add(hit);
                }
            }
        }

        // create intervals
        List<Tuple<JumpHit, JumpHit>> allIntervals = new List<Tuple<JumpHit, JumpHit>>();
        foreach(Polygon polygon in jumpHitsPerPolygon.Keys)
        {
            // assign jumphits to theit walkable chunks
            List<List<int>> walkableChunks = polygon.getWalkableChunks(_MaxAngle);
            Dictionary<List<int>, List<JumpHit>> intervals = new Dictionary<List<int>, List<JumpHit>>();

            foreach(JumpHit hit in jumpHitsPerPolygon[polygon])
            {
                foreach(List<int> walkableChunk in walkableChunks)
                {
                    if(walkableChunk.Contains(hit.edge.Item1) || walkableChunk.Contains(hit.edge.Item2))
                    {
                        if(intervals.TryGetValue(walkableChunk, out List<JumpHit> hitsPerChunk))
                        {
                            hitsPerChunk.Add(hit);
                        }
                        else
                        {
                            intervals.Add(walkableChunk, new List<JumpHit> { hit });
                        }
                    }
                }
            } 

            // create intervals
            foreach (List<JumpHit> chunkHits in intervals.Values)
            {
                List<Tuple<JumpHit, JumpHit>> intervalsPerChunk = new List<Tuple<JumpHit, JumpHit>>();

                chunkHits.Sort((JumpHit hit1, JumpHit hit2) =>
                {
                    BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit1.velocity, gravity);
                    BoxJumpTrajectory jump2 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit2.velocity, gravity);
                    return jump1.getCornerPositionInTime(hit1.time, BoxJumpTrajectory.BOTTOM_LEFT).x.CompareTo(jump2.getCornerPositionInTime(hit2.time, BoxJumpTrajectory.BOTTOM_LEFT).x);
                });

                JumpHit minHit = null;
                JumpHit maxHit = null;
                for(int i = 0; i < chunkHits.Count; i++)
                {
                    if (!chunkHits[i].isReachable)
                    {
                        if(minHit != null && maxHit != null)
                        {
                            intervalsPerChunk.Add(new Tuple<JumpHit, JumpHit>(minHit, maxHit));
                        }
                        minHit = null;
                        maxHit = null;
                    }
                    else
                    {
                        if (minHit == null)
                            minHit = chunkHits[i];
                        else
                            maxHit = chunkHits[i];

                        if(maxHit != null && i == chunkHits.Count - 1)
                        {
                            intervalsPerChunk.Add(new Tuple<JumpHit, JumpHit>(minHit, maxHit));
                        }
                    }
                }
                allIntervals.AddRange(intervalsPerChunk);

            }
        }

        // remove interval containing jumpstart
        for(int i = 0; i < allIntervals.Count; i++)
        {
            (JumpHit hit1, JumpHit hit2) = allIntervals[i];
            if(Vector2.Distance(hit1.position, jumpStart) < 0.0001f || Vector2.Distance(hit2.position, jumpStart) < 0.0001f)
            {
                allIntervals.RemoveAt(i);
                break;
            }
        }

        // draw intervals
        foreach (Tuple<JumpHit, JumpHit> interval in allIntervals)
        {
            Gizmos.color = Color.cyan;
            (JumpHit hit1, JumpHit hit2) = interval;
            BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit1.velocity, gravity);
            BoxJumpTrajectory jump2 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit2.velocity, gravity);
            jump1.drawTrajectoryGizmo(11, 0.1f);
            jump2.drawTrajectoryGizmo(11, 0.1f);
            Gizmos.color = Color.green;
            jump1.drawBoundingBoxGizmo(hit1.time * jump1.velocity.x);
            jump2.drawBoundingBoxGizmo(hit2.time * jump2.velocity.x);
            Handles.color = Color.green;
            Handles.DrawLine(jump1.getCornerPositionInTime(hit1.time, BoxJumpTrajectory.BOTTOM_RIGHT), jump2.getCornerPositionInTime(hit2.time, BoxJumpTrajectory.BOTTOM_LEFT), 10);
        }
    }

    // returns list of hits where box it touching edge with its bottom
    private List<JumpHit> getLandingHits(List<JumpHit> jumpHits, Vector2 target, BoxJumpTrajectory jump)
    {
        List<JumpHit> landingHits = new List<JumpHit>();
        foreach (JumpHit hit in jumpHits)
        {
            //check if it landed on walkable with bottom edge
            JumpTrajectory bottomJump = jump.getSingleTrajectory(BoxJumpTrajectory.BOTTOM_RIGHT);
            float bottomEdgeHeight = bottomJump.getJumpHeightRelative(hit.time * jump.velocity.x) + bottomJump.jumpStart.y;
            bool boxTouchesEdgeWithBottom = Mathf.Abs(bottomEdgeHeight - hit.position.y) < 0.001f;

            if (hit.polygon.isEdgeWalkable(hit.edge, _MaxAngle) && boxTouchesEdgeWithBottom)
            {
                if (hit == jumpHits[0] || (hit == jumpHits[1] && Vector2.Distance(jumpHits[0].position, target) < 0.0001f))
                {
                    hit.isReachable = true;
                    landingHits.Add(hit);
                }
                else
                {
                    hit.isReachable = false;
                    landingHits.Add(hit);
                }
            }
        }
        return landingHits;
    }

    private List<JumpHit> testJump(BoxJumpTrajectory jump, List<Polygon> polygons)
    {
        List<JumpHit> jumpHits = new List<JumpHit>();
        // test collisions with every edge
        foreach (Polygon polygon in polygons)
        {
            foreach (Edge edge in polygon.edges)
            {
                if (jump.collidesWithEdge(polygon, edge, out List<JumpHit> edgeHits))
                {
                    jumpHits.AddRange(edgeHits);
                }
            }
        }
        return jumpHits;
    }

    // sorts targetHits, removes unnecesary targethits and adds hit with target in case its missing - we are assuming trajectory is hitting target
    private void prepareJumpHits(Vector2 target, Edge targetEdge, Polygon targetPoly, BoxJumpTrajectory jump, Vector2 boxHitCorner, List<JumpHit> jumpHits)
    {
        //add target collision, sometimes its not included by collidesWithEdge() - becouse rounding errors probably
        if (Mathf.Abs(jump.getMinPointGlobal(target.x) - target.y) < 0.00001f || Mathf.Abs(jump.getMaxPointGlobal(target.x) - target.y) < 0.00001f)
        {
            JumpHit hit = new JumpHit(target, jump.velocity, (target.x - boxHitCorner.x) / jump.velocity.x, targetPoly, targetEdge);
            jumpHits.Add(hit);
        }

        jumpHits.Sort((JumpHit h1, JumpHit h2) => (int)Mathf.Sign(h1.time - h2.time));

        // collision with point on walkable and unwalkable edge creates duplicate hit that causes bad behaviour -> remove unwalkable hit, all after behave same regardless
        for (int j = 0; j < jumpHits.Count - 1; j++)
        {
            if (Mathf.Abs(jumpHits[j].time - jumpHits[j + 1].time) < 0.0001f)
            {
                if (jumpHits[j].polygon.isEdgeWalkable(jumpHits[j].edge, _MaxAngle))
                {
                    jumpHits.RemoveAt(j + 1);
                }
                else
                {
                    jumpHits.RemoveAt(j);
                }
                j--;
            }
        }

        // only keep first hit per each edge
        HashSet<Tuple<Polygon, Edge>> visited = new HashSet<Tuple<Polygon, Edge>>();
        for (int j = 0; j < jumpHits.Count; j++)
        {
            JumpHit hit = jumpHits[j];
            Tuple<Polygon, Edge> hashingTuple = new Tuple<Polygon, Edge>(hit.polygon, hit.edge);

            if (visited.Contains(hashingTuple))
            {
                jumpHits.RemoveAt(j);
                j--;
            }
            else
            {
                visited.Add(hashingTuple);
            }
        }
    }

    private void drawJumpTrajectory(JumpTrajectory curJump, Vector2 target, List<JumpHit> hits, Color jumpColor)
    {
        Gizmos.color = jumpColor;
        Gizmos.DrawWireSphere(target, 0.2f);
        curJump.drawGizmo(10, 0.1f);
        foreach (JumpHit hit in hits)
        {
            Gizmos.color = hit.isReachable ? Color.green : Color.red;
            Gizmos.DrawSphere(hit.position, 0.1f);
        }
    }

    private List<JumpHit> testJump(JumpTrajectory jump, Vector2 target, List<Polygon> collidablePolygons, Dictionary<Polygon, List<Edge>> collidableEdges)
    {
        Vector2 firstHit = Vector2.positiveInfinity;
        List<JumpHit> hits = new List<JumpHit>();

        foreach (Polygon polygon in collidablePolygons)
        {
            collidableEdges.TryGetValue(polygon, out List<Edge> edges);
            foreach (Edge edge in edges)
            {
                Vector2[] edgePoints = polygon.getEdgePoints(edge);

                if (jump.collidesWithEdge(edgePoints[0], edgePoints[1], out Vector2 coll1, out Vector2 coll2))
                {
                    if (Vector2.Distance(coll1, jump.jumpStart) > 0.001f && coll1.x != float.NegativeInfinity)
                    {
                        if (polygon.isEdgeWalkable(edge, _MaxAngle))
                        {
                            hits.Add(new JumpHit(coll1, jump.velocity, 0, polygon, edge));
                        }
                        if (firstHit == Vector2.positiveInfinity || (Mathf.Abs(coll1.x - jump.jumpStart.x) < Mathf.Abs(firstHit.x - jump.jumpStart.x) && Vector2.Distance(target, coll1) > 0.001f))
                        {
                            firstHit = coll1;
                        }
                    }
                    if (Vector2.Distance(coll2, jump.jumpStart) > 0.001f && coll2.x != float.NegativeInfinity)
                    {
                        if (polygon.isEdgeWalkable(edge, _MaxAngle))
                        {
                            hits.Add(new JumpHit(coll2, jump.velocity, 0, polygon, edge));
                        }
                        if (firstHit == Vector2.positiveInfinity || (Mathf.Abs(coll2.x - jump.jumpStart.x) < Mathf.Abs(firstHit.x - jump.jumpStart.x) && Vector2.Distance(target, coll2) > 0.001f))
                            firstHit = coll2;
                    }
                }
            }
        }
        foreach(JumpHit hit in hits)
        {
            hit.isReachable = Mathf.Abs(hit.position.x - jump.jumpStart.x) < Mathf.Abs(firstHit.x - jump.jumpStart.x) || hit.position == firstHit;
        }

        return hits;
    }

    private void checkSpeed()
    {
        if (Mathf.Abs(_MaxVelocity.x) == 0)
        {
            _MaxVelocity.x = 0.001f;
        }
        if (Mathf.Abs(_MaxVelocity.y) == 0)
        {
            _MaxVelocity.y = 0.001f;
        }
    }

    private List<Polygon> getChildrenPolygons()
    {
        List<Polygon> polygons = new List<Polygon>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent<BoxCollider2D>(out BoxCollider2D boxCollider))
            {
                Polygon polygon = new Polygon(boxCollider);
                polygons.Add(polygon);
            }
            else if (transform.GetChild(i).TryGetComponent<PolygonCollider2D>(out PolygonCollider2D polygonCollider))
            {
                for (int j = 0; j < polygonCollider.pathCount; j++)
                {
                    Polygon polygon = new Polygon(polygonCollider, j);
                    polygons.Add(polygon);
                }
            }
        }
        return polygons;
    }

    private bool isPointJumpable(Vector2 point, Vector2 jumpStart, Vector2 jumpVelocity)
    {
        float x = point.x - jumpStart.x;
        if(Mathf.Sign(x) != Mathf.Sign(jumpVelocity.x) || Mathf.Abs(x) < 0.00001f)
        {
            return false;
        }

        float peakTime = jumpVelocity.y / -gravity;
        float peakX = jumpVelocity.x * peakTime;
        // point is between peak and start X
        if(Mathf.Sign(peakX + jumpStart.x - point.x) != Mathf.Sign(jumpStart.x - point.x))
        {
            float peakY = jumpVelocity.y * peakTime + 0.5f * gravity * peakTime * peakTime;
            return peakY + jumpStart.y > point.y; 
        }
        // point is after peak X
        float jumpY = jumpVelocity.y * x / jumpVelocity.x + 0.5f * gravity * (x / jumpVelocity.x) * (x / jumpVelocity.x);
        return jumpY + jumpStart.y > point.y;
    }

    private bool isPointJumpable(BoxJumpTrajectory boxJump, Vector2 point)
    {
        /*
        JumpTrajectory left = boxJump.getSingleTrajectory(BoxJumpTrajectory.BOTTOM_LEFT);
        JumpTrajectory right = boxJump.getSingleTrajectory(BoxJumpTrajectory.BOTTOM_RIGHT);
        return isPointJumpable(point, left.jumpStart, left.velocity) && isPointJumpable(point, right.jumpStart, right.velocity);
        */
        return Mathf.Sign(point.x - boxJump.jumpStart.x) == Mathf.Sign(boxJump.velocity.x) && Mathf.Abs(boxJump.jumpStart.x - point.x) > 0.001f;
    }
}
