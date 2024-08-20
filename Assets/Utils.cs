using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyUtils
{
    public static class Math
    {
        public static float minBySize(float a, float b)
        {
            float min = Mathf.Min(Mathf.Abs(a), Mathf.Abs(b));
            return min * Mathf.Sign(min);
        }

        public static float minSize(float a, float b)
        {
            float min = Mathf.Min(Mathf.Abs(a), Mathf.Abs(b));
            return min;
        }
    }

    public static class Logic
    {
        public static (Vector2, Vector2) getLeftRightVector2(Vector2 a, Vector2 b)
        {
            return a.x < b.x ? (a, b) : (b, a);
        }
    }
}
