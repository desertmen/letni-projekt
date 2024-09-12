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

        public static void drawAnimationCurve(Vector2 pos, Vector2 size, AnimationCurve curve)
        {
            float borderWidth = 0.2f;
            Gizmos.color = new Color(0.05f, 0.05f, 0.05f);
            Gizmos.DrawCube(pos, size + Vector2.one * borderWidth);
            Gizmos.color = new Color(0.15f, 0.15f, 0.15f);
            Gizmos.DrawCube(pos, size);
            float step = 0.01f;
            Gizmos.color = Color.green;
            for (float t = 0; t + step <= 1f; t += step)
            {
                float val1 = curve.Evaluate(t);
                float val2 = curve.Evaluate(t + step);
                Vector2 BL = pos - size / 2f;

                Gizmos.DrawLine(BL + new Vector2(t, val1) * size, BL + new Vector2(t + step, val2) * size);
            }
        }
    }
}
