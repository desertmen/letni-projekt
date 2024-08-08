using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpConnection
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
}
