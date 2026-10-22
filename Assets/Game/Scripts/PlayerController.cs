
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 12f;

    [Header("Turn Animations")]
    public float turn90Duration = 0.5f;
    public float turn180Duration = 0.8f;
    public float turnThreshold = 35f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;
    public float runningJumpHeight = 2.2f;
    public float gravity = -20f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private bool isTurning;
    private bool isDead;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (isDead) return;

        HandleMovement();
    }

    private Vector2 ReadInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current == null)
            return input;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private bool IsRunning()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.leftShiftKey.isPressed ||
               Keyboard.current.rightShiftKey.isPressed;
    }

    private void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;

        Vector2 input = ReadInput();
        bool moving = input.sqrMagnitude > 0.01f;
        bool running = IsRunning();

        Vector3 moveDirection = Vector3.zero;

        if (moving)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;

                forward.y = 0f;
                right.y = 0f;

                forward.Normalize();
                right.Normalize();
            }

            moveDirection =
                (forward * input.y + right * input.x).normalized;
        }

        // Nhảy
        if (!isTurning &&
            grounded &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool runningJump = moving && running;

            float height = runningJump
                ? runningJumpHeight
                : jumpHeight;

            verticalVelocity.y =
                Mathf.Sqrt(height * -2f * gravity);

            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.ResetTrigger("RunningJump");

                animator.SetTrigger(
                    runningJump ? "RunningJump" : "Jump"
                );
            }
        }

        Vector3 horizontalMovement = Vector3.zero;

        // Không cho di chuyển trong lúc phát animation quay
        if (!isTurning && moving)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            float angle = Vector3.SignedAngle(
                transform.forward,
                moveDirection,
                Vector3.up
            );

            // Chỉ phát animation quay khi đang tiếp đất
            if (grounded && Mathf.Abs(angle) >= turnThreshold)
            {
                StartCoroutine(
                    TurnCharacter(targetRotation, angle)
                );
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                float speed = running ? runSpeed : walkSpeed;

                horizontalMovement =
                    moveDirection * speed;
            }
        }

        // Animation di chuyển
        if (animator != null)
        {
            float targetSpeed = 0f;

            if (moving && !isTurning)
                targetSpeed = running ? 2f : 1f;

            animator.SetFloat(
                "Speed",
                targetSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        // Trọng lực
        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 totalMovement =
            horizontalMovement + verticalVelocity;

        controller.Move(totalMovement * Time.deltaTime);
    }

    private IEnumerator TurnCharacter(
        Quaternion targetRotation,
        float angle)
    {
        isTurning = true;

        string trigger;
        float duration;

        if (Mathf.Abs(angle) >= 135f)
        {
            trigger = "Turn180";
            duration = turn180Duration;
        }
        else if (angle < 0f)
        {
            trigger = "TurnLeft";
            duration = turn90Duration;
        }
        else
        {
            trigger = "TurnRight";
            duration = turn90Duration;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);

            animator.ResetTrigger("TurnLeft");
            animator.ResetTrigger("TurnRight");
            animator.ResetTrigger("Turn180");

            animator.SetTrigger(trigger);
        }

        Quaternion startRotation = transform.rotation;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Quay mượt theo tiến trình animation
            float smoothT = t * t * (3f - 2f * t);

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.rotation = targetRotation;

        isTurning = false;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        StopAllCoroutines();
        isTurning = false;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Die");
        }
    }
}
