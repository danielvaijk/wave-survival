using UnityEngine;

[DisallowMultipleComponent]

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Will set and constantly rotate the given skybox.

public class SkyboxRotater : MonoBehaviour
{
    public float rotateSpeed = 1f;

    public Material skybox;

    private float angle = 0f;

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        // Set and skybox and make sure its starting angle is 0.
        RenderSettings.skybox = skybox;
        RenderSettings.skybox.SetFloat("_Rotation", 0f);
    }

    // Called every frame after Start().
    private void Update ()
    {
        angle += rotateSpeed * Time.deltaTime;

        if (angle >= 360f)
        {
            angle = 0f;
        }

        // Apply the rotation to the skybox material.
        RenderSettings.skybox.SetFloat("_Rotation", angle);
    }
}