using UnityEngine;

public class EnemyRunner : MonoBehaviour
{
    public enum RunMode
    {
        /// <summary>เล็งหาผู้เล่นตอนเกิดครั้งเดียว แล้ววิ่งทางนั้นยาวเลย (แบบ Contra)</summary>
        LockOnSpawn,

        /// <summary>วิ่งไปทาง Fixed Direction ตลอด ไม่สนผู้เล่นอยู่ไหน</summary>
        FixedDirection,

        /// <summary>ไล่ตามผู้เล่นตลอดเวลา กลับตัวได้เรื่อยๆ</summary>
        ChasePlayer
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Behaviour")]
    [SerializeField] private RunMode runMode = RunMode.LockOnSpawn;

    /// <summary>ใช้ตอนโหมด Fixed Direction และใช้เป็นทางสำรองถ้าหาผู้เล่นไม่เจอ</summary>
    [SerializeField] private float fixedDirection = -1f;

    public enum EdgeBehaviour
    {
        /// <summary>กระโดดข้ามไปฝั่งตรงข้าม</summary>
        JumpAcross,

        /// <summary>กลับตัวเดินย้อนกลับ กลายเป็นเดินวนไปมาบนแท่นเดิม</summary>
        TurnAround,

        /// <summary>เดินตกลงไปเลย ไม่สนใจขอบ</summary>
        WalkOff
    }

    [Header("Edge Behaviour (พฤติกรรมตอนถึงขอบพื้น)")]
    [SerializeField] private EdgeBehaviour edgeBehaviour = EdgeBehaviour.TurnAround;

    /// <summary>หน่วงหลังกลับตัว กันสั่นกลับไปกลับมาตอนยืนคร่อมขอบพอดี</summary>
    [SerializeField] private float turnCooldown = 0.25f;

    [Header("Jump Over Gaps")]
    [SerializeField] private bool jumpOverGaps = true;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private LayerMask groundLayer = 1 << 3;

    /// <summary>
    /// จุดเช็คพื้น วัดจาก transform (ซึ่งอยู่ที่เท้าอยู่แล้ว)
    /// พื้นในด่านเป็นแผ่นบาง 0.1 ที่อยู่ "ใต้" ผิวพื้นพอดี
    /// จุดนี้ต้องอยู่ต่ำพอให้วงกลมคร่อมแผ่นได้ ไม่งั้นหาพื้นไม่เจอเลย
    /// </summary>
    [SerializeField] private Vector2 footOffset = new Vector2(0f, 0.08f);
    [SerializeField] private float footRadius = 0.22f;

    /// <summary>หยั่งไปข้างหน้าเท่าไหร่ถึงนับว่าถึงขอบ</summary>
    [SerializeField] private float edgeCheckAhead = 0.75f;
    [SerializeField] private float edgeCheckDepth = 0.5f;

    /// <summary>ไม่เห็นที่ลงก็ไม่กระโดด กันวิ่งโดดลงเหว</summary>
    [SerializeField] private bool requireLandingSpot = true;
    [SerializeField] private float maxJumpDistance = 3f;
    [SerializeField] private float landingCheckDepth = 3f;

    [SerializeField] private float jumpCooldown = 0.35f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D rb;
    private Transform player;
    private bool facingRight = true;

    /// <summary>0 = ยังไม่ล็อก ให้คิดใหม่ทุกเฟรม</summary>
    private float lockedDirection;

    private float nextJumpTime;

