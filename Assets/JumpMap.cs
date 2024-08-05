using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpMap
{
    public List<Polygon> polygons;
    private Dictionary<Polygon, List<Polygon>> polygonConnections = new Dictionary<Polygon, List<Polygon>>();
    private Dictionary<Polygon, List<JumpConnection>> destinationIntervals = new Dictionary<Polygon, List<JumpConnection>>();
    private Dictionary<Polygon, List<JumpConnection>> intervalsOnPolygon = new Dictionary<Polygon, List<JumpConnection>>();

    private HashSet<Tuple<Polygon, Polygon>> alreadyConnectedPolygons = new HashSet<Tuple<Polygon, Polygon>>();

    public JumpMap(List<Polygon> polygons)
    {
        this.polygons = polygons;
    }

    public void addConnection(JumpConnection jumpConnection)
    {
        addToListInDictionary<Polygon, JumpConnection>(jumpConnection.startPolygon, jumpConnection, destinationIntervals);
        addToListInDictionary<Polygon, JumpConnection>(jumpConnection.destinationPolygon, jumpConnection, intervalsOnPolygon);
        
        // make sure polygonConnections are unique
        if(!alreadyConnectedPolygons.Contains(new Tuple<Polygon, Polygon>(jumpConnection.startPolygon, jumpConnection.destinationPolygon)))
        {
            addToListInDictionary<Polygon, Polygon>(jumpConnection.startPolygon, jumpConnection.destinationPolygon, polygonConnections);
            alreadyConnectedPolygons.Add(new Tuple<Polygon, Polygon>(jumpConnection.startPolygon, jumpConnection.destinationPolygon));
            if(jumpConnection.startPolygon != jumpConnection.destinationPolygon)
            {
                addToListInDictionary<Polygon, Polygon>(jumpConnection.destinationPolygon, jumpConnection.startPolygon, polygonConnections);
                alreadyConnectedPolygons.Add(new Tuple<Polygon, Polygon>(jumpConnection.destinationPolygon, jumpConnection.startPolygon));
            }
        }
    }   

    public List<Polygon> getConnectedPolygons(Polygon polygon)
    {
        if(polygonConnections.TryGetValue(polygon, out List<Polygon> polygons))
        {
            return polygons;
        }
        return new List<Polygon>();
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

