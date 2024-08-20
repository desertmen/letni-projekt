
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Edge : Tuple<int, int>
{
    public Polygon polygon;
    public Edge(int item1, int item2, Polygon polygon) : base(item1, item2)
    {
        this.polygon = polygon;
    }
}

public class Polygon 
{
    public static float _MinLength = 0.2f;
    public static float _MinOffset = 0.05f;

    public List<Vector2> points;
    // edges go in clocwise direction
    public List<Edge> edges;

    public Vector2 position;

    private List<WalkableChunk> walkableChunks;

    public Polygon(BoxCollider2D boxCollider)
    {
        Vector2 center = boxCollider.bounds.center;
        Vector2 extends = boxCollider.transform.lossyScale * boxCollider.size / 2;
        Vector2 extends2 = new Vector2(extends.x, -extends.y);
        extends = boxCollider.transform.localToWorldMatrix.rotation * extends;
        extends2 = boxCollider.transform.localToWorldMatrix.rotation * extends2;
                                        //TR                  BR                 BL                 TL
        points = new List<Vector2> { center + extends, center + extends2, center - extends, center - extends2 };
        edges = new List<Edge> { new Edge(0, 1, this), new Edge(1, 2, this), new Edge(2, 3, this), new Edge(3, 0, this) };

        position = boxCollider.transform.position;
    }

    public Polygon(PolygonCollider2D collider, int path = 0)
    {
        position = collider.transform.position;
        points = collider.GetPath(path).ToList<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p1 = points[i] * collider.transform.lossyScale;
            Vector2 p2 = points[(i + 1) % points.Count] * collider.transform.lossyScale;
            Vector2 p3 = points[(i + 2) % points.Count] * collider.transform.lossyScale;
            if(Vector2.Distance(p1, p2) < _MinLength || Vector2.Distance((p2 - p1).normalized, (p3 - p1).normalized) < _MinOffset)
            {
                points.RemoveAt(i + 1);
                i--;
            }
        }
        edges = new List<Edge>();
        for(int i = 0; i < points.Count; i++)
        {
            points[i] = points[i] * collider.transform.lossyScale + (Vector2)collider.transform.position;
            edges.Add(new Edge((i + 1) % points.Count, i, this));
        }
    }

    public WalkableChunk getWalkableChunkTouching(Vector2 center, Vector2 boxSize)
    {
        if (walkableChunks.Count == 1)
            return walkableChunks[0];
        //TODO - fing walkable chunk touched or closest do bbox
    }

    public Vector2[] getEdgePoints(Edge edge)
    {
        return new Vector2[2] { points[edge.Item1], points[edge.Item2] };
    }

    public Vector2 getEdgeNormal(Edge edge)
    {
        Vector2[] edgePoints = getEdgePoints(edge);
        Vector2 dir = edgePoints[0] - edgePoints[1];
        return (new Vector2(dir.y, -dir.x)).normalized;
    }

    // maxAngle in degres
    public bool isEdgeWalkable(Edge edge, float maxAngle)
    {
        float angle = Vector2.Angle(getEdgeNormal(edge), Vector2.up);
        if (angle <= maxAngle)
        {
            return true;
        }
        return false;
    }

    // item1 = left corner, item2 = right corner
    public List<Tuple<Vector2, Vector2>> getWalkableCornerPoints(float maxAngle)
    {
        List<Tuple<Vector2, Vector2>> walkableCorners = new List<Tuple<Vector2, Vector2>>();
        foreach(WalkableChunk chunk in walkableChunks) 
        {
            walkableCorners.Add(new Tuple<Vector2, Vector2>(chunk.positions[0], chunk.positions[chunk.positions.Count - 1]));
        }

        return walkableCorners;
    }
    public List<WalkableChunk> getPrecalculatedWalkableChunks()
    {
        return walkableChunks;
    }

    public List<WalkableChunk> calculateWalkableChunks(float maxAngle)
    {
        walkableChunks = new List<WalkableChunk>();
        int[] walkableEdges = new int[points.Count];
        int[] walkableEdges2 = new int[points.Count];
        bool[] used = new bool[points.Count];
        for (int i = 0; i < walkableEdges.Length; i++)
        {
            walkableEdges[i] = -1;
            walkableEdges2[i] = -1;
        }

        foreach (Edge edge in edges)
        {
            if (isEdgeWalkable(edge, maxAngle))
            {
                walkableEdges[edge.Item1] = edge.Item2;
                walkableEdges2[edge.Item2] = edge.Item1;
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (used[i])
                continue;

            List<int> walkableChunkIndices = new List<int>() { i };

            int end = i;
            int start = i;
            used[i] = true;
            // find start and end
            while (walkableEdges[end] != -1)
            {
                end = walkableEdges[end];
                walkableChunkIndices.Add(end);
                used[end] = true;
            }
            while (walkableEdges2[start] != -1)
            {
                start = walkableEdges2[start];
                walkableChunkIndices.Add(start);
                used[start] = true;
            }

            if(walkableChunkIndices.Count > 1)
                walkableChunks.Add(new WalkableChunk(walkableChunkIndices, getChunkPositionsFromIndicies(walkableChunkIndices)));
        }

        return walkableChunks;
    }

    private List<Vector2> getChunkPositionsFromIndicies(List<int> indicies)
    {
        List<Vector2> chunkPositions = new List<Vector2>();
        foreach (int idx in indicies)
            chunkPositions.Add(points[idx]);
        return chunkPositions;
    }
}
