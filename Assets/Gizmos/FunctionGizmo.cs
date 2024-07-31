using System;
using Unity.VisualScripting;
using UnityEngine;

namespace MyGizmos {
    public static class FunctionGizmo
    {
        private static float offsetX = 0;
        private static float offsetY = 0;
        private static float scaleX = 1;
        private static float scaleY = 1;

        private static bool flip = false;
        
        public static void draw(float start, float end, float startStep, Func<float, float> function)
        {
            if(flip)
            {
                float tmp = start;
                start = -end;
                end = tmp;
            }
            float x = start;
            float y = function(x);
            float x2 = x + startStep;
            float y2 = function(x2);
            for (int i = 0; i < (end - start) / startStep; i++)
            {

                Gizmos.DrawLine(new Vector2(x * scaleX + offsetX, y* scaleY + offsetY), new Vector2(x2 * scaleX + offsetX, y2 * scaleY + offsetY));
                
                x = x2;
                y = y2;
                x2 = Math.Min(x2 + startStep, end);
                y2 = function(x2);
            }
        }

        public static void setOffset(float x, float y)
        {
            offsetX = x;
            offsetY = y;
        }

        public static void setScale(float x, float y)
        {
            scaleX = x;
            scaleY = y;
        }

        public static void setFlip(bool doFlip)
        {
            flip = doFlip;
        }
    }
}
