using System;
using System.Collections.Generic;

public class Astar
{
    private Heap<IAStarNode> open = new Heap<IAStarNode>(HeapType.MIN_HEAP);
    private HashSet<IAStarNode> closed = new HashSet<IAStarNode>();

    private IAStarNode start;
    private IAStarNode goal;

    public Astar(IAStarNode start, IAStarNode goal)
    {
        this.start = start;
        this.goal = goal;
    }

    public List<IAStarNode> getPath()
    {
        closed.Add(start);
        foreach((IAStarNode neighbour, float cost) in start.getNeighbours())
        {
            open.push(neighbour);
            neighbour.h = cost;
            neighbour.g = neighbour.distance(goal);
        }

        bool found = false;
        while(!open.isEmpty())
        {
            IAStarNode current = open.pop();
            closed.Add(current);
            
            foreach ((IAStarNode neighbour, float cost) in current.getNeighbours())
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
                    neighbour.h = neighbour.distance(goal);
                    neighbour.parrent = current;
                    open.push(neighbour);
                }
                else if (newCost < neighbour.g)
                {
                    neighbour.g = newCost;
                    neighbour.h = neighbour.distance(goal);
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
