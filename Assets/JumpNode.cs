using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpNode : IAStarNode
{
    public float g { get ; set ; }
    public float h { get ; set ; }
    public IAStarNode parrent { get; set; }
    public int heapIndex { get; set; }

    public JumpNode()
    {
        g = float.PositiveInfinity;
        h = float.PositiveInfinity;
        parrent = null;
    }

    public int CompareTo(IAStarNode other)
    {
        return (g + h).CompareTo(other.g + other.h);
    }

    public float getDistanceToGoal(IAStarNode goal)
    {
        throw new NotImplementedException();
    }

    public List<Tuple<IAStarNode, float>> getNeighbours()
    {
        throw new NotImplementedException();
    }
}
