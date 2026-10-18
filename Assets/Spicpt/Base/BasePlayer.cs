using System.Collections;
using UnityEngine;

/// <summary>
/// ผู้เล่นในด่านฐาน มองจากข้างหลัง
///
/// ต่างจากด่านปกติเกือบทุกอย่าง
///   - ขยับได้แค่ซ้ายขวา ตำแหน่งแนวลึกตรึงตาย (ต้นฉบับ hard-code ไว้ที่ Y = 168)
///   - ยิงได้ทิศเดียว คือพุ่งเข้าจอ จึงไม่มีท่าเล็ง
///   - ไม่มีพื้นให้ชน ขอบซ้ายขวาบังคับด้วยตัวเลขในโค้ด ไม่ใช่คอลไลเดอร์
///   - กดขึ้นตอนห้องยังไม่เคลียร์ = โดนรั้วไฟฟ้าช็อต ไม่ตายแต่ขยับไม่ได้
///
/// ท่าทั้งหมดมีแค่ 6 ท่าตาม indoor_player_sprite_tbl ของต้นฉบับ
/// </summary>
public class BasePlayer : MonoBehaviour
{
    private enum Pose
    {
        Standing,
        Electrocuted,
        Crouching,
        Walking,
        Advancing,
        Jumping,
        Dead
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;

    /// <summary>สูงสุดของการกระโดด หน่วยเดียวกับฉาก</summary>
    [SerializeField] private float jumpHeight = 1.6f;

    [SerializeField] private float gravity = 26f;

    [Header("Electric Fence (รั้วไฟฟ้า)")]
    /// <summary>ต้นฉบับตั้งไว้ 48 เฟรม = 0.8 วินาที ไม่ตายแต่ขยับไม่ได้เลย</summary>
    [SerializeField] private float shockDuration = 0.8f;

    [SerializeField] private float shockBlinkInterval = 0.06f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;

    /// <summary>ปากกระบอกปืน วัดจากจุดหมุนของตัวละคร</summary>
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.18f, 0.72f);

    [SerializeField] private Vector2 crouchMuzzleOffset = new Vector2(0.18f, 0.28f);

    /// <summary>ไม่ได้ต่อ WeaponSystem ไว้ก็ใช้จังหวะนี้แทน</summary>
    [SerializeField] private float fallbackFireRate = 0.2f;

    /// <summary>ปืนกลเท่านั้นที่กดค้างยิงรัวได้ เหมือนด่านอื่นในเกมนี้</summary>
    [SerializeField] private bool machineGunHoldsFire = true;

    [Header("Collider (ย่อตอนหมอบ กระสุนจะได้ผ่านหัวไป)")]
    [SerializeField] private Vector2 standColliderSize = new Vector2(0.5f, 1.2f);
    [SerializeField] private Vector2 standColliderOffset = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 crouchColliderSize = new Vector2(0.6f, 0.5f);
    [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, 0.25f);

    [Header("Death")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float respawnDelay = 1.1f;
    [SerializeField] private float invulnerableDuration = 2f;
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite standSprite;
    [SerializeField] private Sprite crouchSprite;
    [SerializeField] private Sprite shockSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private Sprite[] advanceSprites;
    [SerializeField] private float walkFrameRate = 10f;

    [Header("Input")]
    /// <summary>ต้องตรงกับด่านอื่นในเกม PlayerController ใช้ X กระโดด Z ยิง</summary>
    [SerializeField] private KeyCode jumpKey = KeyCode.X;

    [SerializeField] private KeyCode fireKey = KeyCode.Z;

    private BoxCollider2D box;
    private WeaponSystem weaponSystem;

    private float leftBound = -3.33f;
    private float rightBound = 3.33f;
    private float floorY;

    private float verticalVelocity;
    private float height;
    private float shockTimer;
    private float nextFireTime;
    private float animTime;

    private Pose pose = Pose.Standing;
    private bool crouching;
    private bool advancing;
    private float advanceTimer;
    private float advanceDuration;
    private Vector2 advanceStart;

    private int lives;
    private bool dead;
    private bool invulnerable;

    public bool IsAdvancing
    {
        get { return advancing; }
    }

    public bool IsBusy
    {
        get { return advancing || dead || shockTimer > 0f; }
    }

    /// <summary>
    /// หมอบอยู่หรือเปล่า
    /// กระสุนปืนใช้ค่านี้เช็คว่าควรผ่านหัวไปแทนที่จะโดน
    /// </summary>
    public bool IsCrouching
    {
        get { return crouching && !advancing && !dead; }
    }

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        weaponSystem = GetComponent<WeaponSystem>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        floorY = BaseStageGeometry.FloorY;
        lives = startingLives;

        ApplyColliderShape();
    }

