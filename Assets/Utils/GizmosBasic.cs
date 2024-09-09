using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyUtils
{
    public static class GizmosBasic 
    {
        public static void drawArrow(Vector2 start, Vector2 end, float arrowSize, float angle = 25)
        {
            Vector2 dir = (end - start).normalized;
            Vector2 neck = end - dir * arrowSize;
            Vector2 perpend = new Vector2(dir.y, -dir.x);

            float offset = Mathf.Atan(Mathf.Deg2Rad * angle) * arrowSize;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(neck + perpend * offset, end);
            Gizmos.DrawLine(neck - perpend * offset, end);
        }
    }
}
