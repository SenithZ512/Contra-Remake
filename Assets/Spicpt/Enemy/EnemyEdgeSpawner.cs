using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ปล่อยศัตรูจากนอกจอฝั่งขวาเข้ามาเป็นระลอก แบบทหารวิ่งใน Contra
/// ผู้เล่นจะไม่เห็นมันโผล่มาเฉยๆ กลางจอ เพราะเกิดนอกขอบจอเสมอ
/// </summary>
public class EnemyEdgeSpawner : MonoBehaviour
{
    /// <summary>
    /// เกิดจากทางไหน
    ///
    /// ต้นฉบับสุ่มสามทางเท่าๆ กันใน gen_soldier_find_pos
    /// หนึ่งในสามคือไล่หาที่ว่างจากบนสุดของจอลงมา ซึ่งได้ทหารร่วงลงมาจากข้างบน
    /// </summary>
    public enum SpawnSide
    {
        Right,
        Left,
        Top,

        /// <summary>
        /// พุ่งขึ้นมาจากใต้จอ
        ///
        /// **อันนี้ไม่มีในต้นฉบับ** ของเดิม bottom_up_find_gen_soldier_pos
        /// เป็นการไล่หาพื้นที่ยืนได้จากล่างขึ้นบนแล้ววางทหารตรงนั้น
        /// ไม่ใช่ทหารกระโดดขึ้นมาจากใต้จอแบบนี้
        /// </summary>
        Bottom,

        /// <summary>สุ่มจากทางที่เปิดไว้</summary>
        Random
    }

    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float firstSpawnDelay = 2f;
    [SerializeField] private float spawnMargin = 2f;

    [Header("Side")]
    [SerializeField] private SpawnSide side = SpawnSide.Right;

    /// <summary>ทางที่จะสุ่ม ใช้เมื่อเลือก Random</summary>
    [SerializeField] private bool allowRight = true;
    [SerializeField] private bool allowLeft;
    [SerializeField] private bool allowTop = true;
    [SerializeField] private bool allowBottom;

    [Header("Spawn Height (ใช้กับเกิดข้างซ้ายขวา)")]
    [SerializeField] private bool useOwnHeight = true;
    [SerializeField] private float spawnY = 0f;

    [Header("Drop From Above")]
    /// <summary>เกิดเหนือขอบบนจอเท่าไหร่ ร่วงลงมาแล้วคนเล่นจะเห็นทัน</summary>
    [SerializeField] private float topSpawnMargin = 1.5f;

    /// <summary>ไม่ให้ร่วงลงมาใกล้ผู้เล่นเกินนี้ จะได้หลบทัน ไม่ใช่ตกใส่หัว</summary>
    [SerializeField] private float keepAwayFromPlayer = 2.5f;

    /// <summary>เว้นจากขอบจอซ้ายขวาเท่าไหร่ตอนสุ่มตำแหน่งที่จะร่วงลง</summary>
    [SerializeField] private float topSpawnEdgeInset = 1.5f;

    [Header("Jump Up From Below")]
    /// <summary>เกิดใต้ขอบล่างจอเท่าไหร่</summary>
    [SerializeField] private float bottomSpawnMargin = 1.5f;

    /// <summary>
    /// พุ่งขึ้นมาให้สูงกว่าขอบล่างจอเท่าไหร่
    ///
    /// คิดความเร็วให้เองจากแรงโน้มถ่วงจริงของพรีแฟบตัวนั้น
    /// เปลี่ยน Gravity Scale ที่ศัตรูแล้วไม่ต้องมาแก้ตรงนี้ซ้ำ
    /// </summary>
    [SerializeField] private float bottomRiseHeight = 3f;

    [SerializeField] private string playerTag = "Player";

    [Header("Limit")]
    [SerializeField] private int maxAlive = 3;

    [Header("Active Range (ช่วงที่กล้องต้องอยู่ถึงจะปล่อย)")]
    [SerializeField] private float activeFromX = -999f;
    [SerializeField] private float activeToX = 999f;

    private Camera mainCamera;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextSpawnTime;

