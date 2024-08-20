using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Vector2 _JumpDirection;
    [SerializeField][Range(0, 89)] private float _MaxAngle;
    [SerializeField] private int _SelectedPolygon;
    [SerializeField] private int _SelectedChunk;
    [SerializeField] private int _SelectedConnection;
    [SerializeField] private JumpGenerator.Mode _Mode;
    [SerializeField] private bool _ShowTrajectories;
    [SerializeField] private bool _ShowAllIntervalsOnChunk;
    [SerializeField] private bool _ShowPolygonConnections;
    [SerializeField] private bool _ShowJumpNodes;
    [SerializeField] private float _MaxJumpVelocity;
    [SerializeField] private Vector2 _BoundingBoxSize;


    [HideInInspector]
    public Vector3 startHandle;
    [HideInInspector]
    public Vector3 goalHandle;

    // is static so its not 0 in onDrawGizmos
    private static float gravity = -10;

    private void OnDrawGizmos()
    {
        checkInputs();
        JumpGenerator jumpGenerator = new JumpGenerator(gravity);
        switch (_Mode)
        {
            case JumpGenerator.Mode.DIRECTED_JUMP:
                jumpGenerator.setModeDirectedJump(_JumpDirection);
                break;
            case JumpGenerator.Mode.CONST_X_VARIABLE_Y:
                jumpGenerator.setModeConstXvariableYJump(_JumpDirection.x);
                break;
            case JumpGenerator.Mode.CONST_Y_VARIABLE_X:
                jumpGenerator.setModeConstYvariableXJump(_JumpDirection.y);
                break;
        }
        List<JumpGenerator> jumpGenerators = new List<JumpGenerator>() { jumpGenerator };

        List<Polygon> polygons = getChildrenPolygons(_MaxAngle);
        JumpFinder jumpFinder = new JumpFinder(_MaxAngle, gravity, _BoundingBoxSize, polygons);
        JumpMap jumpMap = jumpFinder.generateJumpMap(jumpGenerators);

        drawTrajectories(polygons, jumpMap);
        drawPolygonConnections(jumpMap);
        drawJumpNodes(polygons, jumpMap);

        drawHandles();
        JumpNode start = jumpMap.getClosestJumpNode(startHandle);
        JumpNode goal = jumpMap.getClosestJumpNode(goalHandle);

        VelocityBoundAStar astar = new VelocityBoundAStar(start, goal, new Vector2(_MaxJumpVelocity, _MaxJumpVelocity));
        drawPath(start, goal, astar);
    }

    private void checkInputs()
    {
        if (Mathf.Abs(_JumpDirection.x) == 0)
        {
            _JumpDirection.x = 0.001f;
        }
        if (Mathf.Abs(_JumpDirection.y) <= 0)
        {
            _JumpDirection.y = 0.001f;
        }
        if(_BoundingBoxSize.x <= 0)
        {
            _BoundingBoxSize.x = 0.001f;
        }
        if (_BoundingBoxSize.y <= 0)
        {
            _BoundingBoxSize.y = 0.001f;
        }
        if(gravity > 0)
        {
            gravity = -gravity;
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

    private void drawHandles()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startHandle, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(goalHandle, 0.3f);
    }

    private void drawPath(JumpNode start, JumpNode goal, VelocityBoundAStar astar)
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
                drawInterval(path[i].info.hitInterval, path[i].info.jumpStart);
                Handles.DrawLine(path[i].position, path[i].position + path[i].info.hitInterval.Item1.jumpVelocity.normalized, lineWidth);
                Handles.DrawLine(path[i].position, path[i].position + path[i].info.hitInterval.Item2.jumpVelocity.normalized, lineWidth);
            }
            else
            {
                drawInterval(path[i].info.hitInterval, path[i].info.jumpStart);
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
        WalkableChunk selectedChunk = polygons[_SelectedPolygon].getPrecalculatedWalkableChunks()[_SelectedChunk];

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

    private void drawTrajectories(List<Polygon> polygons, JumpMap jumpMap)
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
                    drawInterval(jumpConnection.hitInterval, jumpConnection.jumpStart);
                    // draw jump starting point
                    Gizmos.color = Color.white;
                    BoxJumpTrajectory.drawBoundingBoxGizmo(jumpConnection.jumpStart, _BoundingBoxSize, (int)Mathf.Sign(jumpConnection.hitInterval.Item1.jumpVelocity.x));
                }
            }
        }
    }

    private void drawPolygonConnections(JumpMap jumpMap)
    {
        Gizmos.color = Color.red;
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
