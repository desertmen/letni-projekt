using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class JumpDetection : MonoBehaviour
{
    [SerializeField] private Vector2 _MaxVelocity;
    [SerializeField] [Range(0, 89)] private float _MaxAngle;
    [SerializeField] private int _SelectedPolygon;
    [SerializeField] private int _SelectedChunk;
    [SerializeField] private int _SelectedConnection;
    [SerializeField] private JumpGenerator.Mode _Mode;
    [SerializeField] private bool _ShowTrajectories;
    [SerializeField] private bool _ShowAllIntervalsOnChunk;
    [SerializeField] private bool _ShowPolygonConnections;

    [SerializeField] private Vector2 _BoundingBoxSize;
    private static float gravity = -10;// Physics2D.gravity.y;

    private const int LEFT = -1;
    private const int RIGHT = 1;


    private struct TargetInfo
    {
        public Vector2 position;
        public Polygon polygon;
        public Edge edge;

        public TargetInfo(Vector2 position, Polygon polygon, Edge edge)
        {
            this.position = position;
            this.polygon = polygon;
            this.edge = edge;
        }
    }

    private void OnDrawGizmos()
    {

        checkSpeed();

        List<Polygon> polygons = getChildrenPolygons(_MaxAngle);


        // set up jumpGenerator
        JumpGenerator jumpGenerator = new JumpGenerator(gravity);
        switch (_Mode)
        {
            case JumpGenerator.Mode.DIRECTED_JUMP:
                jumpGenerator.setModeDirectedJump(_MaxVelocity); 
                break;
            case JumpGenerator.Mode.CONST_X_VARIABLE_Y:
                jumpGenerator.setModeConstXvariableYJump(_MaxVelocity.x);
                break;
            case JumpGenerator.Mode.CONST_Y_VARIABLE_X:
                jumpGenerator.setModeConstYvariableXJump(_MaxVelocity.y);
                break;
        }

        JumpMap jumpMap = getJumpMap(polygons, jumpGenerator);

        drawTrajectories(polygons, jumpMap);
        Gizmos.color = Color.red;
        drawConnections(jumpMap);

        //DrawSlopeGizmo(new Vector3(0, 5, 0));
    }

    private void drawTrajectories(List<Polygon> polygons, JumpMap jumpMap)
    {
        if (_ShowTrajectories)
        {
            _SelectedPolygon = Mathf.Clamp(_SelectedPolygon, 0, polygons.Count - 1);
            Polygon selectedPolygon = polygons[_SelectedPolygon];
            _SelectedChunk = Mathf.Clamp(_SelectedPolygon, 0, selectedPolygon.getPrecalculatedWalkableChunks().Count - 1);
            WalkableChunk selectedChunk = polygons[_SelectedPolygon].getPrecalculatedWalkableChunks()[_SelectedChunk];
            List<JumpConnection> connections = jumpMap.getDestinationConnecitons(selectedChunk);
            _SelectedConnection = Mathf.Clamp(_SelectedConnection, 0, connections.Count - 1);

            for (int i = 0; i < connections.Count; i++)
            {
                if (i == _SelectedConnection || _ShowAllIntervalsOnChunk)
                {
                    JumpConnection jumpConnection = connections[i];
                    drawInterval(jumpConnection.hitInterval, jumpConnection.jumpStart);
                    // draw jump starting point
                    Gizmos.color = Color.white;
                    BoxJumpTrajectory.drawBoundingBoxGizmo(jumpConnection.jumpStart, _BoundingBoxSize, (int)Mathf.Sign(jumpConnection.hitInterval.Item1.jumpVelocity.x));
                }
            }
        }
    }

    private void drawConnections(JumpMap jumpMap)
    {
        if (_ShowPolygonConnections)
        {
            foreach (WalkableChunk walkableChunk1 in jumpMap.getAllWalkableChunks())
            {
                foreach (WalkableChunk walkableChunk2 in jumpMap.getConnectedChunks(walkableChunk1))
                {
                    Gizmos.DrawLine(walkableChunk1.positions[0], walkableChunk2.positions[0]);
                }
            }
        }
    }

    private JumpMap getJumpMap(List<Polygon> polygons, JumpGenerator jumpGenerator)
    {
        List<WalkableChunk> allWalkableChunks = new List<WalkableChunk>();
        foreach(Polygon polygon in polygons)
        {
            allWalkableChunks.AddRange(polygon.calculateWalkableChunks(_MaxAngle));
        }
        JumpMap jumpMap = new JumpMap(allWalkableChunks);
        foreach (Polygon polygon in polygons)
        {
            foreach (WalkableChunk walkableChunk in polygon.getPrecalculatedWalkableChunks())
            {
                Vector2 corner1 = walkableChunk.positions[0];
                Vector2 corner2 = walkableChunk.positions[walkableChunk.positions.Count - 1];
                Vector2 leftCorner = corner1.x < corner2.x ? corner1 : corner2;
                Vector2 rightCorner = corner1.x >= corner2.x ? corner1 : corner2;
                leftCorner = allignJumpStart(leftCorner, _BoundingBoxSize, LEFT, polygon);
                rightCorner = allignJumpStart(rightCorner, _BoundingBoxSize, RIGHT, polygon);

                List<Tuple<JumpHit, JumpHit>> intervals = new List<Tuple<JumpHit, JumpHit>>();
                List<Tuple<JumpHit, JumpHit>> leftIntervals = findJumps(leftCorner, LEFT, polygons, jumpGenerator);
                List<Tuple<JumpHit, JumpHit>> rightIntervals = findJumps(rightCorner, RIGHT, polygons, jumpGenerator);

                intervals.AddRange(leftIntervals);
                intervals.AddRange(rightIntervals);

                foreach (Tuple<JumpHit, JumpHit> leftInterval in leftIntervals)
                {
                    WalkableChunk hitWalkableChunkPoints = leftInterval.Item1.walkableChunk;
                    jumpMap.addConnection(new JumpConnection(walkableChunk, hitWalkableChunkPoints, leftCorner, leftInterval));
                }

                foreach (Tuple<JumpHit, JumpHit> rightInterval in rightIntervals)
                {
                    WalkableChunk hitWalkableChunkPoints = rightInterval.Item1.walkableChunk;
                    jumpMap.addConnection(new JumpConnection(walkableChunk, hitWalkableChunkPoints, rightCorner, rightInterval));
                }
            }
        }
        return jumpMap;
    }

    private List<Tuple<JumpHit, JumpHit>> findJumps(Vector2 jumpStart, int jumpDirection, List<Polygon> polygons, JumpGenerator jumpGenerator)
    {
        List<Tuple<JumpHit, JumpHit>> allIntervals = new List<Tuple<JumpHit, JumpHit>>();

        // find targets
        List<TargetInfo> targets = getTargetsInfo(jumpStart, jumpDirection, polygons);
        if (targets.Count == 0)
            return allIntervals;

        // test all targets 
        bool allTargetsBellowHit = true;
        TargetInfo jumpStartTarget = targets[0];
        List<List<JumpHit>> landingHitsPerJump = new List<List<JumpHit>>();
        foreach (TargetInfo target in targets)
        {
            if (target.position == jumpStart)
            {
                jumpStartTarget = target;
                continue;
            }
            //allTargetsHit &= testBoxJump(jumpStart, target, jumpGenerator, polygons, ref landingHitsPerJump);
            allTargetsBellowHit &= testBoxJump(jumpStart, target, jumpGenerator, polygons, jumpDirection, ref landingHitsPerJump) || target.position.y > jumpStart.y;
        }
        // test jump down if possible
        if (allTargetsBellowHit)
        {
            testBoxJump(jumpStart, jumpStartTarget, jumpGenerator, polygons, jumpDirection, ref landingHitsPerJump);
        }

        // create intervals
        Dictionary<Polygon, List<JumpHit>> jumpHitsPerPolygon = getHitsPerPolygon(landingHitsPerJump);
        foreach (Polygon polygon in jumpHitsPerPolygon.Keys)
        {
            // assign jumphits to walkable chunks they hit
            List<WalkableChunk> walkableChunks = polygon.getPrecalculatedWalkableChunks();
            Dictionary<WalkableChunk, List<JumpHit>> jumpHitsPerWalkableChunk = getJumpHitsPerWalkableChunks(jumpHitsPerPolygon[polygon], walkableChunks);

            // create intervals
            foreach(WalkableChunk walkableChunk in jumpHitsPerWalkableChunk.Keys)
            {
                List<JumpHit> chunkHits = jumpHitsPerWalkableChunk[walkableChunk];
                List<Tuple<JumpHit, JumpHit>> intervalsPerChunk = getIntervalsFromHits(chunkHits, jumpStart);
                allIntervals.AddRange(intervalsPerChunk);
                foreach(Tuple<JumpHit, JumpHit> interval  in intervalsPerChunk)
                {
                    interval.Item1.walkableChunk = walkableChunk;
                    interval.Item2.walkableChunk = walkableChunk;
                }
            }
        }

        return allIntervals;
    }

    private Dictionary<Polygon, List<JumpHit>> getHitsPerPolygon(List<List<JumpHit>> landingHitsPerJump)
    {
        Dictionary<Polygon, List<JumpHit>> jumpHitsPerPolygon = new Dictionary<Polygon, List<JumpHit>>();
        foreach (List<JumpHit> jumpHits in landingHitsPerJump)
        {
            foreach (JumpHit hit in jumpHits)
            {
                if (!jumpHitsPerPolygon.TryGetValue(hit.polygon, out List<JumpHit> polyHitList))
                {
                    jumpHitsPerPolygon.Add(hit.polygon, new List<JumpHit> { hit });
                }
                else
                {
                    polyHitList.Add(hit);
                }
            }
        }
        return jumpHitsPerPolygon;
    }

    private Vector2 allignJumpStart(Vector2 jumpStart, Vector2 bboxSize, int direction, Polygon startPolygon)
    {
        Vector2[] corners = BoxJumpTrajectory.getCorners(jumpStart, bboxSize, direction);

        bool collides = false;
        Vector2 highestCollision = Vector2.negativeInfinity;

        foreach(Edge edge in startPolygon.edges)
        {
            Vector2[] edgePoints = startPolygon.getEdgePoints(edge);
            Vector2 a1 = edgePoints[0];
            Vector2 a2 = edgePoints[1];
            for(int i = 0; i < corners.Length; i++)
            {
                Vector2 b1 = corners[i];
                Vector2 b2 = corners[(i + 1) % corners.Length];

                float tb = ((b1.y - a1.y) * (a2.x - a1.x) - (b1.x - a1.x) * (a2.y - a1.y)) / ((b2.x - b1.x) * (a2.y - a1.y) - (b2.y - b1.y) * (a2.x - a1.x));
                float ta = (b1.x + tb * (b2.x - b1.x) - a1.x) / (a2.x - a1.x);

                if(ta >= 0 && ta <= 1 && tb >= 0 && tb <= 1) 
                {
                    collides = true;
                    Vector2 coll = a1 + (a2 - a1) * ta;
                    if (highestCollision.y < coll.y)
                        highestCollision = coll;
                }
            }
        }

        if (collides)
        {
            jumpStart.y = highestCollision.y;
        }

        return jumpStart;
    }

    // creates intervals base on x position of BL corner of bounding box at the hit time
    private List<Tuple<JumpHit, JumpHit>> getIntervalsFromHits(List<JumpHit> chunkHits, Vector2 jumpStart)
    {
        List<Tuple<JumpHit, JumpHit>> intervalsPerChunk = new List<Tuple<JumpHit, JumpHit>>();

        chunkHits.Sort((JumpHit hit1, JumpHit hit2) =>
        {
            BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit1.jumpVelocity, gravity);
            BoxJumpTrajectory jump2 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit2.jumpVelocity, gravity);
            return jump1.getCornerPositionInTime(hit1.time, BoxJumpTrajectory.BOTTOM_LEFT).x.CompareTo(jump2.getCornerPositionInTime(hit2.time, BoxJumpTrajectory.BOTTOM_LEFT).x);
        });

        JumpHit minHit = null;
        JumpHit maxHit = null;
        for (int i = 0; i < chunkHits.Count; i++)
        {
            if (!chunkHits[i].isReachable)
            {
                if (minHit != null && maxHit != null)
                {
                    if(Vector2.Distance(minHit.position, jumpStart) > 0.0001f && Vector2.Distance(maxHit.position, jumpStart) > 0.0001f)
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

                if (maxHit != null && i == chunkHits.Count - 1)
                {
                    if (Vector2.Distance(minHit.position, jumpStart) > 0.0001f && Vector2.Distance(maxHit.position, jumpStart) > 0.0001f)
                        intervalsPerChunk.Add(new Tuple<JumpHit, JumpHit>(minHit, maxHit));
                }
            }
        }
        return intervalsPerChunk;
    }

    // assign jumphits to theit walkable chunks
    private Dictionary<WalkableChunk, List<JumpHit>> getJumpHitsPerWalkableChunks(List<JumpHit> jumpHitsPerPolygon, List<WalkableChunk> walkableChunks)
    {
        Dictionary<WalkableChunk, List<JumpHit>> jumpHitsPerWalkableChunk = new Dictionary<WalkableChunk, List<JumpHit>>();

        foreach (JumpHit hit in jumpHitsPerPolygon)
        {
            foreach (WalkableChunk walkableChunk in walkableChunks)
            {
                if (walkableChunk.vertexIndicies.Contains(hit.edge.Item1) || walkableChunk.vertexIndicies.Contains(hit.edge.Item2))
                {
                    if (jumpHitsPerWalkableChunk.TryGetValue(walkableChunk, out List<JumpHit> hitsPerChunk))
                    {
                        hitsPerChunk.Add(hit);
                    }
                    else
                    {
                        jumpHitsPerWalkableChunk.Add(walkableChunk, new List<JumpHit> { hit });
                    }
                }
            }
        }
        return jumpHitsPerWalkableChunk;
    }

    // returns false if target cant be reached with jumpGenerators current configuration
    private bool testBoxJump(Vector2 jumpStart, TargetInfo target, JumpGenerator jumpGenerator, List<Polygon> polygons, int direction, ref List<List<JumpHit>> landingHitsPerJump)
    {
        Vector2[] jumpStartCorners = BoxJumpTrajectory.getCorners(jumpStart, _BoundingBoxSize, direction);
        foreach (Vector2 boxHitCorner in jumpStartCorners)
        {
            Vector2 velocity = jumpGenerator.getVelocityByMode(boxHitCorner, target.position);

            if (velocity == Vector2.negativeInfinity || velocity.y < 0)
            {
                return false;
            }

            BoxJumpTrajectory jump = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, velocity, gravity);
            List<JumpHit> jumpHits = testJump(jump, polygons);
            prepareJumpHits(target.position, target.edge, target.polygon, jump, boxHitCorner, jumpHits);

            List<JumpHit> landingHits = getLandingHits(jumpHits, target.position, jump);

            landingHitsPerJump.Add(landingHits);
        }
        return true;
    }

    // returns list of hits where box it touching edge with its bottom
    private List<JumpHit> getLandingHits(List<JumpHit> jumpHits, Vector2 target, BoxJumpTrajectory jump)
    {
        List<JumpHit> landingHits = new List<JumpHit>();
        foreach (JumpHit hit in jumpHits)
        {
            //check if it landed on walkable with bottom edge
            JumpTrajectory bottomJump = jump.getSingleTrajectory(BoxJumpTrajectory.BOTTOM_RIGHT);
            float bottomEdgeHeight = bottomJump.getJumpHeightRelative(hit.time * jump.jumpVelocity.x) + bottomJump.jumpStart.y;
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

    private List<TargetInfo> getTargetsInfo(Vector2 jumpStart, int jumpDirection, List<Polygon> polygons)
    {
        List<TargetInfo> targets = new List<TargetInfo>();

        // find targets and create dictionary containing each targets edge and polygon
        foreach (Polygon polygon in polygons)
        {
            foreach (Vector2 point in polygon.points)
            {
                if (isPointJumpable(jumpStart, point, jumpDirection) || point == jumpStart)
                {
                    bool found = false;
                    Edge targetEdge = null;
                    foreach (Edge edge in polygon.edges)
                    {
                        Vector2[] edgePoints = polygon.getEdgePoints(edge);
                        if (edgePoints[0] == point || edgePoints[1] == point)
                        {
                            // assign walkable edge to target if possible
                            if (!found)
                            {
                                found = true;
                                targetEdge = edge;
                            }
                            else
                            {
                                if (polygon.isEdgeWalkable(edge, _MaxAngle))
                                {
                                    targetEdge = edge;
                                }
                                break;
                            }
                        }
                    }
                    targets.Add(new TargetInfo(point, polygon, targetEdge));
                }
            }
        }
        return targets;
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
            float time = (target.x - boxHitCorner.x) / jump.jumpVelocity.x;
            JumpHit hit = new JumpHit(target, jump.jumpVelocity, jump.getCurrentVelocity(time), time, targetPoly, targetEdge);
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

    private void checkSpeed()
    {
        if (Mathf.Abs(_MaxVelocity.x) == 0)
        {
            _MaxVelocity.x = 0.001f;
        }
        if (Mathf.Abs(_MaxVelocity.y) <= 0)
        {
            _MaxVelocity.y = 0.001f;
        }
    }

    private List<Polygon> getChildrenPolygons(float maxAngle)
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
    private bool isPointJumpable(Vector2 jumpStart, Vector2 point, int direction)
    {
        return Mathf.Sign(point.x - jumpStart.x) == Mathf.Sign(direction) && Mathf.Abs(jumpStart.x - point.x) > 0.001f;
    }

    private void DrawSlopeGizmo(Vector3 pos)
    {
        Gizmos.DrawLine(pos, pos - new Vector3(Mathf.Cos(_MaxAngle * Mathf.Deg2Rad), Mathf.Sin(_MaxAngle * Mathf.Deg2Rad), 0));
        Gizmos.DrawLine(pos, pos + new Vector3(Mathf.Cos(-_MaxAngle * Mathf.Deg2Rad), Mathf.Sin(-_MaxAngle * Mathf.Deg2Rad), 0));
    }
    private void drawInterval(Tuple<JumpHit, JumpHit> interval, Vector2 jumpStart)
    {
        Gizmos.color = Color.cyan;
        (JumpHit hit1, JumpHit hit2) = interval;
        BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit1.jumpVelocity, gravity);
        BoxJumpTrajectory jump2 = new BoxJumpTrajectory(jumpStart, _BoundingBoxSize, hit2.jumpVelocity, gravity);
        jump1.drawTrajectoryGizmo(11, 0.1f);
        jump2.drawTrajectoryGizmo(11, 0.1f);
        Gizmos.color = Color.green;
        jump1.drawBoundingBoxGizmo(hit1.time * jump1.jumpVelocity.x);
        jump2.drawBoundingBoxGizmo(hit2.time * jump2.jumpVelocity.x);
        Handles.color = Color.green;
        Handles.DrawLine(jump1.getCornerPositionInTime(hit1.time, BoxJumpTrajectory.BOTTOM_RIGHT), jump2.getCornerPositionInTime(hit2.time, BoxJumpTrajectory.BOTTOM_LEFT), 10);
    }

}