    private void Awake()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "EnemyEdgeSpawner: ยังไม่ได้กำหนด Enemy Prefab"
            );
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        nextSpawnTime = Time.time + firstSpawnDelay;
    }

    private void Update()
    {
        if (mainCamera == null || enemyPrefab == null)
            return;

        float cameraX = mainCamera.transform.position.x;

        if (cameraX < activeFromX || cameraX > activeToX)
            return;

        if (Time.time < nextSpawnTime)
            return;

        if (CountAlive() >= maxAlive)
            return;

        nextSpawnTime = Time.time + spawnInterval;

        Spawn(cameraX);
    }

    private void Spawn(float cameraX)
    {
        SpawnSide chosen = side == SpawnSide.Random ? PickSide() : side;

        GameObject enemy = Instantiate(
            enemyPrefab,
            GetSpawnPosition(chosen, cameraX),
            Quaternion.identity
        );

        if (chosen == SpawnSide.Bottom)
        {
            LaunchUp(enemy);
        }

        spawned.Add(enemy);
    }

    private Vector3 GetSpawnPosition(SpawnSide chosen, float cameraX)
    {
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        if (chosen == SpawnSide.Top || chosen == SpawnSide.Bottom)
            return GetVerticalSpawnPosition(chosen, cameraX, halfWidth);

        float x = chosen == SpawnSide.Left
            ? cameraX - halfWidth - spawnMargin
            : cameraX + halfWidth + spawnMargin;

        float y = useOwnHeight ? transform.position.y : spawnY;

        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// ดีดขึ้นให้ถึงความสูงที่ตั้งไว้พอดี
    /// คิดจากแรงโน้มถ่วงจริงของตัวนั้น ไม่ใช่ใส่ความเร็วตายตัว
    /// </summary>
    private void LaunchUp(GameObject enemy)
    {
        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();

        if (body == null)
        {
            Debug.LogWarning(
                "ศัตรูตัวนี้ไม่มี Rigidbody2D เลยดีดขึ้นมาไม่ได้ " +
                "มันจะค้างอยู่ใต้จอ: " + enemy.name
            );

            return;
        }

        float gravity = Mathf.Abs(Physics2D.gravity.y) * body.gravityScale;

        if (gravity <= 0.01f)
            return;

        // ต้องพุ่งจากจุดเกิดใต้จอ ขึ้นไปให้พ้นขอบล่างตามระยะที่ตั้งไว้
        float climb = bottomSpawnMargin + bottomRiseHeight;

        body.velocity = new Vector2(
            body.velocity.x,
            Mathf.Sqrt(2f * gravity * climb)
        );
    }

    /// <summary>
    /// เกิดนอกจอบนหรือล่าง แล้วเข้ามาในจอ
    /// สุ่มตำแหน่งแนวนอนในจอ แต่เลี่ยงไม่ให้ตรงกับผู้เล่นเกินไป จะได้หลบทัน
    /// </summary>
    private Vector3 GetVerticalSpawnPosition(SpawnSide chosen, float cameraX,
        float halfWidth)
    {
        float left = cameraX - halfWidth + topSpawnEdgeInset;
        float right = cameraX + halfWidth - topSpawnEdgeInset;

        float x = Random.Range(left, right);

        Transform player = FindPlayer();

        if (player != null && keepAwayFromPlayer > 0f)
        {
            float gap = x - player.position.x;

            // ใกล้เกินไปก็ผลักออกไปข้างที่ยังอยู่ในจอ
            if (Mathf.Abs(gap) < keepAwayFromPlayer)
            {
                float pushed = player.position.x +
                    Mathf.Sign(gap == 0f ? 1f : gap) * keepAwayFromPlayer;

                x = Mathf.Clamp(pushed, left, right);
            }
        }

        float cameraY = mainCamera.transform.position.y;
        float halfHeight = mainCamera.orthographicSize;

        float y = chosen == SpawnSide.Bottom
            ? cameraY - halfHeight - bottomSpawnMargin
            : cameraY + halfHeight + topSpawnMargin;

        return new Vector3(x, y, 0f);
    }

    private SpawnSide PickSide()
    {
        // เก็บเฉพาะทางที่เปิดไว้ แล้วสุ่มเท่าๆ กัน แบบต้นฉบับ
        List<SpawnSide> options = new List<SpawnSide>();

        if (allowRight) options.Add(SpawnSide.Right);
        if (allowLeft) options.Add(SpawnSide.Left);
        if (allowTop) options.Add(SpawnSide.Top);
        if (allowBottom) options.Add(SpawnSide.Bottom);

        if (options.Count == 0)
            return SpawnSide.Right;

        return options[Random.Range(0, options.Count)];
    }

    private Transform cachedPlayer;

    private Transform FindPlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);

        if (found != null)
        {
            cachedPlayer = found.transform;
        }

        return cachedPlayer;
    }

    private int CountAlive()
    {
        spawned.RemoveAll(enemy => enemy == null);

        return spawned.Count;
    }

    private void OnDrawGizmosSelected()
    {
        // เส้นระดับความสูงที่ศัตรูจะเกิด
        float y = useOwnHeight ? transform.position.y : spawnY;

        Gizmos.DrawLine(
            new Vector3(activeFromX, y, 0f),
            new Vector3(activeToX, y, 0f)
        );
    }
}
