
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public float distance = 8f;
    public float height = 7f;
    public float horizontalAngle = 45f;

    [Header("Camera Rotation")]
    [Range(10f, 80f)]
    public float verticalAngle = 45f;

    [Header("Smooth Follow")]
    public float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Góc đặt camera quanh nhân vật
        Quaternion rotation = Quaternion.Euler(
            0f,
            horizontalAngle,
            0f
        );

        Vector3 offset = rotation * new Vector3(
            0f,
            height,
            -distance
        );

        // Vị trí camera mong muốn
        Vector3 desiredPosition = target.position + offset;

        // Di chuyển mượt
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );

        // Giữ góc nhìn cố định
        transform.rotation = Quaternion.Euler(
            verticalAngle,
            horizontalAngle,
            0f
        );
    }
}
