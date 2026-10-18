using UnityEngine;

/// <summary>
/// รูบนหน้าผาที่ปล่อยหินร่วง — Rock Cave ของด่าน Waterfall
///
/// ต้นฉบับ rock_cave_routine_02 ง่ายมาก
///   ปล่อยก้อนแรกหลัง 8 เฟรม
///   จากนั้นปล่อยทุก 224 เฟรม = 3.73 วินาที ไปเรื่อยๆ ไม่มีวันหยุด
///
/// ตัวหินจะไปส่ายอยู่ราวหนึ่งวินาทีก่อนร่วงเอง (ดู FallingRock)
/// ช่วงเวลารวมจากปล่อยถึงร่วงจริงจึงราว 4.8 วินาที
/// </summary>
public class RockCave : MonoBehaviour
{
    [Header("Rock")]
    [SerializeField] private GameObject rockPrefab;

    [Header("Timing")]
    /// <summary>หน่วงก่อนก้อนแรก ต้นฉบับ 8 เฟรม</summary>
    [SerializeField] private float firstDelay = 0.13f;

    /// <summary>ปล่อยทุกกี่วินาที ต้นฉบับ 224 เฟรม</summary>
    [SerializeField] private float interval = 3.73f;

    /// <summary>สุ่มบวกลบจากช่วงเวลาข้างบน 0 = ตรงเวลาเป๊ะแบบต้นฉบับ</summary>
    [SerializeField] private float intervalJitter;

    [Header("Spawn")]
    /// <summary>จุดที่หินโผล่ ว่างไว้ = ใช้ตำแหน่งของรูเอง</summary>
    [SerializeField] private Transform spawnPoint;

    /// <summary>ปล่อยได้สูงสุดกี่ก้อน 0 = ไม่จำกัด แบบต้นฉบับ</summary>
    [SerializeField] private int maxRocks;

    [Header("Camera")]
    /// <summary>
    /// ปล่อยเฉพาะตอนรูอยู่ในจอ
    /// ต้นฉบับไม่เช็ค เพราะศัตรูนอกจอถูกถอดทิ้งอยู่แล้ว
    /// ของเราวางไว้ทั้งด่านตั้งแต่แรก ถ้าไม่เช็คจะมีหินร่วงอยู่นอกจอตลอดเวลา
    /// </summary>
    [SerializeField] private bool onlyWhenOnScreen = true;

    [SerializeField] private float screenMargin = 2f;

    private float timer;
    private int spawned;

    private void Start()
    {
        timer = firstDelay;
    }

    private void Update()
    {
        if (rockPrefab == null)
            return;

        if (maxRocks > 0 && spawned >= maxRocks)
            return;

        if (onlyWhenOnScreen && !IsOnScreen())
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = interval + Random.Range(-intervalJitter, intervalJitter);

        Spawn();
    }

    private void Spawn()
    {
        Vector3 spot = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        Instantiate(rockPrefab, spot, Quaternion.identity);

        spawned++;
    }

    private bool IsOnScreen()
    {
        Camera view = Camera.main;

        if (view == null || !view.orthographic)
            return true;

        Vector2 center = view.transform.position;

        float halfHeight = view.orthographicSize + screenMargin;
        float halfWidth = view.orthographicSize * view.aspect + screenMargin;

        Vector3 spot = transform.position;

        return Mathf.Abs(spot.x - center.x) <= halfWidth &&
            Mathf.Abs(spot.y - center.y) <= halfHeight;
    }

    private void OnDrawGizmos()
    {
        Vector3 spot = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        // วาดรูกับทางที่หินจะร่วงลงไป
        Gizmos.DrawWireSphere(spot, 0.3f);
        Gizmos.DrawLine(spot, spot + Vector3.down * 4f);
    }
}
