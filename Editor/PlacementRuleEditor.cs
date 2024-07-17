using UnityEditor;
using UnityEngine;

namespace ChanceGen.Editor
{
    [CustomEditor(typeof(PlacementRule))]
    public class PlacementRuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _placementMode;
        private SerializedProperty _placementOrientation;

        private SerializedProperty _placementRangeP;

        private SerializedProperty _placementRangeE;
        private SerializedProperty _clampable;

        private SerializedProperty _maxPlacements;
        private SerializedProperty _placementChance;
        private SerializedProperty _unique;

        private void OnEnable()
        {
            _placementMode = serializedObject.FindProperty("_placementMode");
            _placementOrientation = serializedObject.FindProperty("_placementOrientation");

            _placementRangeP = serializedObject.FindProperty("_placementRangeP");

            _placementRangeE = serializedObject.FindProperty("_placementRangeE");
            _clampable = serializedObject.FindProperty("_clampable");

            _maxPlacements = serializedObject.FindProperty("_maxPlacements");
            _placementChance = serializedObject.FindProperty("_placementChance");
            _unique = serializedObject.FindProperty("_unique");
        }

        public override void OnInspectorGUI()
        {
            // draw enum fields
            EditorGUILayout.PropertyField(_placementMode,
                new GUIContent(_placementMode.displayName, _placementMode.tooltip), true);
            EditorGUILayout.PropertyField(_placementOrientation,
                new GUIContent(_placementOrientation.displayName, _placementOrientation.tooltip), true);

            EditorGUILayout.Space();

            // draw mode
            if (_placementMode.enumValueIndex == (int)PlacementMode.Exact)
                ExactInspector();
            else
                ProportionalInspector();

            EditorGUILayout.Space();

            // draw rest of fields always present
            EditorGUILayout.PropertyField(_maxPlacements,
                new GUIContent(_maxPlacements.displayName, _maxPlacements.tooltip), true);
            EditorGUILayout.PropertyField(_placementChance,
                new GUIContent(_placementChance.displayName, _placementChance.tooltip), true);
            EditorGUILayout.PropertyField(_unique,
                new GUIContent(_unique.displayName, _unique.tooltip), true);
        }

        private void ProportionalInspector()
        {
            EditorGUILayout.PropertyField(_placementRangeP, new GUIContent("Placement Range", _placementRangeP.tooltip),
                true);
        }

        private void ExactInspector()
        {
            EditorGUILayout.PropertyField(_placementRangeE, new GUIContent("Placement Range", _placementRangeP.tooltip),
                true);
            EditorGUILayout.PropertyField(_clampable, new GUIContent(_clampable.displayName, _clampable.tooltip),
                true);
        }
    }
}