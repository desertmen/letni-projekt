using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NPCMovent))]
public class NPCNavigator : MonoBehaviour
{
    [SerializeField] private Stategy _Strategy = Stategy.TIGHT_JUMP;
    [SerializeField] private bool _ShowGizmos;

    private LevelManager levelManager;
    private NPCMovent npcMovement;
    private WalkableChunk currentChunk;
    private List<JumpNode> path;
    private Vector2 maxJump;
    private Vector2 size;
    private Vector2 target;
    private int targetNodeIdx;
    private float maxRunningSpeed;
    private Action onTargetReached;
    private Action onLastNodeReached;
    private Action onPathNotFound;
    private Action onInitialized;
    private bool jumped = false;
    private bool initialized = false;
    private bool lastNodeReachedCalled = false;
    [HideInInspector] public bool navigate = false;

    private const float jumpPowerAdjustment = 0.05f;

    [Serializable] public enum Stategy
    {
        MEDIUM_JUMP, TIGHT_JUMP
    }

    //  TODO remove target stuff -> create test NPC behaviour

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.FindFirstObjectByType<LevelManager>();
        size = GetComponent<SpriteRenderer>().bounds.size;
        npcMovement = GetComponent<NPCMovent>();
        npcMovement.setActionOnLanding(onLanding);
        npcMovement.setActionOnGrounded(onGrounded);
        maxJump = npcMovement.getMaxJump();
        maxRunningSpeed = npcMovement.getMaxRunningSpeed();
        path = new List<JumpNode>();
        targetNodeIdx = 0;
        if(!levelManager.innitNPC(size, npcMovement.getMaxAngle()))
        {
            Debug.LogError($"NPC <{gameObject.name}> init failed");
            enabled = false;
            return;
        }
        runOnInitialized();
    }

    // Update is called once per frame
    void Update()
    {
        // if on initialized is called after start
        if (!initialized)
            runOnInitialized();

        if(navigate)
            followPath();
    }

    private void OnDrawGizmos()
    {
        if(path != null && path.Count > 1 && _ShowGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(path[targetNodeIdx].position, 0.2f);

            for(int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(path[i].position, 0.1f);
                Gizmos.DrawLine(path[i].position, path[i + 1].position);
            }
            Gizmos.DrawSphere(path[path.Count - 1].position, 0.1f);
        }
    }

    /// <summary>
    /// action is called when NPCNavigator is initialized, or first time this function is called.
    /// This is needed, becouse of Start() order unpredictability.
    /// </summary>
    public void setOnInitialized(Action action)
    {
        onInitialized = action;
    }

    public void goToPosition(Vector2 target)
    {
        this.target = target;
        navigate = true;
        updatePath();
    }

    public void setOnPathNotFound(Action action)
    {
        onPathNotFound = action;
    }

    public void setOnLastNodeReached(Action action)
    {
        onLastNodeReached = action;
    }

    public void setOnTargetReached(Action action)
    {
        onTargetReached = action;
    }

    public float getMaxRunningSpeed()
    {
        return maxRunningSpeed;
    }

    private void followPath()
    {
        if (!npcMovement.isGrounded())
        {
            return;
        }
        
        if (reachedLastNode())
        {
            callOnTargetNodeReached();
            goTowardsTarget();
            return;
        }

        JumpNode currNode = path[targetNodeIdx];
        JumpNode nextNode = targetNodeIdx < path.Count - 1 ? path[targetNodeIdx + 1] : null;

        // missed jump -> update path
        if(isNotOnPath(currNode))
        {
            updatePath();
            return;
        }

        tryToShortenPath(ref currNode, ref nextNode);
        
        if (shouldJump(currNode, nextNode))
        {
            jumped = true;
            Vector2 jumpVeloctiy = getJumpVelocity(currNode, nextNode);
            npcMovement.jump(jumpVeloctiy * currNode.getJumpPower());
            targetNodeIdx++;
        }
        else
        {
            jumped = false;
            int dir = getDirToTarget(currNode, nextNode);
            if (dir == MyUtils.Constants.RIGHT)
            {
                npcMovement.startGoingRight();
            }
            else
            {
                npcMovement.startGoingLeft();
            }
        }
    }

    private void goTowardsTarget()
    {
        if (canWalkToTarget())
        {
            if (isPointsUnderOrOverBBox(target))
                callOnTargetReached();
            else
                goTowardsPoint(target);
        }
        else
        {
            Vector2 edge = getChunkEdgeClosestToPoint(target);
            if (isPointsUnderOrOverBBox(edge))
                callOnTargetReached();
            else
                goTowardsPoint(edge);
        }
    }

    private void goTowardsPoint(Vector2 pos)
    {
        int dir = (int)Mathf.Sign(pos.x - transform.position.x);
        if (dir == MyUtils.Constants.RIGHT)
        {
            npcMovement.startGoingRight();
        }
        else
        {
            npcMovement.startGoingLeft();
        }
    }

    private bool canWalkToTarget()
    {
        JumpNode lastNode = path.Last();
        return MyUtils.Math.isPointInClosedInterval((lastNode.chunk.positions.First().x, lastNode.chunk.positions.Last().x), target.x);
    }

    private bool reachedLastNode()
    {
        return path != null && targetNodeIdx == path.Count - 1 && (path[path.Count - 1].chunk == currentChunk || isPointsUnderOrOverBBox(path[path.Count - 1].position));
    }

    private bool isNotOnPath(JumpNode currNode)
    {
        return npcMovement.isGrounded() && currentChunk != currNode.chunk;
    }


    private int getDirToTarget(JumpNode currNode, JumpNode nextNode)
    {
        Vector2 target;
        if(nextNode == null || isNodeJumpStart(currNode))
        {
            target = currNode.position;
        }
        else
        {
            (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(currNode.info.hitInterval.Item1, currNode.info.hitInterval.Item2, (hit) => hit.position.x);
            int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);
            if (dir == MyUtils.Constants.RIGHT)
            {
                target = rightHit.position + Vector2.left * size.x / 2f;
            }
            else
            {
                target = leftHit.position + Vector2.right * size.x / 2f;
            }
        }
        return (int)Mathf.Sign(target.x - transform.position.x);
    }

    private Vector2 getJumpVelocity(JumpNode currNode, JumpNode nextNode)
    {
        switch(_Strategy)
        {
            case Stategy.MEDIUM_JUMP:
                return getJumpVelocityFromStategy_ANY_JUMP(currNode, nextNode);
            case Stategy.TIGHT_JUMP:
                return getJumpVelocityFromStategy_TIGHT_JUMP(currNode, nextNode);
            default:
                return Vector2.zero;
        }
    }

    private Vector2 getJumpVelocityFromStategy_TIGHT_JUMP(JumpNode currNode, JumpNode nextNode)
    {
        if (isNodeJumpStart(currNode))
        {
            (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(nextNode.info.hitInterval.Item1, nextNode.info.hitInterval.Item2, (hit) => hit.position.x);
            float intervalWidth = rightHit.position.x - leftHit.position.x;
            if (intervalWidth > size.x * 1.5f)
            {
                int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);
                float t = size.x * 1.5f / intervalWidth;
                t = -dir * t + ((dir + 1) / 2f);
                t = 1 - t;
                return Vector2.Lerp(leftHit.jumpVelocity, rightHit.jumpVelocity, t);
            }
            else
            {
                return Vector2.Lerp(leftHit.jumpVelocity, rightHit.jumpVelocity, 0.5f);
            }
        }
        else
        {
            (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(currNode.info.hitInterval.Item1, currNode.info.hitInterval.Item2, (hit) => hit.position.x);
            float intervalWidth = rightHit.position.x - leftHit.position.x;
            int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);                              
            float t = (transform.position.x - dir * size.x / 2f - leftHit.position.x) / intervalWidth;
            return Vector2.Lerp(-leftHit.impactVelocity, -rightHit.impactVelocity, t);
        }
    }

    private Vector2 getJumpVelocityFromStategy_ANY_JUMP(JumpNode currNode, JumpNode nextNode)
    {
        if (isNodeJumpStart(currNode))
        {
            (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(nextNode.info.hitInterval.Item1, nextNode.info.hitInterval.Item2, (hit) => hit.position.x);
            return Vector2.Lerp(leftHit.jumpVelocity, rightHit.jumpVelocity, 0.5f);
        }
        else
        {
            (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(currNode.info.hitInterval.Item1, currNode.info.hitInterval.Item2, (hit) => hit.position.x);
            return Vector2.Lerp(-leftHit.impactVelocity, -rightHit.impactVelocity, 0.5f);
        }
    }

    private void tryToShortenPath(ref JumpNode currNode, ref JumpNode nextNode)
    {

        while(targetNodeIdx < path.Count - 1)
        {
            // is next node on same chunk
            if (currNode.chunk == nextNode.chunk)
            { 
                targetNodeIdx++;
                currNode = nextNode;
                nextNode = targetNodeIdx < path.Count - 1 ? path[targetNodeIdx + 1] : null;
            }
            else break;
        }
    }

    private bool shouldJump(JumpNode currNode, JumpNode nextNode)
    {
        if (nextNode == null || currNode.chunk == nextNode.chunk)
            return false;
        
        if (isNodeJumpStart(currNode))
        {
            if (isPointsUnderOrOverBBox(currNode.info.jumpStart))
                return true;
            return false;
        }
        else // node is interval -> allign jump according to strategy
        {
            switch (_Strategy)
            {
                case Stategy.MEDIUM_JUMP:
                    // is box inside hit interval
                    return shouldJumpWithStategy_ANY_JUMP(currNode);
                case Stategy.TIGHT_JUMP:
                    // is box close enough to edge of interval
                    return shouldJumpWithStategy_TIGHT_JUMP(currNode, nextNode);
            }
        }
        return false;
    }

    private bool shouldJumpWithStategy_TIGHT_JUMP(JumpNode currNode, JumpNode nextNode)
    {
        Tuple<JumpHit, JumpHit> interval = currNode.info.hitInterval;
        (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(interval.Item1, interval.Item2, (hit) => hit.position.x);
        float pos = transform.position.x;
        bool isInInterval = MyUtils.Math.isPointInClosedInterval((leftHit.position.x + size.x /2f, rightHit.position.x - size.x /2f), transform.position.x) || 
            (rightHit.position.x - leftHit.position.x < size.x && MyUtils.Math.isPointInClosedInterval((leftHit.position.x, rightHit.position.x), transform.position.x));
        JumpHit targetHit = nextNode.position.x - currNode.position.x > 0 ? rightHit : leftHit;
        return isInInterval && Mathf.Abs(targetHit.position.x - pos) < size.x / 2f + 0.1f;
    }

    private bool shouldJumpWithStategy_ANY_JUMP(JumpNode currNode)
    {
        Tuple<JumpHit, JumpHit> interval = currNode.info.hitInterval;
        return MyUtils.Math.isPointInClosedInterval((interval.Item1.position.x, interval.Item2.position.x), transform.position.x);
    }

    private bool isPointUnderBBox(Vector2 position)
    {
        float left = transform.position.x - size.x/2f;
        float right = transform.position.x + size.x/2f;
        return position.y <= transform.position.y && left <= position.x && position.x <= right;
    }

    private bool isPointsUnderOrOverBBox(Vector2 position)
    {
        float left = transform.position.x - size.x / 2f;
        float right = transform.position.x + size.x / 2f;
        return left <= position.x && position.x <= right;
    }

    private Vector2 getChunkEdgeClosestToPoint(Vector2 position)
    {
        JumpNode lastNode = path.Last();
        (Vector2 left, Vector2 right) = MyUtils.Math.getMinMax<Vector2>(lastNode.chunk.positions.First(), lastNode.chunk.positions.Last(), (vec) => vec.x);
        return Vector2.Distance(position,left) < Vector2.Distance(position, right) ? left : right;
    }


    private bool isNodeJumpStart(JumpNode node)
    {
        return node.info.jumpStart == node.position;
    }

    private bool isNodeInterval(JumpNode node)
    {
        return node.info.jumpStart != node.position;
    }

    private void updatePath()
    {
        if(npcMovement == null)
        {
            Debug.LogError("Update path called before initialization, use setOnInitialized() to call funtion right after initialization");
            return;
        }
        path = levelManager.getPath(transform.position, target, maxJump, size, npcMovement.getMaxAngle());
        if(path.Count == 0)
        {
            callOnPathNotFound();
            return;
        }
        // path should start on same chunk as box
        targetNodeIdx = 0;
        currentChunk = path[0].chunk;
        lastNodeReachedCalled = false;
    }

    private void runOnInitialized()
    {
        if (onInitialized != null)
        {
            onInitialized();
            initialized = true;
        }
    }

    private void callOnPathNotFound()
    {
        if(onPathNotFound != null)
            onPathNotFound();
    }

    private void callOnTargetReached()
    {
        npcMovement.stopMoving();
        if(onTargetReached != null)
            onTargetReached();
    }

    private void callOnTargetNodeReached()
    {
        if(onLastNodeReached != null && lastNodeReachedCalled == false)
            onLastNodeReached();
        lastNodeReachedCalled = true;
    }

    private void onLanding(Polygon landingPolygon)
    {
        WalkableChunk newWalkableChunk = landingPolygon.getWalkableChunkUnderPoint(transform.position);
        currentChunk = newWalkableChunk;
        
        // targetNodeIdx - 1, becouse +1 after jump
        if(path != null && targetNodeIdx > 0)
        {
            JumpNode jumpedFromNode = path[targetNodeIdx - 1];
            JumpNode currNode = path[targetNodeIdx];

            bool sameChunk = newWalkableChunk != null && currNode.chunk.positions.Count == newWalkableChunk.positions.Count;
            int i = 0;
            while(sameChunk && i < currNode.chunk.positions.Count)
            {
                sameChunk = currNode.chunk.positions[i] == newWalkableChunk.positions[i];
                i++;
            }

            if (!sameChunk && jumped)
            {
                int jumpDir = (int)Mathf.Sign(currNode.position.x - jumpedFromNode.position.x);
                int dirToCurr = (int)Mathf.Sign(currNode.position.x - transform.position.x);
                // overJump
                if(jumpDir != dirToCurr)
                {
                    jumpedFromNode.removeJumpPower(jumpPowerAdjustment);
                    Debug.Log("Jump power decreased " + jumpedFromNode.getJumpPower());
                }
                // underJump
                else
                {
                    jumpedFromNode.addJumpPower(jumpPowerAdjustment);
                    Debug.Log("Jump power increased " + jumpedFromNode.getJumpPower());
                }
            }
        }
    }

    private void onGrounded(Polygon touchingPolygon)
    {
        if(currentChunk == null)
        {
            WalkableChunk newWalkableChunk = touchingPolygon.getWalkableChunkUnderPoint(transform.position);
            currentChunk = newWalkableChunk;
        }
    }
}
