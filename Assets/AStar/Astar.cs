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

    public List<T> getPath()
    {
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
        }

        if (!found)
        {
            return null;
        }
        
        List<T> path = new List<T>();
        T curr = goal;
        while(curr != null && !curr.Equals(start))
        {
            path.Add(curr);
            curr = curr.parrent;
        }
        path.Add(start);

        return path;
    }
}
