using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpHit
{
    public bool isReachable;
    public Vector2 position;
    public Edge edge;
    public Polygon polygon;
    public float time;
    public Vector2 jumpVelocity;
    public Vector2 impactVelocity;
    public WalkableChunk walkableChunk;

    public JumpHit(Vector2 position, Vector2 jumpVelocity, Vector2 impactVelocity, float time, Polygon polygon, Edge edge)
    {
        this.isReachable = false;
        this.position = position;
        this.edge = edge;
        this.polygon = polygon;
        this.time = time;
        this.jumpVelocity = jumpVelocity;
        this.impactVelocity = impactVelocity;
    }
}
