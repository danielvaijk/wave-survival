using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DecalMaterial))]
[CanEditMultipleObjects]
public class DecalMaterialEditor : Editor
{
    private SerializedProperty penetrability = null;

    private SerializedProperty splatterRange = null;

    private SerializedProperty doesSplatter = null;

    private SerializedProperty entryDecals = null;
    private SerializedProperty entryParticles = null;

    private SerializedProperty exitDecals = null;
    private SerializedProperty exitParticles = null;

    private SerializedProperty splatterDecals = null;

    private void OnEnable ()
    {
        penetrability = serializedObject.FindProperty("penetrability");

        splatterRange = serializedObject.FindProperty("splatterRange");

        doesSplatter = serializedObject.FindProperty("doesSplatter");

        entryDecals = serializedObject.FindProperty("entryDecals");
        entryParticles = serializedObject.FindProperty("entryParticles");

        exitDecals = serializedObject.FindProperty("exitDecals");
        exitParticles = serializedObject.FindProperty("exitParticles");

        splatterDecals = serializedObject.FindProperty("splatterDecals");
    }

    public override void OnInspectorGUI ()
    {
        DecalMaterial decalScript = (DecalMaterial)target;

        serializedObject.Update();

        decalScript.penetrability = EditorGUILayout.Slider("Penetrability", penetrability.floatValue, 0f, 1f);

        if (penetrability.floatValue > 0f)
        {
            decalScript.doesSplatter = EditorGUILayout.Toggle("Does Splatter", doesSplatter.boolValue);
        }

        EditorGUILayout.PropertyField(entryDecals, true);
        EditorGUILayout.PropertyField(entryParticles, true);

        if (penetrability.floatValue > 0f)
        {
            EditorGUILayout.PropertyField(exitDecals, true);
            EditorGUILayout.PropertyField(exitParticles, true);
        }

        if (doesSplatter.boolValue)
        {
            EditorGUILayout.PropertyField(splatterRange);
            EditorGUILayout.PropertyField(splatterDecals, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}