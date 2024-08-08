using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkableChunk
{
    public List<Vector2> positions;
    public List<int> vertexIndicies;

    public WalkableChunk(List<int> vertexIndicies, List<Vector2> positions)
    {
        this.vertexIndicies = vertexIndicies;
        this.positions = positions;
    }
}
