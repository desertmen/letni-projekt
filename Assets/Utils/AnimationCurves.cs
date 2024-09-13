using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyUtils
{
    public static class AnimationCurves
    {
        public static float getCurveDuration(AnimationCurve curve)
        {
            return curve.keys.Last().time;
        }

        public static float getMaxValue(AnimationCurve curve)
        {
            float max = float.MinValue;
            foreach(var key in curve.keys)
            {
                max = key.value > max ? key.value : max;
            }
            return max;
        }

        public static float getMinValue(AnimationCurve curve)
        {
            float min = float.MaxValue;
            foreach (var key in curve.keys)
            {
                min = key.value < min ? key.value : min;
            }
            return min;
        }

        public static float getTimeOnAscendingCurve(float targetValue, AnimationCurve curve)
        {
            float maxTime = getCurveDuration(curve);
            if (curve.Evaluate(0) > targetValue)
                return 0;
            if (curve.Evaluate(maxTime) < targetValue)
                return maxTime;
            float minDist = 0.001f;
            float center = 0;
            float left = 0, right = maxTime;
            while (right - left > minDist)
            {
                center = (right + left) / 2f;
                float value = curve.Evaluate(center);
                // go left
                if (value > targetValue)
                {
                    right = center;
                }
                // go right
                else if (value < targetValue)
                {
                    left = center;
                }
                else
                    return center;
            }
            return center;
        }

        public static float getTimeOnDescendingCurve(float targetValue, AnimationCurve curve)
        {
            float maxTime = getCurveDuration(curve);
            if (curve.Evaluate(0) < targetValue)
                return 0;
            if (curve.Evaluate(maxTime) > targetValue)
                return maxTime;
            float minDist = 0.001f;
            float center = 0;
            float left = 0, right = maxTime;
            while (right - left > minDist)
            {
                center = (right + left) / 2f;
                float value = curve.Evaluate(center);
                // go left
                if (value < targetValue)
                {
                    right = center;
                }
                // go right
                else if (value > targetValue)
                {
                    left = center;
                }
                else
                    return center;
            }
            return center;
        }
    }
}
