using System;
using UnityEngine;

public class JumpConnection
{
    public Polygon startPolygon;
    public Polygon destinationPolygon;
    public Vector2 jumpStart;
    public Tuple<JumpHit, JumpHit> hitInterval;

    public JumpConnection(Polygon startPolygon, Polygon destinationPolygon, Vector2 jumpStart, Tuple<JumpHit, JumpHit> interval)
    {
        this.startPolygon = startPolygon;
        this.destinationPolygon = destinationPolygon;
        this.jumpStart = jumpStart;
        this.hitInterval = interval;
    }
}
