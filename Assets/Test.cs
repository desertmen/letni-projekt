using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] Vector2 A1;
    [SerializeField] Vector2 A2;
    [SerializeField] Vector2 B1;
    [SerializeField] Vector2 B2;

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(A1, 0.1f);
        Gizmos.DrawSphere(A2, 0.1f);
        Gizmos.DrawSphere(B1, 0.1f);
        Gizmos.DrawSphere(B2, 0.1f);
        Gizmos.color = MyUtils.Math.linesCollide(A1, A2, B1, B2) ? Color.green : Color.red;
        Gizmos.DrawLine(A1, A2);
        Gizmos.DrawLine(B1, B2);
        Gizmos.DrawSphere(MyUtils.Math.getLinesIntersection(A1, A2, B1, B2), 0.1f);
    }
}
