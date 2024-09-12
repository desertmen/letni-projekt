using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyUtils
{
    public static class Random
    {
        public static Vector2 getRandomPointInRect(Rect rect)
        {
            float xOffset = UnityEngine.Random.Range(-rect.width / 2f, rect.width / 2f);
            float yOffset = UnityEngine.Random.Range(-rect.height / 2f, rect.height/ 2f);
            return new Vector2(xOffset, yOffset) + new Vector2(rect.x, rect.y);
        }

        public static Vector2 getRandomPointInRect(Vector2 center, Vector2 size)
        {
            float xOffset = UnityEngine.Random.Range(-size.x / 2f, size.x/ 2f);
            float yOffset = UnityEngine.Random.Range(-size.y / 2f, size.y / 2f);
            return new Vector2(xOffset, yOffset) + center;
        }
    }
}