    /// <summary>ทิศที่กลับตัวไว้ ทับทุกโหมดเพราะเดินวนอยู่บนแท่นแล้ว</summary>
    private float turnDirection;
    private float nextTurnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "EnemyRunner: ต้องมี Rigidbody2D"
            );
        }
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        LockDirection();
    }

    /// <summary>ตัดสินใจทิศทางครั้งเดียวตอนเกิด แล้วไม่เปลี่ยนใจอีก</summary>
    private void LockDirection()
    {
        if (runMode == RunMode.ChasePlayer)
        {
            lockedDirection = 0f;
            return;
        }

        if (runMode == RunMode.FixedDirection || player == null)
        {
            lockedDirection = fixedDirection >= 0f ? 1f : -1f;
            return;
        }

        float difference =
            player.position.x - transform.position.x;

        // เกิดทับผู้เล่นพอดี ก็ใช้ทางสำรองไป
        lockedDirection = Mathf.Abs(difference) < 0.05f
            ? (fixedDirection >= 0f ? 1f : -1f)
            : Mathf.Sign(difference);
    }

    /// <summary>
    /// ให้จุดเกิดสั่งพฤติกรรมเฉพาะตัวได้ โดยไม่ต้องแยก prefab
    /// เรียกหลัง Instantiate ทันที จะทันก่อน Start() ที่ล็อกทิศทาง
    /// </summary>
    public void SetRunMode(RunMode mode)
    {
        runMode = mode;
    }

    public void SetEdgeBehaviour(EdgeBehaviour behaviour)
    {
        edgeBehaviour = behaviour;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        float direction = GetMoveDirection();

        rb.velocity = new Vector2(
            direction * moveSpeed,
            rb.velocity.y
        );

        UpdateFacing(direction);

        HandleEdge(direction);
    }

    private void HandleEdge(float direction)
    {
        if (edgeBehaviour == EdgeBehaviour.WalkOff)
            return;

        if (edgeBehaviour == EdgeBehaviour.JumpAcross)
        {
            TryJumpOverGap(direction);
            return;
        }

        TryTurnAround(direction);
    }

    /// <summary>ถึงขอบแล้วกลับตัว เดินวนไปมาอยู่บนแท่นเดิม</summary>
    private void TryTurnAround(float direction)
    {
        if (Time.time < nextTurnTime)
            return;

        if (!IsGrounded())
            return;

        if (!IsAtLedge(direction))
            return;

        turnDirection = -direction;

        nextTurnTime = Time.time + turnCooldown;
    }

    /// <summary>ถึงขอบพื้นเมื่อไหร่ก็กระโดดข้ามไปฝั่งตรงข้าม</summary>
    private void TryJumpOverGap(float direction)
    {
        if (!jumpOverGaps)
            return;

        if (Time.time < nextJumpTime)
            return;

        if (!IsGrounded())
            return;

        if (!IsAtLedge(direction))
            return;

        if (requireLandingSpot && !HasLandingSpot(direction))
            return;

        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        nextJumpTime = Time.time + jumpCooldown;
    }

    private float GetScale()
    {
        return Mathf.Abs(transform.lossyScale.y);
    }

    private Vector2 GetFootPosition()
    {
        return (Vector2)transform.position + footOffset * GetScale();
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            GetFootPosition(),
            footRadius * GetScale(),
            groundLayer
        ) != null;
    }

    /// <summary>หยั่งลงตรงข้างหน้า ไม่เจอพื้น = ยืนอยู่ปากเหว</summary>
    private bool IsAtLedge(float direction)
    {
        float scale = GetScale();

        Vector2 probe = GetFootPosition() +
            new Vector2(direction * edgeCheckAhead * scale, 0f);

        return Physics2D.Raycast(
            probe,
            Vector2.down,
            edgeCheckDepth * scale,
            groundLayer
        ).collider == null;
    }

    /// <summary>ไล่หยั่งไปเรื่อยๆ จนสุดระยะกระโดด เจอพื้นที่ไหนก็ลงได้</summary>
    private bool HasLandingSpot(float direction)
    {
        float scale = GetScale();
        float step = 0.4f * scale;

        for (float distance = edgeCheckAhead * scale;
             distance <= maxJumpDistance * scale;
             distance += step)
        {
            Vector2 probe = GetFootPosition() +
                new Vector2(direction * distance, 0f);

            bool found = Physics2D.Raycast(
                probe,
                Vector2.down,
                landingCheckDepth * scale,
                groundLayer
            ).collider != null;

            if (found)
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        float scale = Mathf.Abs(transform.lossyScale.y);
        float sign = facingRight ? 1f : -1f;

        Vector2 foot = (Vector2)transform.position + footOffset * scale;

        Gizmos.DrawWireSphere(foot, footRadius * scale);

        Vector2 edge = foot + new Vector2(sign * edgeCheckAhead * scale, 0f);

        Gizmos.DrawLine(edge, edge + Vector2.down * (edgeCheckDepth * scale));

        Vector2 far = foot + new Vector2(sign * maxJumpDistance * scale, 0f);

        Gizmos.DrawLine(far, far + Vector2.down * (landingCheckDepth * scale));
    }

    private float GetMoveDirection()
    {
        // กลับตัวที่ขอบแล้วก็เดินวนตามนั้น ทับทุกโหมด
        if (turnDirection != 0f)
        {
            return turnDirection;
        }

        // ล็อกทางไว้แล้วก็วิ่งตรงไปเลย ไม่กลับตัวตามผู้เล่น
        if (lockedDirection != 0f)
        {
            return lockedDirection;
        }

        if (player == null)
        {
            return fixedDirection >= 0f ? 1f : -1f;
        }

        float difference =
            player.position.x - transform.position.x;

        // ยืนซ้อนกันพอดี ให้วิ่งทางเดิมต่อ กันอาการสั่นกลับไปกลับมา
        if (Mathf.Abs(difference) < 0.05f)
        {
            return facingRight ? 1f : -1f;
        }

        return Mathf.Sign(difference);
    }

    private void UpdateFacing(float direction)
    {
        if (direction > 0f && !facingRight)
        {
            Flip();
        }
        else if (direction < 0f && facingRight)
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
}
