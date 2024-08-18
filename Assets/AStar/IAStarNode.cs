using System;
using System.Collections;
using System.Collections.Generic;

public interface IAStarNode : IHeapItem<IAStarNode>
{
    // distance from start
    float g { set; get; }
    // distance to goal
    float h { set; get; }
    IAStarNode parrent {set; get;}

    public float getDistanceToGoal(IAStarNode goal);
    public List<Tuple<IAStarNode, float>> getNeighbours();
}
