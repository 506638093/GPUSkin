using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinGenerator))]
public class GPUSkinGeneratorEditor : Editor
{
    private bool guiEnabled = false;

    private float time = 0;

    public override void OnInspectorGUI()
    {
        GPUSkinGenerator gen = target as GPUSkinGenerator;
        if (gen == null)
        {
            return;
        }

        gen.CollectAnimationClips();

        OnGUIProperty(gen);
        OnPreview(gen);

    }

    private void Awake()
    {
        EditorApplication.update += UpdateHandler;

        if (!Application.isPlaying)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinGenerator.ReadTempData(GPUSkinGenerator.TEMP_SAVED_ANIM_PATH));
            if (obj != null && obj is GPUSkinAnimationData)
            {
                serializedObject.FindProperty("animData").objectReferenceValue = obj;
            }
            obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinGenerator.ReadTempData(GPUSkinGenerator.TEMP_SAVED_MESH_PATH));
            if (obj != null && obj is Mesh)
            {
                serializedObject.FindProperty("savedMesh").objectReferenceValue = obj;
            }
            
            serializedObject.ApplyModifiedProperties();

            GPUSkinGenerator.DeleteTempData(GPUSkinGenerator.TEMP_SAVED_ANIM_PATH);
            GPUSkinGenerator.DeleteTempData(GPUSkinGenerator.TEMP_SAVED_MESH_PATH);
        }
    }
    private void OnDestroy()
    {
        EditorApplication.update -= UpdateHandler;
    }

    private void UpdateHandler()
    {
        GPUSkinGenerator gen = target as GPUSkinGenerator;

        if (!gen.isSampling && gen.IsSamplingProgress())
        {
            if (++gen.samplingClipIndex < gen.animClips.Count)
            {
                gen.StartSample();
            }
            else
            {
                gen.EndSample();
                EditorApplication.isPlaying = false;
                LockInspector(false);
            }
        }
    }

    private void BeginBox()
    {
        EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
        EditorGUILayout.Space();
    }

    private void EndBox()
    {
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
    }

    private void LockInspector(bool isLocked)
    {
        System.Type type = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.InspectorWindow");
        FieldInfo field = type.GetField("m_AllInspectors", BindingFlags.Static | BindingFlags.NonPublic);
        System.Collections.ArrayList windows = new System.Collections.ArrayList(field.GetValue(null) as System.Collections.ICollection);
        foreach (var window in windows)
        {
            PropertyInfo property = type.GetProperty("isLocked");
            property.SetValue(window, isLocked, null);
        }
    }


    private void OnGUIProperty(GPUSkinGenerator gen)
    {
        guiEnabled = !Application.isPlaying;

        BeginBox();
        {
            GUI.enabled = guiEnabled;
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"), new GUIContent("Animation Name"));

                GUI.enabled = false;
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animData"), new GUIContent("Animation Data"));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("savedMesh"), new GUIContent("Saved Mesh"));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUI.enabled = guiEnabled;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("skinQuality"), new GUIContent("Quality"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("rootBoneTransform"), new GUIContent("Root Bone"));

                OnGUIAnimClips(gen);

            }
            GUI.enabled = true;

            if (!Application.isPlaying && gen.rootBoneTransform != null)
            {
                gen.rootTransformMatrix = Matrix4x4.TRS(gen.rootBoneTransform.localPosition, gen.rootBoneTransform.localRotation, gen.rootBoneTransform.localScale);
            }

            if (GUILayout.Button("Step1: Start Generate Animation Data"))
            {
                //DestroyPreview();
                EditorApplication.isPlaying = true;
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Step2: Sample And Generate "))
                {
                    LockInspector(true);
                    gen.BeginSample();
                    gen.StartSample();
                }
            }
        }
        EndBox();
                
    }

    private void OnGUIAnimClips(GPUSkinGenerator gen)
    {
        BeginBox();
        {
            if (!gen.IsAnimatorOrAnimation())
            {
                EditorGUILayout.HelpBox("Set AnimClips with Animation Component", MessageType.Info);
            }

            EditorGUILayout.PrefixLabel("Sample Clips");

            int animClips_count = gen.animClips.Count;

            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.IntField("Size", animClips_count);
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = guiEnabled;

            EditorGUILayout.BeginHorizontal();
            {
                for (int j = -1; j < 5; ++j)
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (j == -1)
                            {
                                GUILayout.Label("   ");
                            }
                            else if (j == 0)
                            {
                                GUILayout.Label("Name");
                            }
                            else if (j == 1)
                            {
                                GUILayout.Label("Frame Rate");
                            }
                            else if (j == 2)
                            {
                                GUILayout.Label("Wrap Mode");
                            }
                            else if (j == 3)
                            {
                                GUILayout.Label("Animation Clip");
                            }
                            else if (j == 4)
                            {
                                GUILayout.Label("Root Motion");
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        for (int i = 0; i < animClips_count; i++)
                        {
                            var data = gen.animClips[i];
                            if (j == -1)
                            {
                                GUILayout.Label((i + 1) + ":    ");
                            }
                            else if (j == 0)
                            {
                                data.name = EditorGUILayout.TextField(data.name);
                            }
                            else if (j == 1)
                            {
                                data.frameRate = EditorGUILayout.IntField(data.frameRate);
                            }
                            else if (j == 2)
                            {
                                data.wrapMode = (GPUSkinWrapMode)EditorGUILayout.EnumPopup(data.wrapMode);
                            }
                            else if (j == 3)
                            {
                                GUI.enabled = gen.IsAnimatorOrAnimation() && guiEnabled;
                                EditorGUILayout.ObjectField(data.clip, data.clip.GetType(), false);
                                GUI.enabled = guiEnabled;
                            }
                            else if (j == 4)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                data.rootMotion = GUILayout.Toggle(data.rootMotion, string.Empty);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EndBox();

    }

    private void OnPreview(GPUSkinGenerator gen)
    {
        BeginBox();
        {
            if (GUILayout.Button("Preview"))
            {
                
            }
        }
        EndBox();

        serializedObject.ApplyModifiedProperties();
    }
}
