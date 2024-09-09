using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(JumpGenerator.JumpGeneratorInput))]
public class JumpGeneratorDrawer : PropertyDrawer
{
    float lineHeight = EditorGUIUtility.singleLineHeight;
    float textWidth = 50;
    float indent = 5;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        Rect modeRect = new Rect(position.x, position.y, 150, lineHeight);

        SerializedProperty mode = property.FindPropertyRelative("mode");
        EditorGUI.PropertyField(modeRect, mode, GUIContent.none);

        if(mode.enumValueFlag == (int)JumpGenerator.Mode.DIRECTED_JUMP)
        {
            Rect labelRect = new Rect(position.x - textWidth - indent, position.y + lineHeight + indent, textWidth, lineHeight);
            Rect valueRect = new Rect(position.x, position.y + lineHeight + indent, 150, lineHeight);
            SerializedProperty direction = property.FindPropertyRelative("direction");
            EditorGUI.PropertyField(valueRect, direction, GUIContent.none);
            EditorGUI.LabelField(labelRect, "Direction: ");
        }
        else
        {
            Rect labelRect = new Rect(position.x - textWidth - indent, position.y + lineHeight + indent, textWidth + 50, lineHeight);
            Rect valueRect = new Rect(position.x + 50, position.y + lineHeight + indent, 100, lineHeight);
            SerializedProperty velocity = property.FindPropertyRelative("velocity");
            EditorGUI.PropertyField(valueRect, velocity, GUIContent.none);
            EditorGUI.LabelField(labelRect, "Const Velocity: ");
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return lineHeight * 2 + indent;
    }
}
