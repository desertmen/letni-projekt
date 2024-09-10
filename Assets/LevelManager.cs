using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
// ------------------ debuging gizmos stuff
    [SerializeField] private int _SelectedPolygon;
    [SerializeField] private int _SelectedChunk;
    [SerializeField] private int _SelectedConnection;
    [SerializeField] public bool _ShowTrajectories;
    [SerializeField] public bool _ShowAllIntervalsOnChunk;
    [SerializeField] private bool _ShowPolygonConnections;
    [SerializeField] private bool _ShowJumpNodes;
    [SerializeField] public bool _ShowHandlesPath;
    [SerializeField] private NPCMovent _GizmoNPC;

    [HideInInspector]
    public Vector3 startHandle;
    [HideInInspector]
    public Vector3 goalHandle;

    private JumpMap gizmoJumpMap;
    private Vector2 gizmoBBoxSize;
    private float gizmoMaxAngle;

// ------------------ actually needed
    [SerializeField] private List<JumpGenerator.JumpGeneratorInput> _JumpGenerators;

    private List<Polygon> polygons = null;
    private float gravity;
    private Dictionary<(float, Vector2), JumpMap> jumpMapDict = new();

    private void Awake()
    {
        gravity = Physics2D.gravity.y;
        //generateJumpMap();
    }
    
    private void OnDrawGizmos()
    {
        gravity = Physics2D.gravity.y;

        if (_GizmoNPC == null)
            return;

        Vector2 thisGizmoBBox = _GizmoNPC.getSize();
        float thisGizmoMaxangle = _GizmoNPC.getMaxAngle();

        if (gizmoJumpMap == null || polygons == null || gizmoBBoxSize != thisGizmoBBox || thisGizmoMaxangle != gizmoMaxAngle)
        {
            gizmoJumpMap = generateJumpMap(thisGizmoBBox, thisGizmoMaxangle);
            gizmoMaxAngle = thisGizmoMaxangle;
            gizmoBBoxSize = thisGizmoBBox;
        }
        
        drawTrajectories(polygons, gizmoJumpMap, gizmoBBoxSize);
        drawPolygonConnections(gizmoJumpMap, _GizmoNPC.getMaxJump());
        drawJumpNodes(polygons, gizmoJumpMap);
        drawPathBetweenHandles(gizmoJumpMap, gizmoBBoxSize, _GizmoNPC.getMaxJump());
    }

    // TODO - dictionary of jumpmaps based on maxAngle
    //      - add function to generate map when npc finds level Manager
    
    public bool innitNPC(Vector2 boundingBoxSize, float maxAngle)
    {
        if (boundingBoxSize.x <= 0)
        {
            Debug.LogError($"NPC boundingboxSize less or 0!");
            return false;
        }
        if (boundingBoxSize.y <= 0)
        {
            Debug.LogError("NPC boundingboxSize less or 0!");
            return false;
        }

        if (!jumpMapDict.TryGetValue((maxAngle, boundingBoxSize), out JumpMap jumpMap))
        {
            jumpMap = generateJumpMap(boundingBoxSize, maxAngle);
            jumpMapDict.Add((maxAngle, boundingBoxSize), jumpMap);
        }
        return true;
    }
    
    public List<JumpNode> getPath(Vector2 start, Vector2 goal, Vector2 maxJump, Vector2 boundingBoxSize, float maxAngle)
    {
        if (!jumpMapDict.TryGetValue((maxAngle, boundingBoxSize), out JumpMap jumpMap))
        {
            Debug.LogError("NPC geting path not initialized!");
        }

        JumpNode startNode = jumpMap.getClosestJumpNodeUnderBox(start, boundingBoxSize.x);
        JumpNode goalNode = jumpMap.getClosestJumpNodeUnderBox(goal, boundingBoxSize.x);
        if(startNode == null || goalNode == null)
        {
            return new List<JumpNode>();
        }
        VelocityBoundAStar aStar = new VelocityBoundAStar(startNode, goalNode, maxJump);
        return aStar.getPath();
    }

    public JumpMap generateJumpMap(Vector2 boundingBoxSize, float maxAngle)
    {
        JumpMap jumpMap;
        DateTime before = DateTime.Now;

        checkInputs();

        polygons = getChildrenPolygons();
        List<JumpGenerator> jumpGenerators = new List<JumpGenerator>();
        foreach (JumpGenerator.JumpGeneratorInput input in _JumpGenerators)
        {
            JumpGenerator jumpGenerator = new JumpGenerator(gravity);
            switch (input.mode)
            {
                case JumpGenerator.Mode.DIRECTED_JUMP:
                    jumpGenerator.setModeDirectedJump(input.direction);
                    break;
                case JumpGenerator.Mode.CONST_X_VARIABLE_Y:
                    jumpGenerator.setModeConstXvariableYJump(input.velocity);
                    break;
                case JumpGenerator.Mode.CONST_Y_VARIABLE_X:
                    jumpGenerator.setModeConstYvariableXJump(input.velocity);
                    break;
            }
            jumpGenerators.Add(jumpGenerator);
        }

        JumpFinder jumpFinder = new JumpFinder(maxAngle, gravity, boundingBoxSize, polygons);
        jumpMap = jumpFinder.generateJumpMap(jumpGenerators);

        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log($"Generated jumpMap (maxAngle {maxAngle}°, bbox {boundingBoxSize}) in {duration.Milliseconds}ms");
        return jumpMap;
    }

    private void checkInputs()
    {
        if(_JumpGenerators == null || _JumpGenerators.Count == 0)
        {
            Debug.LogError("Level Manager - no jumps assigned");
            return;
        }
        foreach(JumpGenerator.JumpGeneratorInput input in _JumpGenerators)
        {
            for(int i = 0; i< _JumpGenerators.Count; i++)
            {
                if(input.mode == JumpGenerator.Mode.DIRECTED_JUMP)
                {
                    if (Mathf.Abs(input.direction.x) == 0)
                    {
                        Debug.LogError($"Jumgenerators (Element {i}) DIRECTED_JUMP must have Non-Zero X value");
                    }
                    if (Mathf.Abs(input.direction.y) <= 0)
                    {
                        Debug.LogError($"Jumgenerators (Element {i}) DIRECTED_JUMP must have positive Y value");
                    }   
                }
                if(input.mode == JumpGenerator.Mode.CONST_Y_VARIABLE_X && input.velocity <= 0)
                {
                    Debug.LogError($"Jumgenerators (Element {i}) CONST_Y_VARIABLE_X must have positive Y value");
                }
            }
        }
        if(gravity == 0)
        {
            gravity = -10;
        }
        else if(gravity > 0)
        {
            gravity = -gravity;
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

    /* GIZMO DRAWING FUNCTIONS -------------------------------------------------------------------------------------------------------------------
                |
                |
                |
                |
                |
            \   |   /
             \  |  /
              \ | /
               \|/
    */

    private void drawHandles()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startHandle, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(goalHandle, 0.3f);
    }

    private void drawPathBetweenHandles(JumpMap jumpMap, Vector2 boundingBoxSize, Vector2 maxJump)
    {
        if(_ShowHandlesPath)
        {
            drawHandles();
            JumpNode start = jumpMap.getClosestJumpNode(startHandle);
            JumpNode goal = jumpMap.getClosestJumpNode(goalHandle);

            VelocityBoundAStar astar = new VelocityBoundAStar(start, goal, maxJump);
            drawPath(start, goal, astar, boundingBoxSize);
        }
    }

    private void drawPath(JumpNode start, JumpNode goal, VelocityBoundAStar astar, Vector2 boundingBoxSize)
    {
        List<JumpNode> path = astar.getPath();

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(goal.position, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(start.position, 0.2f);

        float lineWidth = 6;
        Handles.color = Color.green;
        for (int i = 0; i < path.Count - 1; i++)
        {

            if (path[i].chunk == path[i + 1].chunk)
            {
                Gizmos.color = Color.green;
                Handles.DrawLine(path[i].position, path[i + 1].position, lineWidth);
            }
            else if (path[i].position == path[i].info.jumpStart)
            {
                drawInterval(path[i].info.hitInterval, path[i].info.jumpStart, boundingBoxSize);
                Handles.DrawLine(path[i].position, path[i].position + path[i].info.hitInterval.Item1.jumpVelocity.normalized, lineWidth);
                Handles.DrawLine(path[i].position, path[i].position + path[i].info.hitInterval.Item2.jumpVelocity.normalized, lineWidth);
            }
            else
            {
                drawInterval(path[i].info.hitInterval, path[i].info.jumpStart, boundingBoxSize);
                Handles.DrawLine(path[i].position, path[i].position - path[i].info.hitInterval.Item1.impactVelocity.normalized, lineWidth);
                Handles.DrawLine(path[i].position, path[i].position - path[i].info.hitInterval.Item2.impactVelocity.normalized, lineWidth);
            }
        }
    }

    private void drawJumpNodes(List<Polygon> polygons, JumpMap jumpMap)
    {
        if (!_ShowJumpNodes)
            return;

        _SelectedPolygon = Mathf.Clamp(_SelectedPolygon, 0, polygons.Count - 1);
        Polygon selectedPolygon = polygons[_SelectedPolygon];
        _SelectedChunk = Mathf.Clamp(_SelectedPolygon, 0, selectedPolygon.getPrecalculatedWalkableChunks().Count - 1);

        foreach (JumpNode node in jumpMap.getJumpNodes())
        {
            foreach ((JumpNode neighbour, float cost) in node.getNeighbours())
            {
                Gizmos.DrawLine(node.position, neighbour.position);
                Gizmos.DrawSphere(node.position, 0.1f);
                Gizmos.DrawSphere(neighbour.position, 0.1f);
            }
        }
    }

    private void drawTrajectories(List<Polygon> polygons, JumpMap jumpMap, Vector2 boundingBoxSize)
    {
        if (_ShowTrajectories)
        {
            _SelectedPolygon = Mathf.Clamp(_SelectedPolygon, 0, polygons.Count - 1);
            Polygon selectedPolygon = polygons[_SelectedPolygon];
            _SelectedChunk = Mathf.Clamp(_SelectedPolygon, 0, selectedPolygon.getPrecalculatedWalkableChunks().Count - 1);
            WalkableChunk selectedChunk = polygons[_SelectedPolygon].getPrecalculatedWalkableChunks()[_SelectedChunk];
            List<JumpConnectionInfo> connections = jumpMap.getOutgoingConnections(selectedChunk);
            _SelectedConnection = Mathf.Clamp(_SelectedConnection, 0, connections.Count - 1);

            for (int i = 0; i < connections.Count; i++)
            {
                if (i == _SelectedConnection || _ShowAllIntervalsOnChunk)
                {
                    JumpConnectionInfo jumpConnection = connections[i];
                    drawInterval(jumpConnection.hitInterval, jumpConnection.jumpStart, boundingBoxSize);
                    // draw jump starting point
                    Gizmos.color = Color.white;
                    BoxJumpTrajectory.drawBoundingBoxGizmo(jumpConnection.jumpStart, boundingBoxSize, (int)Mathf.Sign(jumpConnection.hitInterval.Item1.jumpVelocity.x));
                }
            }
        }
    }

    private void drawPolygonConnections(JumpMap jumpMap, Vector2 maxJump)
    {
        if (_ShowPolygonConnections)
        {
            Gizmos.color = Color.red;
            foreach(WalkableChunk chunk in jumpMap.getWalkableChunks())
            {
                foreach(JumpConnectionInfo jump in jumpMap.getOutgoingConnections(chunk))
                {
                    if(MyUtils.Math.absLessOrEqual(jump.hitInterval.Item1.jumpVelocity, maxJump) ||
                       MyUtils.Math.absLessOrEqual(jump.hitInterval.Item2.jumpVelocity, maxJump))
                    {
                        MyUtils.GizmosBasic.drawArrow(jump.jumpStart, (jump.hitInterval.Item1.position + jump.hitInterval.Item2.position) /2f, 0.3f);
                    }
                }
                foreach (JumpConnectionInfo jump in jumpMap.getIncomingConnections(chunk))
                {
                    if (MyUtils.Math.absLessOrEqual(jump.hitInterval.Item1.impactVelocity, maxJump) ||
                        MyUtils.Math.absLessOrEqual(jump.hitInterval.Item2.impactVelocity, maxJump))
                    {
                        MyUtils.GizmosBasic.drawArrow((jump.hitInterval.Item1.position + jump.hitInterval.Item2.position) /2f, jump.jumpStart, 0.3f);
                    }
                }
            }
        }
    }

    private void DrawSlopeGizmo(Vector3 pos, float maxAngle)
    {
        Gizmos.DrawLine(pos, pos - new Vector3(Mathf.Cos(maxAngle * Mathf.Deg2Rad), Mathf.Sin(maxAngle * Mathf.Deg2Rad), 0));
        Gizmos.DrawLine(pos, pos + new Vector3(Mathf.Cos(-maxAngle * Mathf.Deg2Rad), Mathf.Sin(-maxAngle * Mathf.Deg2Rad), 0));
    }

    public void drawInterval(Tuple<JumpHit, JumpHit> interval, Vector2 jumpStart, Vector2 boundingBoxSize)
    {
        Gizmos.color = Color.cyan;
        (JumpHit hit1, JumpHit hit2) = interval;
        BoxJumpTrajectory jump1 = new BoxJumpTrajectory(jumpStart, boundingBoxSize, hit1.jumpVelocity, gravity);
        BoxJumpTrajectory jump2 = new BoxJumpTrajectory(jumpStart, boundingBoxSize, hit2.jumpVelocity, gravity);
        jump1.drawTrajectoryGizmo(11, 0.1f);
        jump2.drawTrajectoryGizmo(11, 0.1f);
        Gizmos.color = Color.green;
        jump1.drawBoundingBoxGizmo(hit1.time * jump1.jumpVelocity.x);
        jump2.drawBoundingBoxGizmo(hit2.time * jump2.jumpVelocity.x);
        Handles.color = Color.green;
        Handles.DrawLine(jump1.getCornerPositionInTime(hit1.time, BoxJumpTrajectory.BOTTOM_RIGHT), jump2.getCornerPositionInTime(hit2.time, BoxJumpTrajectory.BOTTOM_LEFT), 10);
    }
}
