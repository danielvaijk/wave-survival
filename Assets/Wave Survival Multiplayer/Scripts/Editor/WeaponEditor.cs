using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Weapon))]
[CanEditMultipleObjects]
public class WeaponEditor : Editor
{
    private SerializedProperty fireType = null;

    private SerializedProperty loadedAmmo = null;
    private SerializedProperty storedAmmo = null;
    private SerializedProperty maxAmmoStorage = null;
    private SerializedProperty maxAmmoPerLoad = null;

    private SerializedProperty fireDistance = null;
    private SerializedProperty fireRate = null;
    private SerializedProperty reloadSpeed = null;
    private SerializedProperty aimSpeed = null;
    private SerializedProperty shotDamage = null;

    private SerializedProperty reloadAnimationTrigger = null;

    private SerializedProperty allowDebugging = null;

    private SerializedProperty shootPoint;
    private SerializedProperty aimPoint;

    private SerializedProperty muzzleFlash;
    private SerializedProperty throwObject;

    private SerializedProperty shotSound;
    private SerializedProperty reloadSound;
    private SerializedProperty emptySound;

    private void OnEnable()
    {
        fireType = serializedObject.FindProperty("fireType");

        loadedAmmo = serializedObject.FindProperty("loadedAmmo");
        storedAmmo = serializedObject.FindProperty("storedAmmo");
        maxAmmoStorage = serializedObject.FindProperty("maxAmmoStorage");
        maxAmmoPerLoad = serializedObject.FindProperty("maxAmmoPerLoad");

        fireDistance = serializedObject.FindProperty("fireDistance");
        fireRate = serializedObject.FindProperty("fireRate");
        reloadSpeed = serializedObject.FindProperty("reloadSpeed");
        aimSpeed = serializedObject.FindProperty("aimSpeed");
        shotDamage = serializedObject.FindProperty("shotDamage");

        reloadAnimationTrigger = serializedObject.FindProperty("reloadAnimationTrigger");

        allowDebugging = serializedObject.FindProperty("allowDebugging");

        shootPoint = serializedObject.FindProperty("shootPoint");
        aimPoint = serializedObject.FindProperty("aimPoint");

        muzzleFlash = serializedObject.FindProperty("muzzleFlash");
        throwObject = serializedObject.FindProperty("throwObject");

        shotSound = serializedObject.FindProperty("shotSound");
        reloadSound = serializedObject.FindProperty("reloadSound");
        emptySound = serializedObject.FindProperty("emptySound");
    }

    public override void OnInspectorGUI ()
    {
        Weapon weaponScript = (Weapon)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(fireType);

        string currentFireType = fireType.enumNames[fireType.enumValueIndex];

        /* Loaded Ammo Field */

        if (currentFireType == "Throwable")
        {
            weaponScript.loadedAmmo = EditorGUILayout.IntField("Ammo Amount", loadedAmmo.intValue);
            weaponScript.maxAmmoPerLoad = EditorGUILayout.IntField("Max Ammo Amount", maxAmmoPerLoad.intValue);
        }
        else
        {
            EditorGUILayout.LabelField("Loaded Ammo:");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Loaded", GUILayout.Width(45));
            weaponScript.loadedAmmo = EditorGUILayout.IntField(loadedAmmo.intValue);

            if (loadedAmmo.intValue > maxAmmoPerLoad.intValue)
            {
                loadedAmmo.intValue = maxAmmoPerLoad.intValue;
            }

            EditorGUILayout.LabelField("Max", GUILayout.Width(30));

            weaponScript.maxAmmoPerLoad = EditorGUILayout.IntField(maxAmmoPerLoad.intValue);

            if (currentFireType == "BoltAction")
            {
                maxAmmoPerLoad.intValue = 1;
            }

            EditorGUILayout.EndHorizontal();
        }

        /* Stored Ammo Field */

        if (currentFireType != "Throwable")
        {
            EditorGUILayout.LabelField("Stored Ammo:");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Stored", GUILayout.Width(45));
            weaponScript.storedAmmo = EditorGUILayout.IntField(storedAmmo.intValue);

            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            weaponScript.maxAmmoStorage = EditorGUILayout.IntField(maxAmmoStorage.intValue);

            if (storedAmmo.intValue > maxAmmoStorage.intValue)
            {
                storedAmmo.intValue = maxAmmoStorage.intValue;

                Debug.LogWarning("<storedAmmo> value cannot be bigger than <maxAmmoStorage> value.");
            }

            EditorGUILayout.EndHorizontal();
        }

        weaponScript.fireDistance = EditorGUILayout.FloatField("Fire Distance", fireDistance.floatValue);

        if (currentFireType != "BoltAction")
        {
            weaponScript.fireRate = EditorGUILayout.FloatField("Fire Rate", fireRate.floatValue);
        }

        if (currentFireType != "Throwable")
        {
            weaponScript.reloadSpeed = EditorGUILayout.FloatField("Reload Speed", reloadSpeed.floatValue);
            weaponScript.aimSpeed = EditorGUILayout.FloatField("Aiming Speed", aimSpeed.floatValue);
            weaponScript.shotDamage = EditorGUILayout.FloatField("Damage", shotDamage.floatValue);
            weaponScript.reloadAnimationTrigger = EditorGUILayout.TextField("Reload Animation Trigger", reloadAnimationTrigger.stringValue);
        }

        weaponScript.allowDebugging = EditorGUILayout.Toggle("Allow Debugging", allowDebugging.boolValue);
        weaponScript.shootPoint = EditorGUILayout.ObjectField("Shoot Point", shootPoint.objectReferenceValue, typeof(Transform), true) as Transform;

        if (currentFireType != "Throwable")
        {
            weaponScript.aimPoint = EditorGUILayout.Vector3Field("Aim Point", aimPoint.vector3Value);
            
            weaponScript.muzzleFlash = EditorGUILayout.ObjectField("Muzzle Flash", muzzleFlash.objectReferenceValue, typeof(GameObject), true) as GameObject;

            weaponScript.shotSound = EditorGUILayout.ObjectField("Shot Sound", shotSound.objectReferenceValue, typeof(AudioClip), false) as AudioClip;
            weaponScript.reloadSound = EditorGUILayout.ObjectField("Reload Sound", reloadSound.objectReferenceValue, typeof(AudioClip), false) as AudioClip;
            weaponScript.emptySound = EditorGUILayout.ObjectField("Empty Sound", emptySound.objectReferenceValue, typeof(AudioClip), false) as AudioClip;
        }

        if (currentFireType == "Throwable")
        {
            weaponScript.shotSound = EditorGUILayout.ObjectField("Throw Sound", weaponScript.shotSound, typeof(AudioClip), false) as AudioClip;
            weaponScript.throwObject = EditorGUILayout.ObjectField("Throw Object", throwObject.objectReferenceValue, typeof(GameObject), true) as GameObject;
        }

        serializedObject.ApplyModifiedProperties();
    }
}