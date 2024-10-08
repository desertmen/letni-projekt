using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpMap
{
    private List<WalkableChunk> walkableChunks;
    private Dictionary<WalkableChunk, List<WalkableChunk>> connections = new Dictionary<WalkableChunk, List<WalkableChunk>>();
    private Dictionary<WalkableChunk, List<JumpConnectionInfo>> outgoingIntervals = new Dictionary<WalkableChunk, List<JumpConnectionInfo>>();
    private Dictionary<WalkableChunk, List<JumpConnectionInfo>> incomingIntervals = new Dictionary<WalkableChunk, List<JumpConnectionInfo>>();

    private HashSet<Tuple<WalkableChunk, WalkableChunk>> alreadyConnectedPolygons = new HashSet<Tuple<WalkableChunk, WalkableChunk>>();

    private List<JumpNode> jumpNodes = new List<JumpNode>();
    private Dictionary<WalkableChunk, List<JumpNode>> jumpNodesPerChunk = new Dictionary<WalkableChunk, List<JumpNode>>();

    public JumpMap(List<WalkableChunk> walkableChunks)
    {
        this.walkableChunks = walkableChunks;
    }

    public void addConnection(JumpConnectionInfo jumpConnection)
    {
        addToListInDictionary<WalkableChunk, JumpConnectionInfo>(jumpConnection.startChunk, jumpConnection, outgoingIntervals);
        addToListInDictionary<WalkableChunk, JumpConnectionInfo>(jumpConnection.destinationChunk, jumpConnection, incomingIntervals);

        addChunkConnection(jumpConnection);

        addJumpNodes(jumpConnection);
    }   

    private void addJumpNodes(JumpConnectionInfo jumpConnection)
    {
        Vector2 intervalMiddle = (jumpConnection.hitInterval.Item1.position + jumpConnection.hitInterval.Item2.position) / 2.0f;
        JumpNode intervalNode = new JumpNode(intervalMiddle, jumpConnection.destinationChunk, jumpConnection, this);
        JumpNode startNode = new JumpNode(jumpConnection.jumpStart, jumpConnection.startChunk, jumpConnection, this);

        startNode.setIntervalNeighbour(intervalNode);
        intervalNode.setIntervalNeighbour(startNode);

        jumpNodes.Add(startNode);
        jumpNodes.Add(intervalNode);

        addToListInDictionary<WalkableChunk, JumpNode>(jumpConnection.startChunk, startNode, jumpNodesPerChunk);
        addToListInDictionary<WalkableChunk, JumpNode>(jumpConnection.destinationChunk, intervalNode, jumpNodesPerChunk);
    }

    public JumpNode getClosestJumpNode(Vector2 position)
    {
        float minDist = float.PositiveInfinity;
        JumpNode minNode = null;

        foreach(JumpNode jumpNode in jumpNodes)
        {
            float dist = Vector2.Distance(jumpNode.position, position);
            if (dist < minDist)
            {
                minDist = dist;
                minNode = jumpNode;
            }
        }
        return minNode;
    }

    public JumpNode getClosestJumpNodeUnderBox(Vector2 position, float boxWidth)
    {
        float minDist = float.PositiveInfinity;
        JumpNode minNode = null;

        foreach (JumpNode jumpNode in jumpNodes)
        {
            float dist = Vector2.Distance(jumpNode.position, position);
            bool isChunkNodeUnder = false;
            for(int i = 0; i < jumpNode.chunk.positions.Count - 1; i++)
            {
                (Vector2 left, Vector2 right) = MyUtils.Math.getMinMax<Vector2>(jumpNode.chunk.positions[i], jumpNode.chunk.positions[i + 1], (pos) => pos.x);
                if(left.x <= position.x + boxWidth/2 && position.x - boxWidth/2 <= right.x && (position.y >= left.y || position.y >= right.y))
                {
                    isChunkNodeUnder = true;
                    break;
                }
            }
            if (dist < minDist && isChunkNodeUnder)
            {
                minDist = dist;
                minNode = jumpNode;
            }
        }
        return minNode;
    }

    public List<JumpNode> getJumpNodesOnChunk(WalkableChunk chunk)
    {
        if(jumpNodesPerChunk.TryGetValue(chunk, out List<JumpNode> jumpNodes)) 
        {
            return jumpNodes; 
        }
        return new List<JumpNode>();
    }

    public List<JumpNode> getJumpNodes()
    {
        return jumpNodes;
    }

    public List<WalkableChunk> getAllWalkableChunks() { return walkableChunks; }
    public List<WalkableChunk> getWalkableChunks() { return walkableChunks; }

    public List<WalkableChunk> getConnectedChunks(WalkableChunk walkableChunk)
    {
        if(connections.TryGetValue(walkableChunk, out List<WalkableChunk> connectedChunks))
        {
            return connectedChunks;
        }
        return new List<WalkableChunk>();
    }
    public List<JumpConnectionInfo> getOutgoingConnections(WalkableChunk walkableChunk)
    {
        if(outgoingIntervals.TryGetValue(walkableChunk, out List<JumpConnectionInfo> jumpConnections))
        {
            return jumpConnections;
        }
        return new List<JumpConnectionInfo>();
    }

    public List<JumpConnectionInfo> getIncomingConnections(WalkableChunk walkableChunk)
    {
        if (incomingIntervals.TryGetValue(walkableChunk, out List<JumpConnectionInfo> jumpConnections))
        {
            return jumpConnections;
        }
        return new List<JumpConnectionInfo>();
    }

    private void addChunkConnection(JumpConnectionInfo jumpConnection)
    {
        if(!alreadyConnectedPolygons.Contains(new Tuple<WalkableChunk, WalkableChunk>(jumpConnection.startChunk, jumpConnection.destinationChunk)))
        {
            addToListInDictionary<WalkableChunk, WalkableChunk>(jumpConnection.startChunk, jumpConnection.destinationChunk, connections);
            alreadyConnectedPolygons.Add(new Tuple<WalkableChunk, WalkableChunk>(jumpConnection.startChunk, jumpConnection.destinationChunk));
            if(jumpConnection.startChunk != jumpConnection.destinationChunk)
            {
                addToListInDictionary<WalkableChunk, WalkableChunk>(jumpConnection.destinationChunk, jumpConnection.startChunk, connections);
                alreadyConnectedPolygons.Add(new Tuple<WalkableChunk, WalkableChunk>(jumpConnection.destinationChunk, jumpConnection.startChunk));
            }
        }
    }

    private void addToListInDictionary<K, V>(K key, V value, Dictionary<K, List<V>> dictionary)
    {
        if(!dictionary.TryGetValue(key, out List<V> list))
        {
            list = new List<V>();
            dictionary.Add(key, list);
        }
        list.Add(value);
    }
}

