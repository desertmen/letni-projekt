using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelManager))]
[CanEditMultipleObjects]
public class LevelManagerEditor : Editor
{
    SerializedProperty jumpGeneratorList;
    LevelManager levelManager;
    private void OnEnable()
    {
        levelManager = (LevelManager)target;
        jumpGeneratorList = serializedObject.FindProperty("_JumpGenerators");
    }

    void OnSceneGUI()
    {
        if (levelManager._ShowHandlesPath)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newTarget1Pos = Handles.PositionHandle(levelManager.startHandle, Quaternion.identity);
            Vector3 newTarget2Pos = Handles.PositionHandle(levelManager.goalHandle, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(levelManager, "Change test handle position");
                levelManager.startHandle = newTarget1Pos;
                levelManager.goalHandle = newTarget2Pos;
            }
        }
    }

    
    private bool prevShowTrajectory = false;
    private bool prevShowAllTrajectory = false;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // show all intervals switched
        if (levelManager._ShowAllIntervalsOnChunk != prevShowAllTrajectory)
        {
            levelManager._ShowTrajectories = levelManager._ShowAllIntervalsOnChunk;
            prevShowAllTrajectory = levelManager._ShowAllIntervalsOnChunk;
        }
        // show trajectory swithced
        if (levelManager._ShowTrajectories != prevShowTrajectory)
        {
            if (levelManager._ShowTrajectories == false)
            {
                levelManager._ShowAllIntervalsOnChunk = false;
            }
            prevShowTrajectory = levelManager._ShowTrajectories;
        }
    }
}
