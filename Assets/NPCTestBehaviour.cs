using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCNavigator))]
public class NPCTestBehaviour : MonoBehaviour
{
    [SerializeField] Rect targetArea;

    private Vector2 target = Vector2.zero;
    private NPCNavigator navigation;

    private const float maxTime = 30;
    private float time = 0;

    // Start is called before the first frame update
    void Start()
    {
        navigation = GetComponent<NPCNavigator>();

        navigation.setOnInitialized(updateTarget);
        navigation.setOnPathNotFound(updateTarget);
        navigation.setOnTargetReached(updateTarget);
    }

    private void updateTarget()
    {
        time = 0;
        changeTargetPosition();
        navigation.goToPosition(target);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector2(targetArea.x, targetArea.y), new Vector2(targetArea.width, targetArea.height));
        time = Mathf.Clamp(time + Time.deltaTime, 0, maxTime);
        Gizmos.color = Color.Lerp(Color.green, Color.red, time / maxTime);
        Gizmos.DrawSphere(target, 0.3f);
    }

    private void changeTargetPosition()
    {
        target = MyUtils.Random.getRandomPointInRect(targetArea);
    }
}
