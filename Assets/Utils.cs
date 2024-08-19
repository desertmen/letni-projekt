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
}
