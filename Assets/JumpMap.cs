using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpMap
{
    // TODO -> froom polygon oriented to walkable chunk oriented
    private List<WalkableChunk> walkableChunks;
    private Dictionary<WalkableChunk, List<WalkableChunk>> connections = new Dictionary<WalkableChunk, List<WalkableChunk>>();
    private Dictionary<WalkableChunk, List<JumpConnection>> destinationIntervals = new Dictionary<WalkableChunk, List<JumpConnection>>();
    private Dictionary<WalkableChunk, List<JumpConnection>> intervalsOnWalkableChunk = new Dictionary<WalkableChunk, List<JumpConnection>>();

    private HashSet<Tuple<WalkableChunk, WalkableChunk>> alreadyConnectedPolygons = new HashSet<Tuple<WalkableChunk, WalkableChunk>>();

    public JumpMap(List<WalkableChunk> walkableChunks)
    {
        this.walkableChunks = walkableChunks;
    }

    public void addConnection(JumpConnection jumpConnection)
    {
        addToListInDictionary<WalkableChunk, JumpConnection>(jumpConnection.startChunk, jumpConnection, destinationIntervals);
        addToListInDictionary<WalkableChunk, JumpConnection>(jumpConnection.destinationChunk, jumpConnection, intervalsOnWalkableChunk);
        
        // make sure connections are unique
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

    public List<WalkableChunk> getAllWalkableChunks() { return walkableChunks; }
    public List<WalkableChunk> getPolygons() { return walkableChunks; }

    public List<WalkableChunk> getConnectedChunks(WalkableChunk walkableChunk)
    {
        if(connections.TryGetValue(walkableChunk, out List<WalkableChunk> connectedChunks))
        {
            return connectedChunks;
        }
        return new List<WalkableChunk>();
    }

    public List<JumpConnection> getDestinationConnecitons(WalkableChunk walkableChunk)
    {
        if(destinationIntervals.TryGetValue(walkableChunk, out List<JumpConnection> jumpConnections))
        {
            return jumpConnections;
        }
        return new List<JumpConnection>();
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

