using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Water")]
    /// <summary>ลุยน้ำแล้วช้าลงเท่าไหร่ ตั้งจาก WaterZone ตอนลงน้ำ</summary>
    private float waterSpeedMultiplier = 1f;
    private float waterJumpMultiplier = 1f;

    /// <summary>นับโซนน้ำที่ตัวซ้อนอยู่ กันค่าเพี้ยนตอนโซนน้ำวางทับกัน</summary>
    private int waterZoneCount;

    [Header("Crouch")]
    [SerializeField] private float standingColliderHeight = 1.7f;
    [SerializeField] private float crouchingColliderHeight = 0.9f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("One-Way Platform (ลงจากพื้นต่างระดับ)")]
    [SerializeField] private LayerMask oneWayPlatformLayer;
    [SerializeField] private float dropThroughDuration = 0.3f;
    [SerializeField] private string solidGroundTag = "SolidGround";

    [Header("Screen Lock (Invisible Wall)")]
    [SerializeField] private CameraFollow cameraFollow;

    /// <summary>
    /// ปิดทางขวาตลอดเวลา ไม่ใช่เฉพาะตอนล็อกฉากบอส
    /// ด่านปีนขึ้นแนวตั้งต้องเปิด ไม่งั้นเดินหลุดออกข้างจอ
    /// </summary>
    [SerializeField] private bool clampRightAlways;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;

    private float moveInput;
    private bool isGrounded;
    private bool isCrouching;
    private bool facingRight = true;

    public float MoveInput => moveInput;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsInWater => waterZoneCount > 0;

    /// <summary>WaterZone เรียกตอนตัวแตะน้ำ</summary>
    public void EnterWater(float speedMultiplier, float jumpMultiplier)
    {
        waterZoneCount++;

        waterSpeedMultiplier = speedMultiplier;
        waterJumpMultiplier = jumpMultiplier;
    }

    /// <summary>WaterZone เรียกตอนขึ้นจากน้ำ</summary>
    public void ExitWater()
    {
        waterZoneCount = Mathf.Max(0, waterZoneCount - 1);

        if (waterZoneCount > 0)
            return;

        waterSpeedMultiplier = 1f;
        waterJumpMultiplier = 1f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (rb == null)
        {
            Debug.LogError(
                "PlayerController: Player ต้องมี Rigidbody2D"
            );
        }

        if (capsuleCollider == null)
        {
            Debug.LogError(
                "PlayerController: Player ต้องมี CapsuleCollider2D"
            );
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Player ที่มาจาก prefab จะอ้างถึง CameraFollow ในฉากไม่ได้
        // ต้องหาเองตอนรัน ไม่งั้นกำแพงล่องหนไม่ทำงานทั้งด่าน
        if (cameraFollow == null)
        {
            cameraFollow = Object.FindObjectOfType<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            Debug.LogWarning(
                "PlayerController: หา CameraFollow ในฉากไม่เจอ กำแพงล่องหนจะไม่ทำงาน"
            );
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(
                capsuleCollider.size.x,
                standingColliderHeight
            );

            capsuleCollider.offset = new Vector2(
                capsuleCollider.offset.x,
                standingColliderHeight / 2f
            );
        }
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        HandleCrouch();
        HandleJumpOrDropThrough();
        HandleFlip();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        HandleMovement();

        // ต้องหลัง HandleMovement และอยู่ใน FixedUpdate
        // ถ้าไปเซ็ต rb.position ใน Update จะโดนฟิสิกส์เขียนทับในสเต็ปถัดไป
        ClampToCameraBoundary();
    }

    private void ReadInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    private void HandleMovement()
    {
        if (rb == null)
            return;

        // คลานอยู่กับที่ เดินไม่ได้ (ตามต้นฉบับ Contra 1987)
        if (isCrouching)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        rb.velocity = new Vector2(
            moveInput * moveSpeed * waterSpeedMultiplier,
            rb.velocity.y
        );
    }

    private void HandleJumpOrDropThrough()
    {
        if (rb == null)
            return;

        if (!Input.GetKeyDown(KeyCode.X))
            return;

        if (!isGrounded)
            return;

        // กดลูกศรลง + X = พยายามลงจากพื้นต่างระดับ (One-Way Platform) แทนการกระโดด
        bool downHeld = Input.GetKey(KeyCode.DownArrow);

        if (downHeld)
        {
            TryDropThroughPlatform();
            return;
        }

        // ห้ามกระโดดขณะ Crouch
        if (isCrouching)
            return;

        rb.velocity = new Vector2(
            rb.velocity.x,
            jumpForce * waterJumpMultiplier
        );
    }

    private void TryDropThroughPlatform()
    {
        if (groundCheck == null)
            return;

        Collider2D[] platforms = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            oneWayPlatformLayer
        );

        if (platforms.Length == 0)
            return;

        // กันพลาดสองชั้น: พื้นที่ติด Tag นี้ (เช่นพื้นเกมหลัก) ห้ามลอดทะลุเด็ดขาด แม้จะอยู่ผิด Layer
        List<Collider2D> droppablePlatforms = new List<Collider2D>();

        foreach (Collider2D platform in platforms)
        {
            if (platform.CompareTag(solidGroundTag))
                continue;

            droppablePlatforms.Add(platform);
        }

        if (droppablePlatforms.Count == 0)
            return;

        StartCoroutine(DropThroughRoutine(droppablePlatforms));
    }

    private IEnumerator DropThroughRoutine(List<Collider2D> platforms)
    {
        foreach (Collider2D platform in platforms)
        {
            Physics2D.IgnoreCollision(capsuleCollider, platform, true);
        }

        yield return new WaitForSeconds(dropThroughDuration);

        foreach (Collider2D platform in platforms)
        {
            if (platform != null)
            {
                Physics2D.IgnoreCollision(capsuleCollider, platform, false);
            }
        }
    }

    private void HandleCrouch()
    {
        if (!isGrounded)
        {
            SetCrouch(false);
            return;
        }

        // กดลูกศรลงค้าง "เดี่ยวๆ" (ไม่กดซ้าย/ขวาร่วมด้วย) = คลาน / ย่อตัว
        // ถ้ากดลูกศรลงพร้อมซ้าย/ขวา ให้เดินต่อและยิงเฉียงลงแทน ไม่ใช่คลาน
        bool downHeld = Input.GetKey(KeyCode.DownArrow);
        bool hasHorizontalInput = Mathf.Abs(moveInput) > 0.01f;

        bool crouchInput = downHeld && !hasHorizontalInput;

        SetCrouch(crouchInput);
    }

    private void SetCrouch(bool crouch)
    {
        if (isCrouching == crouch)
            return;

        isCrouching = crouch;

        if (capsuleCollider == null)
            return;

        float height = isCrouching
            ? crouchingColliderHeight
            : standingColliderHeight;

        capsuleCollider.size = new Vector2(
            capsuleCollider.size.x,
            height
        );

        // จุดหมุน (pivot) ของสไปรท์อยู่ที่เท้า ดังนั้นขอบล่างของ collider ต้องอยู่ที่เท้าเสมอ
        // ไม่ว่าจะยืนหรือคลาน (สูง/เตี้ยต่างกัน แต่เท้าต้องแตะพื้นจุดเดียวกัน)
        capsuleCollider.offset = new Vector2(
            capsuleCollider.offset.x,
            height / 2f
        );
    }

    private void ClampToCameraBoundary()
    {
        if (cameraFollow == null || rb == null)
            return;

        // เผื่อครึ่งตัวไว้ ไม่งั้นลำตัวซีกซ้ายจะโผล่พ้นขอบจอ
        // ใช้ bounds เพราะย่อขยายตัวละครแล้วค่าปรับตามเอง
        float halfWidth = capsuleCollider != null
            ? capsuleCollider.bounds.extents.x
            : 0f;

        float leftBoundary = cameraFollow.LeftBoundaryX + halfWidth;

        if (rb.position.x < leftBoundary)
        {
            rb.position = new Vector2(leftBoundary, rb.position.y);

            if (rb.velocity.x < 0f)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }

            return;
        }

        // ด่านแนวนอนปิดทางขวาเฉพาะตอนล็อกฉากบอส (จอหยุดเลื่อนแล้ว)
        // ด่านแนวตั้งต้องปิดตลอด เพราะจอไม่เลื่อนตามแกน X อยู่แล้ว
        if (!clampRightAlways && !cameraFollow.IsBossLocked)
            return;

        float rightBoundary = cameraFollow.RightBoundaryX - halfWidth;

        if (rb.position.x > rightBoundary)
        {
            rb.position = new Vector2(rightBoundary, rb.position.y);

            if (rb.velocity.x > 0f)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
    }

    private void HandleFlip()
    {
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}