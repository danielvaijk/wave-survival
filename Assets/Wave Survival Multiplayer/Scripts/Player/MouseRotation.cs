using UnityEngine;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Rotates a GameObject based on given Input from the mouse.

public class MouseRotation : MonoBehaviour
{
    public RotationAxis rotationAxis = RotationAxis.X;

    public float sensitivity = 12f;

    public float minimum = -360f;
    public float maximum = 360f;

    private float rotationY = 0f;
    private float rotationX = 0f;

    private float idleTimer = 0f;

    private Quaternion lastRotation = Quaternion.identity;
    private Quaternion originalRotation = Quaternion.identity;

    private Animator animator = null;

    private NetworkUser localNetworkUser = null;

    // Different types of rotation axis.
    public enum RotationAxis { X, Y };

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        if (GetComponent<Camera>() != null)
        {
            // If this is on the Player's camera GameObject.

            localNetworkUser = GetComponentInParent<NetworkUser>();
            animator = GetComponentInParent<Animator>();
        }
        else
        {
            // If this is on the Player's body GameObject.

            localNetworkUser = GetComponent<NetworkUser>();
            animator = GetComponent<Animator>();
        }

        originalRotation = transform.localRotation;
        lastRotation = originalRotation;
    }

    // Called every frame after Start().
    private void Update ()
    {
        // Only allow the Player to rotate if the Escape Menu is not active.
        if (!localNetworkUser.showEscapeMenu)
        {
            rotationX += Input.GetAxis("Mouse X") * sensitivity;
            rotationX = ClampAngle(rotationX, minimum, maximum);

            rotationY += Input.GetAxis("Mouse Y") * sensitivity;
            rotationY = ClampAngle(rotationY, minimum, maximum);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);

            switch (rotationAxis)
            {
                case RotationAxis.X:
                    transform.localRotation = originalRotation * xQuaternion;
                    break;

                case RotationAxis.Y:
                    transform.localRotation = originalRotation * yQuaternion;
                    break;

                default:
                    Debug.LogWarning("No rotation axis selected.");
                    break;
            }
        }

        if (lastRotation != transform.localRotation)
        {
            // Called if the Player has rotated to a new rotation.

            lastRotation = transform.localRotation;

            localNetworkUser.BreakIdle();
            idleTimer = 0f;
        }
        else
        {
            // Called if the Player has not changed his rotation (starts count down to idle).

            idleTimer += Time.deltaTime;

            if (idleTimer >= localNetworkUser.idleTime)
            {
                // Set the player as idle.
                animator.SetBool("Idle", true);
                idleTimer = 0f;
            }
        }
    }

    // Clamps a angle between the -360f and 360f circumference degrees and also
    // clamps the angle based on the given <min> and <max> values.
    private float ClampAngle (float angle, float min, float max)
    {
        if (angle < -360f || angle > 360f)
        {
            angle = 0f;
        }

        return Mathf.Clamp(angle, min, max);
    }
}