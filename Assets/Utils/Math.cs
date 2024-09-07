using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MyUtils
{
    public static class Math
    {
        public static Vector2 rotateVec2AroundOrigin(Vector2 pos, float radAngle)
        {
            float2x2 rot = new float2x2(Mathf.Cos(radAngle), -Mathf.Sin(radAngle),
                                        Mathf.Sin(radAngle), Mathf.Cos(radAngle));
            float2 vec = new float2(pos.x, pos.y);
            vec = Unity.Mathematics.math.mul(rot, vec);
            return new Vector2(vec.x, vec.y);
        }

        // returns Vector2.positiveInfinity if lines are colinear
        public static Vector2 getLinesIntersection(Vector2 A1,  Vector2 A2, Vector2 B1, Vector2 B2)
        {
            float x1 = A1.x;
            float y1 = A1.y;
            float x2 = A2.x;
            float y2 = A2.y;
            float x3 = B1.x;
            float y3 = B1.y;
            float x4 = B2.x;
            float y4 = B2.y;
            if ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4) == 0)
            {
                return Vector2.positiveInfinity;
            }
            float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4))
                    / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            /*
            float u = ((y1 - y2) * (x1 - x3) - (x1 - x2) * (y1 - y3))
                    / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            */
            return A1 + t * (A2 - A1);
        }

        public static bool linesCollide(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
        {
            float x1 = A1.x;
            float y1 = A1.y;
            float x2 = A2.x;
            float y2 = A2.y;
            float x3 = B1.x;
            float y3 = B1.y;
            float x4 = B2.x;
            float y4 = B2.y;
            float Tnumerator = (x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4);
            float Tdenominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Tdenominator == 0)
            {
                if(Tnumerator == 0)
                {
                    return true;
                }
                return false;
            }
            float Unumerator = (y1 - y2) * (x1 - x3) - (x1 - x2) * (y1 - y3);
            float Udenominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Mathf.Sign(Tnumerator) == Mathf.Sign(Tdenominator) && Mathf.Abs(Tdenominator) >= Mathf.Abs(Tnumerator) &&
                Mathf.Sign(Unumerator) == Mathf.Sign(Udenominator) && Mathf.Abs(Udenominator) >= Mathf.Abs(Unumerator))
            {
                return true;
            }
            return false;
        }


        public static bool intervalsOverlap((float, float) interval1, (float, float) interval2)
        {
            (float min1, float max1) = getMinMax<float>(interval1.Item1, interval1.Item2, (x) => x);
            (float min2, float max2) = getMinMax<float>(interval2.Item1, interval2.Item2, (x) => x);
            return !(max1 < min2 || min1 > max2);
        }

        public static bool isPointInClosedInterval((float, float) interval, float point)
        {
            (float min, float max) = getMinMax<float>(interval.Item1, interval.Item2, (x) => x);
            return min <= point && point <= max;
        }

        public static bool isPointInOpenInterval((float, float) interval, float point)
        {
            (float min, float max) = getMinMax<float>(interval.Item1, interval.Item2, (x) => x);
            return min < point && point < max;
        }

        public static float minBySize(float a, float b)
        {
            return Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
        }

        public static float absMin(float a, float b)
        {
            float min = Mathf.Min(Mathf.Abs(a), Mathf.Abs(b));
            return min;
        }

        public static float maxSize(float a, float b)
        {
            float max = Mathf.Max(Mathf.Abs(a), Mathf.Abs(b));
            return max;
        }

        public static Vector2 projectPointOnLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 AB = lineEnd - lineStart;
            Vector2 AP = point - lineStart;
            return (Vector2.Dot(AB, AP) / Vector2.Dot(AB, AB)) * AB + lineStart;
        }
        public static (T, T) getMinMax<T>(T a, T b, Func<T, float> getValue)
        {
            return getValue(a) < getValue(b) ? (a , b) : (b, a);
        }
    }
}
