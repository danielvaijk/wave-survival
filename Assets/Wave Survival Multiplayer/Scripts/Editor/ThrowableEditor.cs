using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Throwable))]
[CanEditMultipleObjects]
public class ThrowableEditor : Editor
{
    private SerializedProperty throwableType = null;

    private SerializedProperty damage = null;
    private SerializedProperty timer = null;
    private SerializedProperty blastRadius = null;

    private SerializedProperty showBlastRadius = null;

    private SerializedProperty explosionParticle = null;
    private SerializedProperty fireParticle = null;

    private SerializedProperty explosionSound = null;
    private SerializedProperty collisionSound = null;

    private void OnEnable()
    {
        throwableType = serializedObject.FindProperty("throwableType");

        damage = serializedObject.FindProperty("damage");
        timer = serializedObject.FindProperty("timer");
        blastRadius = serializedObject.FindProperty("blastRadius");

        showBlastRadius = serializedObject.FindProperty("showBlastRadius");

        explosionParticle = serializedObject.FindProperty("explosionParticle");
        fireParticle = serializedObject.FindProperty("fireParticle");

        explosionSound = serializedObject.FindProperty("explosionSound");
        collisionSound = serializedObject.FindProperty("collisionSound");
    }

    public override void OnInspectorGUI()
    {
        Throwable throwableScript = (Throwable)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(throwableType);

        string currentthrowableType = throwableType.enumNames[throwableType.enumValueIndex];

        throwableScript.damage = EditorGUILayout.FloatField("Damage", damage.floatValue);

        if (currentthrowableType == "Grenade")
        {
            throwableScript.timer = EditorGUILayout.FloatField("Timer", timer.floatValue);
        }

        throwableScript.blastRadius = EditorGUILayout.FloatField("Blast Radius", blastRadius.floatValue);

        throwableScript.showBlastRadius = EditorGUILayout.Toggle("Show Blast Radius", showBlastRadius.boolValue);

        throwableScript.explosionParticle = EditorGUILayout.ObjectField("Explosion Particle", explosionParticle.objectReferenceValue, typeof(GameObject), true) as GameObject;

        if (currentthrowableType != "Grenade")
        {
            throwableScript.fireParticle = EditorGUILayout.ObjectField("Fire Particle", fireParticle.objectReferenceValue, typeof(GameObject), true) as GameObject;
        }

        if (currentthrowableType != "Molotov")
        {
            throwableScript.explosionSound = EditorGUILayout.ObjectField("Explosion Sound", explosionSound.objectReferenceValue, typeof(AudioClip), true) as AudioClip;
        }
        else
        {
            throwableScript.collisionSound = EditorGUILayout.ObjectField("Collision Sound", collisionSound.objectReferenceValue, typeof(AudioClip), true) as AudioClip;
        }

        serializedObject.ApplyModifiedProperties();
    }
}