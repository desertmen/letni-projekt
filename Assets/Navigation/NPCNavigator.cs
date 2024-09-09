using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NPCMovent))]
public class NPCNavigator : MonoBehaviour
{
    [SerializeField] private Stategy _Strategy = Stategy.TIGHT_JUMP;
    [SerializeField] private float _MaxDistFromEdge;
    [SerializeField] private bool _ShowGizmos;
    
    [SerializeField] private GameObject target;
    [SerializeField] private TargetTest targetTest;

    private WalkableChunk currentChunk;
    private LevelManager levelManager;
    private NPCMovent npcMovement;
    private List<JumpNode> path;
    private Vector2 lastFirstNodePos = Vector2.positiveInfinity;
    private Vector2 maxJump;
    private Vector2 size;
    private int targetNodeIdx;
    private float maxRunningSpeed;
    
    private const float minFirstNodeDist = 1;
    private const float jumpPowerAdjustment = 0.05f;

    [Serializable] public enum Stategy
    {
        ANY_JUMP, TIGHT_JUMP
    }

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.FindFirstObjectByType<LevelManager>();
        size = GetComponent<SpriteRenderer>().bounds.size;
        npcMovement = GetComponent<NPCMovent>();
        npcMovement.setActionOnLanding(onLanding);
        maxJump = npcMovement.getMaxJump();
        maxRunningSpeed = npcMovement.getMaxRunningSpeed();
        updatePath();
        targetNodeIdx = 0;
    }

    // Update is called once per frame
    void Update()
    {
        followPath();
    }

    private void OnDrawGizmos()
    {
        if(path != null && _ShowGizmos)
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

    private bool jumped = false;
    private void followPath()
    {
        if (!npcMovement.isGrounded())
        {
            return;
        }
        
        if (reachedLastNode())
        {
            onTargetReached();
            return;
        }

        if(targetNodeIdx >= path.Count)
        {
            updatePath();
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

    private bool reachedLastNode()
    {
        return path != null && targetNodeIdx == path.Count - 1 && (path[path.Count - 1].chunk == currentChunk || isPointsUnderBBox(path[path.Count - 1].position));
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
            case Stategy.ANY_JUMP:
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
                Debug.Log($"jumpstart lerp, l: {leftHit.jumpVelocity}, r:{rightHit.jumpVelocity}, t: {t} = {size.x}/{intervalWidth}");
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
            Debug.Log($"interval lerp, l: {-leftHit.impactVelocity}, r:{-rightHit.impactVelocity}, t: {t}");
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
            if (isPointsUnderBBox(currNode.info.jumpStart))
                return true;
            return false;
        }
        else // node is interval -> allign jump according to strategy
        {
            switch (_Strategy)
            {
                case Stategy.ANY_JUMP:
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
        bool isInInterval = MyUtils.Math.isPointInClosedInterval((interval.Item1.position.x, interval.Item2.position.x), transform.position.x);
        JumpHit targetHit = nextNode.position.x - currNode.position.x > 0 ? rightHit : leftHit;
        return isInInterval && Mathf.Abs(targetHit.position.x - pos) < size.x / 2f;
    }

    private bool shouldJumpWithStategy_ANY_JUMP(JumpNode currNode)
    {
        Tuple<JumpHit, JumpHit> interval = currNode.info.hitInterval;
        return MyUtils.Math.isPointInClosedInterval((interval.Item1.position.x, interval.Item2.position.x), transform.position.x);
    }

    private bool isPointsUnderBBox(Vector2 position)
    {
        float left = transform.position.x - size.x/2f;
        float right = transform.position.x + size.x/2f;
        return position.y <= transform.position.y && left <= position.x && position.x <= right;
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
        if(path != null)
            lastFirstNodePos = path[0].position;
        path = levelManager.getPath(transform.position, target.transform.position, maxJump);
        if(path.Count == 0)
        {
            targetTest.changePosition();
            updatePath();
            return;
        }
        // path should start on same chunk as box
        targetNodeIdx = 0;
        currentChunk = path[0].chunk;
    }

    private void onTargetReached()
    {
        targetTest.changePosition();
        updatePath();
    }

    private void onLanding(Polygon landingPolygon)
    {
        WalkableChunk newWalkableChunk = landingPolygon.getWalkableChunkUnderPoint(transform.position);
        Debug.Log("WALKABLE CHUNK CANGED, grounded: " + npcMovement.isGrounded());
        currentChunk = newWalkableChunk;
        
        // todo - check underJump / overJump -> change jumpPower of node
        // targetNodeIdx - 1, becouse +1 after jump
        if(path != null && targetNodeIdx > 0)
        {
            JumpNode jumpedFromNode = path[targetNodeIdx - 1];
            JumpNode currNode = path[targetNodeIdx];

            if(currNode.chunk != newWalkableChunk && jumped)
            {
                int jumpDir = (int)Mathf.Sign(currNode.position.x - jumpedFromNode.position.x);
                int dirToCurr = (int)Mathf.Sign(currNode.position.x - transform.position.x);
                // overJump
                if(jumpDir != dirToCurr)
                {
                    jumpedFromNode.removeJumpPower(jumpPowerAdjustment);
                    Debug.LogError("Jump power decreased");
                }
                // underJump
                else
                {
                    jumpedFromNode.addJumpPower(jumpPowerAdjustment);
                    Debug.LogError("Jump power incresed");
                }
            }
        }
    }

    public void onTargetChunkChange()
    {

    }

    public float getMaxRunningSpeed()
    {
        return maxRunningSpeed;
    }

}
