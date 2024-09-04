using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMovent))]
public class NPCNavigator : MonoBehaviour
{
    [SerializeField] private Stategy _Strategy = Stategy.TIGHT_JUMP;
    [SerializeField] private float _MaxDistFromEdge;
    
    [SerializeField] private GameObject target;
    [SerializeField] private TargetTest targetTest;

    private NPCMovent npcMovement;
    private LevelManager levelManager;
    private WalkableChunk currentChunk;
    private List<JumpNode> path;
    private int targetNodeIdx;
    private Vector2 size;
    private Vector2 maxJump;
    private float maxRunningSpeed;
    
    [Serializable] public enum Stategy
    {
        ANY_JUMP, TIGHT_JUMP
    }

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.FindFirstObjectByType<LevelManager>();
        size = GetComponent<BoxCollider2D>().bounds.size;
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
        if(path != null)
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

    private List<JumpNode> lastPath = null;
    private float jumpMultiplier = 1;
    private void followPath()
    {
        JumpNode currNode = path[targetNodeIdx];
        JumpNode nextNode = targetNodeIdx < path.Count - 1 ? path[targetNodeIdx + 1] : null;

        if (reachedLastNode())
        {
            targetTest.changePosition();
            updatePath();
            return;
        }

        // missed jump -> update path
        if(isNotOnPath(currNode))
        {
            updatePath();
            if (isPathRepeating())
                jumpMultiplier += 0.2f;
            return;
        }
        jumpMultiplier = isPathRepeating() ? jumpMultiplier : 1;

        tryToShortenPath(ref currNode, ref nextNode);

        if (!npcMovement.isGrounded())
        {
            return;
        }
        else if (shouldJump(currNode, nextNode))
        {
            Vector2 jumpVeloctiy = getJumpVelocity(currNode, nextNode);
            npcMovement.jump(jumpVeloctiy * jumpMultiplier);
            targetNodeIdx++;
        }
        else
        {
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
        return targetNodeIdx == path.Count - 1 && isPointsUnderBBox(path[path.Count - 1].position);
    }

    private bool isNotOnPath(JumpNode currNode)
    {
        return npcMovement.isGrounded() && currentChunk != currNode.chunk;
    }

    private bool isPathRepeating()
    {
        bool samePath = lastPath != null && lastPath.Count == path.Count;
        if (samePath)
        {
            for (int i = 0; i < lastPath.Count; i++)
            {
                if (path[i] != lastPath[i])
                {
                    samePath = false;
                    break;
                }
            }
        }
        return samePath;
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
            if (intervalWidth > size.x)
            {
                int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);
                float t = (size.x) / intervalWidth;
                t = -dir * t + ((dir + 1) / 2f);
                t = 1 - t;
                Debug.Log($"jumpstart lerp, l: {leftHit.jumpVelocity}, r:{rightHit.jumpVelocity}, t: {t}");
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
            if (intervalWidth > size.x)
            {
                int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);
                float t = (size.x) / intervalWidth;
                t = -dir * t + ((dir + 1) / 2f);
                Debug.Log($"interval lerp, l: {-leftHit.impactVelocity}, r:{-rightHit.impactVelocity}, t: {t}");
                return Vector2.Lerp(-leftHit.impactVelocity, -rightHit.impactVelocity, t);
            }
            else
            {
                return Vector2.Lerp(-leftHit.impactVelocity, -rightHit.impactVelocity, 0.5f);
            }
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
                    return shouldJumpWithStategy_ANY_JUMP(currNode, nextNode);
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
        Vector2 BLcorner = (Vector2)transform.position - size / 2f;
        Vector2 BRcorner = (Vector2)transform.position + new Vector2(size.x / 2f, -size.y / 2f);
        int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);

        if (dir == MyUtils.Constants.RIGHT)
        {
            float dist = rightHit.position.x - BRcorner.x;
            return dist < _MaxDistFromEdge && leftHit.position.x <= BRcorner.x && BRcorner.x <= rightHit.position.x;
        }
        else
        {
            float dist = BLcorner.x - leftHit.position.x;
            return dist < _MaxDistFromEdge && leftHit.position.x <= BLcorner.x && BLcorner.x <= rightHit.position.x;
        }
    }

    private bool shouldJumpWithStategy_ANY_JUMP(JumpNode currNode, JumpNode nextNode)
    {
        Tuple<JumpHit, JumpHit> interval = currNode.info.hitInterval;
        (JumpHit leftHit, JumpHit rightHit) = MyUtils.Math.getMinMax<JumpHit>(interval.Item1, interval.Item2, (hit) => hit.position.x);
        Vector2 BLcorner = (Vector2)transform.position - size / 2f;
        Vector2 BRcorner = (Vector2)transform.position + new Vector2(size.x / 2f, -size.y / 2f);
        int dir = (int)Mathf.Sign(nextNode.position.x - currNode.position.x);

        if (dir == MyUtils.Constants.RIGHT)
        {
            return leftHit.position.x <= BRcorner.x && BRcorner.x <= rightHit.position.x;
        }
        else
        {
            return leftHit.position.x <= BLcorner.x && BLcorner.x <= rightHit.position.x;
        }
    }

    private bool isPointsUnderBBox(Vector2 position)
    {
        float left = transform.position.x - size.x;
        float right = transform.position.x + size.x;
        return position.y <= transform.position.y && left < position.x && position.x < right;
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
        lastPath = path;
        path = levelManager.getPath(transform.position, target.transform.position, maxJump, size.x);
        // path should start on same chunk as box
        targetNodeIdx = 0;
        currentChunk = path[0].chunk;
    }

    private void onLanding(Polygon landingPolygon)
    {
        WalkableChunk newWalkableChunk = landingPolygon.getWalkableChunkTouching(transform.position, size);
        Debug.Log("WALKABLE CHUNK CANGED" + newWalkableChunk.positions[0] + ", grounded : " + npcMovement.isGrounded());
        currentChunk = newWalkableChunk;
    }

    public void onTargetChunkChange()
    {

    }

    public float getMaxRunningSpeed()
    {
        return maxRunningSpeed;
    }

}
