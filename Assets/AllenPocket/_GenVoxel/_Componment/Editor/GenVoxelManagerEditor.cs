using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GenVoxelTools {
    [CustomEditor(typeof(GenVoxelManager))]
    [CanEditMultipleObjects]
    public class GenVoxelManagerEditor : Editor {

        private GenVoxelManager manager;
        private SerializedProperty positionTypeProperty;

        private SerializedProperty xProperty;
        private SerializedProperty zProperty;
        private SerializedProperty widthProperty;
        private SerializedProperty lengthProperty;

        void OnEnable()
        {
            manager = target as GenVoxelManager;
            positionTypeProperty = serializedObject.FindProperty("positionType");

            xProperty = serializedObject.FindProperty("x");
            zProperty = serializedObject.FindProperty("z");
            widthProperty = serializedObject.FindProperty("width");
            lengthProperty = serializedObject.FindProperty("length");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical();

            // Draw Path Field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wtAsset"));
            // Draw Rule Field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rule"));
            // Draw voxMaterial Field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("voxMaterial"));
            // Draw Area Field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Range");
            GUILayout.Label("W");
            widthProperty.intValue = EditorGUILayout.IntField(widthProperty.intValue);
            GUILayout.Label("L");
            lengthProperty.intValue = EditorGUILayout.IntField(lengthProperty.intValue);
            if (GUILayout.Button("Update") && manager != null)
            {
                manager.UpdateArea();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(positionTypeProperty);
            // Draw Normal Field
            if (positionTypeProperty.enumValueIndex == (int)(PositionType.Normal))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("Position"));
                GUILayout.Label("X");
                xProperty.intValue = EditorGUILayout.IntField(xProperty.intValue);
                GUILayout.Label("Z");
                zProperty.intValue = EditorGUILayout.IntField(zProperty.intValue);
                if (GUILayout.Button("Update") && manager != null)
                {
                    manager.UpdateArea();
                }
                EditorGUILayout.EndHorizontal();
            }
            else if(positionTypeProperty.enumValueIndex == (int)(PositionType.Transform))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
            }
            //Draw scale
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"));

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}