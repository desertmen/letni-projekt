using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(JumpFinder))]
public class JumpFinderEditor : Editor
{
    private void OnSceneGUI()
    {
        DrawDefaultInspector();

        JumpFinder jumpFinder = (JumpFinder)target;

        EditorGUI.BeginChangeCheck();
        Vector3 newTarget1Pos = Handles.PositionHandle(jumpFinder.startHandle, Quaternion.identity);
        Vector3 newTarget2Pos = Handles.PositionHandle(jumpFinder.goalHandle, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(jumpFinder, "Change test handle position");
            jumpFinder.startHandle = newTarget1Pos;
            jumpFinder.goalHandle = newTarget2Pos;
        }
        
    }
}
