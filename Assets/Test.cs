using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Range(0.0000001f, 1f)]
    public float _SmoothingFactor = 0.1f;
    [Range(0.0000001f, 1f)]
    public float _MinStep = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Func<float, float> func = x => (float)Math.Sin(x);
        MyGizmos.FunctionGizmo.draw(0, 7, 1, func);

        Gizmos.DrawWireSphere(new Vector3(7, 0, 0), 0.3f);
    }
}
