using System;
using System.Collections;
using System.Collections.Generic;

public interface IAStarNode<T> : IHeapItem<T>
{
    // distance from start
    float g { set; get; }
    // distance to goal
    float h { set; get; }
    IAStarNode<T> parrent {set; get;}

    public float getDistanceToGoal(T goal);
    public List<Tuple<T, float>> getNeighbours();
}