    /// <summary>ตั้งขอบเดินตามห้อง ห้องบอสกว้างกว่าห้องทางเดิน</summary>
    public void ApplyRoomBounds(BaseRoom room)
    {
        bool boss = room != null && room.IsBossRoom;

        leftBound = BaseStageGeometry.ToWorldX(
            boss
                ? BaseStageGeometry.BossRoomLeftPx
                : BaseStageGeometry.CorridorLeftPx
        );

        rightBound = BaseStageGeometry.ToWorldX(
            boss
                ? BaseStageGeometry.BossRoomRightPx
                : BaseStageGeometry.CorridorRightPx
        );
    }

    private void Update()
    {
        if (dead)
            return;

        if (advancing)
        {
            UpdateWalkIntoScreen();
            return;
        }

        if (shockTimer > 0f)
        {
            UpdateShock();
            return;
        }

        HandleMovement();
        HandleJump();
        HandleAdvanceInput();
        HandleShooting();

        UpdatePose();
    }

    private void HandleMovement()
    {
        float input = Input.GetAxisRaw("Horizontal");

        crouching = height <= 0.001f &&
            Input.GetAxisRaw("Vertical") < -0.5f;

        ApplyColliderShape();

        // หมอบอยู่ก็ขยับไม่ได้ เหมือนต้นฉบับ
        if (crouching)
        {
            input = 0f;
        }

        if (Mathf.Abs(input) > 0.01f)
        {
            float x = transform.position.x + input * moveSpeed * Time.deltaTime;

            transform.position = new Vector3(
                Mathf.Clamp(x, leftBound, rightBound),
                transform.position.y,
                transform.position.z
            );
        }

        // หันตัวตามทางที่เดิน แต่ยังหันหลังให้กล้องอยู่
        if (Mathf.Abs(input) > 0.01f && spriteRenderer != null)
        {
            spriteRenderer.flipX = input < 0f;
        }
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(jumpKey) && height <= 0.001f && !crouching)
        {
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        if (height > 0f || verticalVelocity > 0f)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            height += verticalVelocity * Time.deltaTime;

            if (height <= 0f)
            {
                height = 0f;
                verticalVelocity = 0f;
            }
        }

