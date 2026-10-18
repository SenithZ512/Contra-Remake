using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int startingLives = 3;

    [Header("Invulnerable (ช่วงอมตะหลังเกิดใหม่)")]
    [SerializeField] private float invulnerableDuration = 2f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Barrier (ไอเทม B)")]
    /// <summary>
    /// ต้นฉบับตั้งตัวนับไว้ที่ $80 แล้วลดทีละหนึ่งทุก 8 เฟรม
    /// 128 x 8 = 1024 เฟรม หารด้วย 60 ได้ 17.07 วินาที
    /// </summary>
    [SerializeField] private float barrierDuration = 17.07f;

    /// <summary>
    /// ด่าน Hangar ด่านเดียวที่ได้นานกว่า ต้นฉบับตั้ง $90 = 19.2 วินาที
    /// ใส่ชื่อฉากที่อยากให้ใช้ค่านี้ เว้นว่างไว้ก็ใช้ค่าปกติทุกด่าน
    /// </summary>
    [SerializeField] private string longBarrierScene = "Hangar";

    [SerializeField] private float longBarrierDuration = 19.2f;

    /// <summary>สีที่วนตอนมีเกราะ ต้นฉบับสลับพาเลตต์ให้ตัวละครเปลี่ยนสีรัว</summary>
    [SerializeField] private Color[] barrierColors =
    {
        new Color(1f, 1f, 1f),
        new Color(0.6f, 0.9f, 1f),
        new Color(1f, 0.8f, 0.4f),
        new Color(0.8f, 0.6f, 1f)
    };

    [SerializeField] private float barrierColorRate = 12f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private float respawnHeightOffset = 5f;
    [SerializeField] private float respawnEdgeMargin = 1.5f;

    /// <summary>
    /// เกิดใหม่โดยอิงกล้อง ไม่ใช่อิงจุดเริ่มด่าน
    ///
    /// **จำเป็นสำหรับด่านที่กล้องเลื่อน** อย่าง WaterFall ที่ไต่ขึ้นไปเรื่อยๆ
    /// ถ้าปิดอันนี้ จุดเกิดจะค้างอยู่ที่ก้นด่านตลอด
    /// ปีนขึ้นไปสูงแล้วตายเมื่อไหร่ก็เกิดใต้กล้อง แล้วตายซ้ำไม่จบ
    ///
    /// ปิดได้ถ้าด่านนั้นกล้องล็อกอยู่กับที่ จะได้เกิดจุดเดิมเป๊ะทุกครั้ง
    /// </summary>
    [SerializeField] private bool respawnAtCamera = true;

    /// <summary>
    /// ยอมให้เกิดเหนือขอบจอแล้วร่วงลงมาไหม **ปิดไว้เป็นค่าเริ่มต้น**
    ///
    /// ต้นฉบับให้ตัวละครโผล่ "ในจอ" เสมอ เห็นตัวเองตั้งแต่วินาทีแรก
    /// ถ้าเกิดเหนือขอบจอ ช่วงที่กำลังร่วงจะมองไม่เห็นตัวเลย
    /// บังคับก็ไม่ได้เพราะไม่รู้ว่าตัวอยู่ตรงไหน
    ///
    /// ตอนสู้บอสยิ่งหนัก กล้องล็อกอยู่กับที่ ร่วงลงมาก็เจอกระสุนบอสพอดี
    /// ทั้งที่ยังไม่ทันเห็นจอด้วยซ้ำ
    ///
    /// เปิดได้ถ้าอยากได้ภาพร่วงลงมาจากฟ้าในด่านโล่งๆ ที่ไม่มีบอส
    /// </summary>
    [SerializeField] private bool respawnAboveScreen = false;

    /// <summary>โผล่เหนือขอบบนกล้องเท่าไหร่ ใช้เฉพาะตอนเปิด Respawn Above Screen</summary>
    [SerializeField] private float respawnDropHeight = 1.5f;

    /// <summary>
    /// ลึกจากขอบบนลงมาเท่าไหร่ ใช้กับด่านที่กล้องไต่ตามผู้เล่น
    ///
    /// ด่านแบบนั้นห้ามเกิดเหนือขอบจอเด็ดขาด เพราะกล้องจะไต่ตามขึ้นไป
    /// แล้วรอบหน้าก็เกิดสูงกว่าเดิมอีก ไต่หนีขึ้นไปเรื่อยๆ ไม่มีวันจบ
    /// </summary>
    [SerializeField] private float respawnInsideMargin = 1f;

    /// <summary>
    /// เกิดใกล้จุดที่ตาย แทนที่จะเกิดริมซ้ายของจอเสมอ
    /// ปิดแล้วจะกลับไปเกิดชิดขอบซ้ายแบบเดิม
    /// </summary>
    [SerializeField] private bool respawnNearDeathSpot = true;

    /// <summary>
    /// ความเร็วตอนโผล่มาเกิดใหม่ ให้ลอยเฉียงเข้าจอทางขวานิดหน่อย
    /// แบบต้นฉบับที่ตัวละครร่วงลงมาพร้อมเคลื่อนเข้าสนาม ไม่ใช่ตกดิ่งนิ่งๆ
    /// </summary>
    [SerializeField] private Vector2 respawnDropVelocity =
        new Vector2(2.5f, 1.5f);

    /// <summary>
    /// เลเยอร์ที่ถือว่า "ตัน" ตอนหาที่เกิด
    ///
    /// เกิดในจอแล้วมีโอกาสไปโผล่ซ้อนกับพื้นหรือเพดานในห้องบอส
    /// ใส่เลเยอร์พื้นไว้ ระบบจะไล่หาที่ว่างลงมาให้เอง
    /// ว่างไว้ = ไม่ต้องหา เกิดตรงที่คำนวณได้เลย
    /// </summary>
    [SerializeField] private LayerMask respawnBlockers;

    /// <summary>รัศมีที่ใช้เช็คว่าที่ตรงนั้นว่างพอให้ยืนไหม</summary>
    [SerializeField] private float respawnClearRadius = 0.5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Out of Camera Death (หลุดกรอบกล้องแล้วตาย)")]
    /// <summary>
    /// ออกนอกกรอบที่กล้องเห็น = ตาย
    ///
    /// ใช้กรอบที่ "มองเห็นจริง" ไม่ใช่ขนาดหน้าต่าง
    /// ถ้าล็อกจอไว้ 4:3 ด้วย ScreenAspect ขอบที่ใช้ตัดสินก็เป็น 4:3 ตามนั้น
    /// </summary>
    [SerializeField] private bool dieWhenOutOfCamera = true;

    /// <summary>ตกต่ำกว่าขอบล่าง = เหว อันนี้คือตัวหลัก</summary>
    [SerializeField] private bool killBelowCamera = true;

    /// <summary>
    /// ปิดไว้เป็นค่าเริ่มต้น
    /// กระโดดสูงหรือตอนเกิดใหม่ที่ร่วงลงมาจากข้างบน จะโผล่พ้นขอบบนชั่วคราว
    /// เปิดแล้วจะตายฟรีทั้งที่เล่นถูก
    /// </summary>
    [SerializeField] private bool killAboveCamera;

    /// <summary>ถูกดันตกขอบซ้ายจนกล้องทิ้งไว้ข้างหลัง</summary>
    [SerializeField] private bool killLeftOfCamera = true;

    [SerializeField] private bool killRightOfCamera = true;

    /// <summary>เผื่อพ้นขอบไปเท่าไหร่ก่อนถือว่าหลุด กันตายทั้งที่ยังเห็นตัวอยู่</summary>
    [SerializeField] private float outOfCameraMargin = 0.6f;

    /// <summary>
    /// เว้นช่วงต้นฉากไว้ก่อนเริ่มตรวจ
    /// เฟรมแรกๆ กล้องอาจยังไม่ขยับไปที่ผู้เล่น ถ้าตรวจเลยจะตายทันทีที่เริ่มด่าน
    /// </summary>
    [SerializeField] private float outOfCameraGrace = 0.5f;

    [Header("Fall Death (เส้นก้นเหวตายตัว)")]
    /// <summary>
    /// ใช้คู่กันได้ อันบนตัดสินจากกล้อง อันนี้ตัดสินจากเส้นในโลก
    /// ด่านที่กล้องล็อกอยู่กับที่ ใช้เส้นตายตัวจะคาดเดาง่ายกว่า
    /// </summary>
    [SerializeField] private bool dieWhenFalling = true;

    /// <summary>ก้นเหว ต่ำกว่าเส้นนี้เมื่อไหร่ = ตาย เป็นเส้นตายตัวในโลก</summary>
    [SerializeField] private float fallDeathY = -8f;

    /// <summary>
    /// วัดจากขอบล่างจอแทนเส้นตายตัว
    /// เหมาะกับเกมที่กล้องเลื่อนขึ้นลงตามผู้เล่นเท่านั้น
    /// กล้องของเกมนี้ล็อกแนวตั้งอยู่ ใช้เส้นตายตัวจะคาดเดาง่ายกว่า
    /// </summary>
    [SerializeField] private bool useCameraBottom;

    [SerializeField] private float fallDeathMargin = 2f;

    [Header("Weapon")]
    [SerializeField] private bool resetWeaponOnDeath = true;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private WeaponSystem weaponSystem;

    private int currentHealth;
    private int currentLives;
    private bool isDead;
    private bool isInvulnerable;
    private Vector2 spawnPosition;

    /// <summary>ตำแหน่งตอนตาย ใช้เลือกจุดเกิดใหม่ให้ใกล้ที่เดิม</summary>
    private Vector2 deathPosition;
    private Camera mainCamera;

    public bool IsDead => isDead;
    public int CurrentLives => currentLives;

    private float barrierUntil;

    /// <summary>
    /// กำลังมีเกราะอยู่ไหม
    /// ต้นฉบับกันได้ทุกอย่างยกเว้นตกเหว และศัตรูที่มาโดนตัวจะตายทันที
    /// </summary>
    public bool HasBarrier => Time.time < barrierUntil;

    /// <summary>ไอเทม B เรียกตอนถูกเก็บ ใส่ 0 เพื่อใช้ระยะเวลามาตรฐานของด่าน</summary>
    public void ActivateBarrier(float duration)
    {
        if (duration <= 0f)
        {
            duration = GetBarrierDuration();
        }

        // เก็บซ้ำไม่ต่อเวลา แต่เริ่มนับใหม่ เหมือนต้นฉบับที่เขียนทับตัวนับ
        barrierUntil = Time.time + duration;
    }

    private float GetBarrierDuration()
    {
        if (string.IsNullOrEmpty(longBarrierScene))
            return barrierDuration;

        string scene = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name;

        return scene == longBarrierScene
            ? longBarrierDuration
            : barrierDuration;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        weaponSystem = GetComponent<WeaponSystem>();
        playerCombat = GetComponent<PlayerCombat>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        currentHealth = maxHealth;
        currentLives = startingLives;
        spawnPosition = transform.position;

        mainCamera = Camera.main;

        // หาเองถ้าไม่ได้ลากใส่ไว้
        // ตัวนี้เป็นตัวตัดสินว่าจะเกิดใหม่ตรงไหน ลืมใส่แล้วอาการจะแปลกมาก
        if (cameraFollow == null)
        {
            cameraFollow = Object.FindObjectOfType<CameraFollow>();
        }
    }

    private void Update()
    {
        CheckFallDeath();
        UpdateBarrierLook();
    }

    /// <summary>วนสีตัวละครตอนมีเกราะ ให้รู้ว่ายังอมตะอยู่</summary>
    private void UpdateBarrierLook()
    {
        if (spriteRenderer == null)
            return;

        if (!HasBarrier)
        {
            // ช่วงกะพริบหลังเกิดใหม่จัดการสีเองอยู่แล้ว อย่าไปทับ
            if (!isInvulnerable)
            {
                spriteRenderer.color = Color.white;
            }

            return;
        }

        if (barrierColors == null || barrierColors.Length == 0)
            return;

        int index = Mathf.FloorToInt(Time.time * barrierColorRate) %
            barrierColors.Length;

        spriteRenderer.color = barrierColors[index];
    }

    /// <summary>ตกต่ำกว่าขอบล่างจอ = ตายทันที แบบ Contra ต้นฉบับ</summary>
    private void CheckFallDeath()
    {
        if (isDead)
            return;

        if (CheckOutOfCamera())
            return;

        if (!dieWhenFalling)
            return;

        if (transform.position.y > GetFallDeathLine())
            return;

        // ตกจอแล้วตายเลย ไม่สนช่วงอมตะหลังเกิดใหม่
        KillByTerrain();
    }

    /// <summary>
    /// หลุดกรอบกล้องแล้วตาย
    /// คืนค่า true ถ้าตายไปแล้ว ตัวเรียกจะได้ไม่ต้องเช็คเส้นก้นเหวซ้ำ
    /// </summary>
    private bool CheckOutOfCamera()
    {
        if (!dieWhenOutOfCamera)
            return false;

        if (Time.timeSinceLevelLoad < outOfCameraGrace)
            return false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || !mainCamera.orthographic)
            return false;

        Vector2 center = mainCamera.transform.position;

        float halfHeight = mainCamera.orthographicSize + outOfCameraMargin;
        float halfWidth =
            mainCamera.orthographicSize * mainCamera.aspect + outOfCameraMargin;

        Vector3 spot = transform.position;

        bool outside =
            (killBelowCamera && spot.y < center.y - halfHeight) ||
            (killAboveCamera && spot.y > center.y + halfHeight) ||
            (killLeftOfCamera && spot.x < center.x - halfWidth) ||
            (killRightOfCamera && spot.x > center.x + halfWidth);

        if (!outside)
            return false;

        KillByTerrain();

        return true;
    }

    /// <summary>ตายเพราะฉาก ไม่ใช่เพราะโดนยิง ข้ามช่วงอมตะและเกราะทั้งหมด</summary>
    private void KillByTerrain()
    {
        currentHealth = 0;

        Die();
    }

    private float GetFallDeathLine()
    {
        if (!useCameraBottom)
            return fallDeathY;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || !mainCamera.orthographic)
            return fallDeathY;

        return mainCamera.transform.position.y -
            mainCamera.orthographicSize -
            fallDeathMargin;
    }

    private void OnDrawGizmosSelected()
    {
        // ขีดเส้นก้นเหวให้เห็นว่าตกถึงไหนถึงตาย
        float y = Application.isPlaying
            ? GetFallDeathLine()
            : fallDeathY;

        Gizmos.DrawLine(
            new Vector3(transform.position.x - 20f, y, 0f),
            new Vector3(transform.position.x + 20f, y, 0f)
        );

        DrawCameraDeathBox();
    }

    /// <summary>วาดกรอบที่ถ้าหลุดออกไปแล้วตาย เอาไว้เช็คตอนจัดฉาก</summary>
    private void DrawCameraDeathBox()
    {
        if (!dieWhenOutOfCamera)
            return;

        Camera view = mainCamera != null ? mainCamera : Camera.main;

        if (view == null || !view.orthographic)
            return;

        float halfHeight = view.orthographicSize + outOfCameraMargin;
        float halfWidth =
            view.orthographicSize * view.aspect + outOfCameraMargin;

        Gizmos.DrawWireCube(
            view.transform.position,
            new Vector3(halfWidth * 2f, halfHeight * 2f, 0f)
        );
    }

    public void TakeDamage(int damage)
    {
        // อมตะอยู่ (เพิ่งเกิดใหม่ หรือมีเกราะ) หรือตายไปแล้ว ไม่ต้องรับดาเมจซ้ำ
        if (isDead || isInvulnerable || HasBarrier)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // จำไว้ก่อน เพราะเดี๋ยวตัวละครจะถูกย้ายตอนเกิดใหม่
        deathPosition = transform.position;
        currentLives--;

        // ตายแล้วเสียอาวุธที่เก็บมา กลับไปใช้ปืนพื้นฐาน ตามต้นฉบับ Contra
        if (resetWeaponOnDeath && weaponSystem != null)
        {
            weaponSystem.SetWeapon(WeaponType.Normal);
            weaponSystem.DisableRapidFire();
        }

        if (currentLives <= 0)
        {
            Debug.Log("Game Over");
            SetPlayerActive(false);
            return;
        }

        Debug.Log("Player ตาย เหลืออีก " + currentLives + " ชีวิต");

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetPlayerActive(false);

        yield return new WaitForSeconds(respawnDelay);

        transform.position = GetRespawnPosition();

        currentHealth = maxHealth;
        isDead = false;

        SetPlayerActive(true);

        // ต้องสั่งหลังเปิดสคริปต์กลับมา ไม่งั้น HandleMovement เฟรมถัดไปจะล้างทิ้ง
        // ใส่ความเร็วแกน X ไว้ด้วย ตัวจะได้ลอยเฉียงเข้าจอ ดูมีน้ำหนัก
        if (rb != null)
        {
            Vector2 drop = respawnDropVelocity;

            // เกิดในจอแล้วห้ามดีดขึ้น เพราะอยู่ใกล้ขอบบนอยู่แล้ว
            // ด่านที่กล้องไต่ตามจะโดนดันขึ้นไปอีก กลายเป็นไต่หนีหน้าเหมือนเดิม
            // ห้องบอสก็จะลอยพ้นขอบจอไปชั่วครู่ ซึ่งมองไม่เห็นตัวเองอยู่ดี
            if (MustRespawnInsideView())
            {
                drop.y = Mathf.Min(0f, drop.y);
            }

            rb.velocity = drop;
        }

        yield return StartCoroutine(InvulnerableRoutine());
    }

    private Vector2 GetRespawnPosition()
    {
        // อิงกล้องก่อนเสมอถ้าเปิดไว้ ด่านที่กล้องเลื่อนต้องใช้ทางนี้
        if (respawnAtCamera)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null && mainCamera.orthographic)
            {
                return GetCameraRespawnPosition();
            }
        }

        // เกิดใหม่ที่ขอบซ้ายของจอแล้วตกลงมาจากด้านบน (แบบ Contra ต้นฉบับ)
        if (cameraFollow != null)
        {
            return new Vector2(
                cameraFollow.LeftBoundaryX + respawnEdgeMargin,
                spawnPosition.y + respawnHeightOffset
            );
        }

        return new Vector2(
            spawnPosition.x,
            spawnPosition.y + respawnHeightOffset
        );
    }

    /// <summary>
    /// หาจุดเกิดใหม่จากกรอบที่กล้องเห็นตอนนี้
    /// บีบให้อยู่ในจอทั้งสองแกน ตายนอกจอก็ยังเกิดในจอ
    /// </summary>
    private Vector2 GetCameraRespawnPosition()
    {
        Vector2 center = mainCamera.transform.position;

        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        float left = center.x - halfWidth + respawnEdgeMargin;
        float right = center.x + halfWidth - respawnEdgeMargin;

        float x = respawnNearDeathSpot
            ? Mathf.Clamp(deathPosition.x, left, right)
            : left;

        // ค่าเริ่มต้นคือเกิด "ในจอ" เสมอ ตรงกับต้นฉบับที่เห็นตัวเองทันที
        //
        // สองกรณีนี้ห้ามเกิดเหนือขอบจอเด็ดขาด ต่อให้เปิดสวิตช์ไว้ก็ตาม
        //
        //   กล้องไต่ตาม  เกิดเหนือขอบแล้วกล้องจะไต่ตามขึ้นไปอีก
        //                ตายรอบหน้าก็เกิดสูงกว่าเดิม วนจนลอยหนีไปเลย
        //
        //   ล็อกห้องบอส  ร่วงลงมาโดยมองไม่เห็นจอ เจอกระสุนบอสพอดี
        //                หลบไม่ได้เพราะไม่รู้ว่าตัวเองอยู่ตรงไหน
        float y = MustRespawnInsideView()
            ? center.y + halfHeight - respawnInsideMargin
            : center.y + halfHeight + respawnDropHeight;

        return new Vector2(x, FindClearSpot(x, y, center.y - halfHeight));
    }

    /// <summary>ต้องเกิดในจอไหม ใช้ทั้งตอนหาตำแหน่งและตอนใส่ความเร็ว</summary>
    private bool MustRespawnInsideView()
    {
        if (!respawnAboveScreen)
            return true;

        if (cameraFollow == null)
            return false;

        return cameraFollow.FollowsUpward || cameraFollow.IsBossLocked;
    }

    /// <summary>
    /// ไล่หาที่ว่างลงมาจากจุดที่คำนวณได้
    ///
    /// เกิดในจอมีโอกาสไปโผล่ซ้อนกับเพดานหรือชานในห้องบอส
    /// ซึ่งฟิสิกส์จะดีดตัวออกมั่วๆ หรือหล่นทะลุพื้นไปเลย
    /// ไม่ได้ตั้ง Respawn Blockers ไว้ก็ข้ามทั้งหมด ไม่เสียแรงเช็ค
    /// </summary>
    private float FindClearSpot(float x, float y, float floorY)
    {
        if (respawnBlockers.value == 0 || respawnClearRadius <= 0f)
            return y;

        for (int step = 0; step < 24 && y > floorY; step++)
        {
            Collider2D hit = Physics2D.OverlapCircle(
                new Vector2(x, y), respawnClearRadius, respawnBlockers
            );

            if (hit == null)
                return y;

            y -= respawnClearRadius;
        }

        return y;
    }

    private IEnumerator InvulnerableRoutine()
    {
        isInvulnerable = true;

        float elapsed = 0f;

        while (elapsed < invulnerableDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        isInvulnerable = false;
    }

    private void SetPlayerActive(bool active)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }

        if (playerController != null)
        {
            playerController.enabled = active;
        }

        if (playerCombat != null)
        {
            playerCombat.enabled = active;
        }
    }
}
