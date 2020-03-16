using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(GPUSkinAnimation))]
public class GPUSkinAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GPUSkinAnimation animation = target as GPUSkinAnimation;
        if (animation == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animData"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("material"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingMode"), new GUIContent("Culling Mode"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("rootMotionEnabled"), new GUIContent("Apply Root Motion"));

        serializedObject.ApplyModifiedProperties();
    }
}
