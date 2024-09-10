using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTest : MonoBehaviour
{
    [SerializeField] private Vector2 center;
    [SerializeField] private Vector2 size;

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(center, size);
    }

    public void changePosition()
    {
        Vector2 extents = size / 2f;
        transform.position = new Vector2(Random.Range(center.x - extents.x, center.x + extents.x), Random.Range(center.y - extents.y, center.y + extents.y));
    }
}
