using ChanceGen.Attributes;
using UnityEditor;
using UnityEngine;

namespace ChanceGen.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
    public class ReadOnlyInInspectorPropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginDisabledGroup(true);

            EditorGUI.PropertyField(position, property, label, true);

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndProperty();
        }
    }
}