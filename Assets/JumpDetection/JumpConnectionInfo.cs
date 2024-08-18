using System;
using UnityEngine;

public struct JumpConnectionInfo 
{
    public WalkableChunk startChunk;
    public WalkableChunk destinationChunk;
    public Vector2 jumpStart;
    public Tuple<JumpHit, JumpHit> hitInterval;

    public JumpConnectionInfo(WalkableChunk startPolygon, WalkableChunk destinationPolygon, Vector2 jumpStart, Tuple<JumpHit, JumpHit> interval)
    {
        this.startChunk = startPolygon;
        this.destinationChunk = destinationPolygon;
        this.jumpStart = jumpStart;
        this.hitInterval = interval;
    }
}
