using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
    [SerializeField] private Vector2 _MaxJump;
    [SerializeField] private float _MaxRunningSpeed;

    private LevelManager levelManager;
    private WalkableChunk currentChunk;

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.FindFirstObjectByType<LevelManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void onChunkChange()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.transform.TryGetComponent<Polygon>(out Polygon polygon))
        {
            // TODO - 
            currentChunk = polygon.getWalkableChunkTouching(collision);
            onChunkChange();
        }
    }
}
