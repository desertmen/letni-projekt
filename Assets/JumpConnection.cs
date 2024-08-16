using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpConnection : IAStarNode
{
    public WalkableChunk startChunk;
    public WalkableChunk destinationChunk;
    public Vector2 jumpStart;
    public Tuple<JumpHit, JumpHit> hitInterval;

    public JumpConnection(WalkableChunk startPolygon, WalkableChunk destinationPolygon, Vector2 jumpStart, Tuple<JumpHit, JumpHit> interval)
    {
        this.startChunk = startPolygon;
        this.destinationChunk = destinationPolygon;
        this.jumpStart = jumpStart;
        this.hitInterval = interval;
    }

    public float g { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public float h { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IAStarNode parrent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int heapIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IAStarNode item { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int CompareTo(IAStarNode other)
    {
        throw new NotImplementedException();
    }

    public float distance(IAStarNode node)
    {
        throw new NotImplementedException();
    }

    public List<Tuple<IAStarNode, float>> getNeighbours()
    {
        throw new NotImplementedException();
    }
}
