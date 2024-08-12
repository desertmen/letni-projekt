using System;
using System.Collections;
using System.Collections.Generic;

public interface INode : IComparable<INode>
{
    public float distance(INode node);
    public void setDistanceTraveled(float dist);
    public float getDistanceTraveled();
}
