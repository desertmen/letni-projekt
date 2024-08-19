using System;
using System.Collections.Generic;

public class Astar<T> where T : IAStarNode<T>
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

    public virtual bool isNeighbourValid(T neighbour, T current)
    {
        return true;
    }

    public List<T> getPath()
    {
        open.clear();
        closed.Clear();

        if(start.Equals(goal)) 
            return new List<T>() { start };

        bool found = false;
        start.g = 0;
        start.h = start.getDistanceToGoal(goal);
        open.push(start);

        while(!open.isEmpty() && !found)
        {
            T current = open.pop();
            closed.Add(current);
            
            foreach ((T neighbour, float cost) in current.getNeighbours())
            {
                if (closed.Contains(neighbour) || isNeighbourValid(neighbour, current) == false)
                    continue;

                if (neighbour.Equals(goal))
                {
                    goal.parrent = current;
                    found = true;
                    break;
                }

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
        }

        if (!found)
            return new List<T>();
        
        List<T> path = new List<T>();
        T curr = goal;
        while(curr != null && !curr.Equals(start))
        {
            path.Add(curr);
            curr = curr.parrent;
        }
        path.Add(start);
        path.Reverse();

        return path;
    }
}
