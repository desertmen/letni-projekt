using System;
using System.Collections.Generic;

public class Astar<T> where T : IAStarNode<T>, IHeapItem<T>
{
    private Heap<T> open = new Heap<T>(HeapType.MIN_HEAP);
    private HashSet<T> closed = new HashSet<T>();

    private T start;
    private T goal;

    public Astar(T start, T goal)
    {
        this.start = start;
        this.goal = goal;
    }

    public List<T> getPath()
    {
        closed.Add(start);
        foreach((T neighbour, float cost) in start.getNeighbours())
        {
            open.push(neighbour);
            neighbour.h = cost;
            neighbour.g = neighbour.getDistanceToGoal(goal);
        }

        bool found = false;
        while(!open.isEmpty())
        {
            T current = open.pop();
            closed.Add(current);
            
            foreach ((T neighbour, float cost) in current.getNeighbours())
            {
                if (neighbour.Equals(goal))
                {
                    goal.parrent = current;
                    found = true;
                    break;
                }

                if (closed.Contains(neighbour))
                    continue;

                float newCost = current.g + cost;

                if (!open.contains(neighbour))
                {
                    neighbour.g = newCost;
                    neighbour.h = neighbour.getDistanceToGoal(goal);
                    neighbour.parrent = current;
                    open.push(neighbour);
                }
                else if (newCost < neighbour.g)
                {
                    neighbour.g = newCost;
                    neighbour.h = neighbour.getDistanceToGoal(goal);
                    neighbour.parrent = current;
                    open.updateUp(neighbour);
                }
            }

            if (found) 
                break;
        }



        return null;
    }
}