        transform.position = new Vector3(
            transform.position.x,
            floorY + height,
            transform.position.z
        );
    }

    /// <summary>
    /// กดขึ้น = พยายามเดินเข้าห้องถัดไป
    /// ห้องยังไม่เคลียร์จะโดนรั้วช็อตแทน ตรงตาม d_pad_up_pressed ของต้นฉบับ
    /// </summary>
    private void HandleAdvanceInput()
    {
        if (Input.GetAxisRaw("Vertical") < 0.5f)
            return;

        if (height > 0.001f)
            return;

        BaseRoom room = BaseStage.Instance != null
            ? BaseStage.Instance.CurrentRoom
            : null;

        if (room != null && room.IsCleared)
        {
            BaseStage.Instance.RequestAdvance();
            return;
        }

        Electrocute();
    }

    public void Electrocute()
    {
        if (shockTimer > 0f)
            return;

        shockTimer = shockDuration;

        SetPose(Pose.Electrocuted);
    }

    private void UpdateShock()
    {
        shockTimer -= Time.deltaTime;

        // แกนกลางพังระหว่างที่ยังช็อตอยู่ อาการหยุดทันที ไม่ต้องรอครบเวลา
        BaseRoom room = BaseStage.Instance != null
            ? BaseStage.Instance.CurrentRoom
            : null;

        if (room != null && room.IsCleared)
        {
            shockTimer = 0f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled =
                Mathf.Repeat(Time.time, shockBlinkInterval * 2f) <
                shockBlinkInterval;
        }

        if (shockTimer <= 0f && spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void HandleShooting()
    {
        if (bulletPrefab == null)
            return;

        bool held = Input.GetKey(fireKey);
        bool pressed = Input.GetKeyDown(fireKey);

        bool autoFire = machineGunHoldsFire &&
            weaponSystem != null &&
            weaponSystem.CurrentWeapon == WeaponType.MachineGun;

        if (!(autoFire ? held : pressed))
            return;

        if (Time.time < nextFireTime)
            return;

        float rate = weaponSystem != null
            ? weaponSystem.CurrentFireRate
            : fallbackFireRate;

        nextFireTime = Time.time + rate;

        GameAudio.PlayerShot(
            weaponSystem != null ? weaponSystem.CurrentWeapon : WeaponType.Normal
        );

        Vector2 offset = crouching ? crouchMuzzleOffset : muzzleOffset;

        Instantiate(
            bulletPrefab,
            (Vector2)transform.position + offset,
            Quaternion.identity
        );
    }

    private void ApplyColliderShape()
    {
        if (box == null)
            return;

        box.size = crouching ? crouchColliderSize : standColliderSize;
        box.offset = crouching ? crouchColliderOffset : standColliderOffset;
    }

    // ---------- เดินเข้าห้องถัดไป ----------

    public void BeginWalkIntoScreen(float duration)
    {
        advancing = true;
        advanceTimer = 0f;
        advanceDuration = Mathf.Max(0.01f, duration);
        advanceStart = transform.position;

        crouching = false;
        height = 0f;
        verticalVelocity = 0f;

        ApplyColliderShape();
        SetPose(Pose.Advancing);
    }

    private void UpdateWalkIntoScreen()
    {
        advanceTimer += Time.deltaTime;

        float t = Mathf.Clamp01(advanceTimer / advanceDuration);

        // เดินลึกเข้าไปพร้อมย่อขนาด ให้รู้สึกว่าไกลออกไปเรื่อยๆ
        float y = Mathf.Lerp(advanceStart.y, BaseStageGeometry.WallY, t);

        transform.position = new Vector3(
            Mathf.Lerp(advanceStart.x, 0f, t),
            y,
            transform.position.z
        );

        transform.localScale = Vector3.one * Mathf.Lerp(
            BaseStageGeometry.NearScale,
            BaseStageGeometry.FarScale,
            t
        );

        AnimateFrames(advanceSprites);
    }

    public void EndWalkIntoScreen(bool hop)
    {
        advancing = false;

        transform.localScale = Vector3.one;

        transform.position = new Vector3(0f, floorY, transform.position.z);

        height = 0f;

        // ต้นฉบับสั่งกระโดดหนึ่งทีทันทีที่ถึงห้องใหม่ (INDOOR_PLAYER_JUMP_FLAG)
        verticalVelocity = hop ? Mathf.Sqrt(2f * gravity * jumpHeight) : 0f;

        SetPose(Pose.Standing);
    }

    // ---------- ตายและเกิดใหม่ ----------

    public void Kill()
    {
        if (dead || invulnerable || advancing)
            return;

        dead = true;
        lives--;

        SetPose(Pose.Dead);

        if (deathEffectPrefab != null)
        {
            Instantiate(
                deathEffectPrefab,
                transform.position + Vector3.up * 0.6f,
                Quaternion.identity
            );
        }

        if (lives <= 0)
        {
            Debug.Log("Game Over");
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        transform.position = new Vector3(0f, floorY, transform.position.z);

        height = 0f;
        verticalVelocity = 0f;
        crouching = false;
        shockTimer = 0f;
        dead = false;

        ApplyColliderShape();
        SetPose(Pose.Standing);

        yield return StartCoroutine(InvulnerableRoutine());
    }

    private IEnumerator InvulnerableRoutine()
    {
        invulnerable = true;

        float elapsed = 0f;

        while (elapsed < invulnerableDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(0.08f);

            elapsed += 0.08f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        invulnerable = false;
    }

    // ---------- ท่าทาง ----------

    private void UpdatePose()
    {
        // ลอยอยู่ต้องมาก่อนทุกท่า ไม่งั้นกระโดดแล้วภาพยังเป็นท่าเดิน
        if (height > 0.001f)
        {
            SetPose(Pose.Jumping);
            return;
        }

        if (crouching)
        {
            SetPose(Pose.Crouching);
            return;
        }

        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f)
        {
            SetPose(Pose.Walking);

            AnimateFrames(walkSprites);

            return;
        }

        SetPose(Pose.Standing);
    }

    private void SetPose(Pose next)
    {
        if (pose != next)
        {
            pose = next;
            animTime = 0f;
        }

        if (spriteRenderer == null)
            return;

        switch (pose)
        {
            case Pose.Crouching:
                if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                return;

            case Pose.Electrocuted:
                if (shockSprite != null) spriteRenderer.sprite = shockSprite;
                return;

            case Pose.Jumping:
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                return;

            case Pose.Dead:
                if (deadSprite != null) spriteRenderer.sprite = deadSprite;
                return;

            case Pose.Standing:
                if (standSprite != null) spriteRenderer.sprite = standSprite;
                return;
        }
    }

    private void AnimateFrames(Sprite[] frames)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        animTime += Time.deltaTime;

        int index = Mathf.FloorToInt(animTime * walkFrameRate) % frames.Length;

        spriteRenderer.sprite = frames[index];
    }

    private void OnDrawGizmosSelected()
    {
        // ขีดเส้นขอบเดินให้เห็นว่าวิ่งได้ถึงไหน
        float y = Application.isPlaying ? floorY : BaseStageGeometry.FloorY;

        Gizmos.DrawLine(new Vector3(leftBound, y - 1f), new Vector3(leftBound, y + 2f));
        Gizmos.DrawLine(new Vector3(rightBound, y - 1f), new Vector3(rightBound, y + 2f));
    }
}
