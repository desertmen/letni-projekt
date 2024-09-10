using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpNode : IAStarNode<JumpNode>
{
    public float g { get; set; }
    public float h { get; set; }
    public JumpNode parrent { get; set; }
    public int heapIndex { get; set; }

    private JumpMap jumpMap;
    public JumpConnectionInfo info { get; private set; }
    public WalkableChunk chunk { get; private set; }
    public Vector2 position { get; private set; }

    private JumpNode intervalNeighbour;
    private float jumpPower = 1;

    public JumpNode(Vector2 position, WalkableChunk chunk, JumpConnectionInfo info, JumpMap jumpMap)
    {
        this.position = position;
        this.jumpMap = jumpMap;
        this.chunk = chunk;
        this.info = info;

        g = float.PositiveInfinity;
        h = float.PositiveInfinity;
        parrent = null;
    }

    public void addJumpPower(float power)
    {
        jumpPower += power;
    }

    public void removeJumpPower(float power)
    {
        jumpPower -= power;
    }

    public float getJumpPower()
    {
        return jumpPower;
    }

    public void setIntervalNeighbour(JumpNode intervalNeighbour)
    {
        this.intervalNeighbour = intervalNeighbour;
    }

    public float getDistanceToGoal(JumpNode goal)
    {
        return distance(goal);
    }

    public List<Tuple<JumpNode, float>> getNeighbours()
    {
        List<Tuple<JumpNode, float>> neighbours = new List<Tuple<JumpNode, float>>();

        if(intervalNeighbour != null)
            neighbours.Add(new Tuple<JumpNode, float>(intervalNeighbour, distance(intervalNeighbour)));
        
        foreach(JumpNode neighbour in jumpMap.getJumpNodesOnChunk(chunk))
        {
            if (neighbour != this)
                neighbours.Add(new Tuple<JumpNode, float>(neighbour, distance(neighbour)));
        }

        return neighbours;
    }

    public int CompareTo(JumpNode other)
    {
        return (g + h).CompareTo(other.g + other.h);
    }

    private float distance(JumpNode other)
    {
        return Vector2.Distance(position, other.position);
    }
}
