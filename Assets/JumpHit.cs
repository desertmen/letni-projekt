using System;
using UnityEngine;

public class JumpHit
{
    public bool isReachable;
    public Vector2 position;
    public Edge edge;
    public Polygon polygon;
    public float time;
    public Vector2 velocity;

    public JumpHit(Vector2 position, Vector2 veloctiy, float time, Polygon polygon, Edge edge)
    {
        this.isReachable = false;
        this.position = position;
        this.edge = edge;
        this.polygon = polygon;
        this.time = time;
        this.velocity = veloctiy;
    }
}
