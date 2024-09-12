using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonReference : MonoBehaviour
{
    public Polygon polygon { get; private set; }
    
    public void setPolygon(Polygon polygon)
    {
        this.polygon = polygon;
    }

    private void Start()
    {
        if (!transform.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            Debug.LogWarning(gameObject.name + ", does not have tag 'Platform");
        }

    }
}
