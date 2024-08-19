using UnityEngine;

public class VelocityBoundAStar : Astar<JumpNode>
{
    private Vector2 maxVelocity;
    public VelocityBoundAStar(JumpNode start, JumpNode goal, Vector2 maxVelocity) : base(start, goal)
    {
        this.maxVelocity = new Vector2(Mathf.Abs(maxVelocity.x), Mathf.Abs(maxVelocity.y));
    }

    public override bool isNeighbourValid(JumpNode neighbour, JumpNode current)
    {
        if (neighbour.chunk == current.chunk)
            return true;

        Vector2 velocity;
        // neighbour is node created from interval of two hits, where current node is its jumpstart
        if(current.position == current.info.jumpStart)
        {
            float minVelocityX = MyUtils.Math.minSize(current.info.hitInterval.Item1.jumpVelocity.x, current.info.hitInterval.Item2.jumpVelocity.x);
            float minVelocityY = MyUtils.Math.minSize(current.info.hitInterval.Item1.jumpVelocity.y, current.info.hitInterval.Item2.jumpVelocity.y);
            velocity = new Vector2(minVelocityX, minVelocityY);
        }
        // neighbour is node created from jumpstart, current is created from hit interval
        else
        {
            float minVelocityX = MyUtils.Math.minSize(current.info.hitInterval.Item1.impactVelocity.x, current.info.hitInterval.Item2.impactVelocity.x);
            float minVelocityY = MyUtils.Math.minSize(current.info.hitInterval.Item1.impactVelocity.y, current.info.hitInterval.Item2.impactVelocity.y);
            velocity = new Vector2(minVelocityX, minVelocityY);
        }
        // velocity is positive from minSize()
        if(velocity.x <= maxVelocity.x && velocity.y <= maxVelocity.y)
        {
            return true;
        }
        return false;
    }
}
