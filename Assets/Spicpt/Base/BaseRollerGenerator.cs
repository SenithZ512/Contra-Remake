using UnityEngine;

/// <summary>
/// ช่องปล่อยลูกกลิ้ง Darr (電光自走弾) ที่ผนังหลัง
///
/// ต้นฉบับมีเฉพาะห้องที่ 4 ของ Base 1 ลูกกลิ้งไหลออกจากช่องใต้แกนกลาง
/// แล้วกลิ้งเข้าหาเรา ระเบิดเมื่อชน ยิงแตกได้ถ้าทัน
///
/// ตัวลูกกลิ้งเองใช้ BaseProjectile ที่ปิดการเล็งผู้เล่นไว้
/// มันจะไหลตรงลงมาจากช่องที่ออก ไม่เลี้ยวตาม
/// </summary>
public class BaseRollerGenerator : MonoBehaviour
{
    [Header("Roller")]
    [SerializeField] private GameObject rollerPrefab;

    [Header("Holes (ตำแหน่งช่องบนผนัง เป็นพิกเซลแบบ NES)")]
    /// <summary>ช่องอยู่ตรงไหนบ้าง ค่าเป็นพิกเซลแนวนอนของจอ NES (0-256)</summary>
    [SerializeField] private float[] holePixelX = { 96f, 128f, 160f };

    /// <summary>ความสูงของช่องบนผนัง เป็นพิกเซลแนวตั้งของจอ NES</summary>
    [SerializeField] private float holePixelY = 120f;

    [Header("Timing")]
    [SerializeField] private float firstDelay = 3f;
    [SerializeField] private float minInterval = 1.4f;
    [SerializeField] private float maxInterval = 2.6f;

    /// <summary>ปล่อยได้สูงสุดกี่ลูกต่อห้อง 0 = ไม่จำกัด</summary>
    [SerializeField] private int maxRollers;

    [Header("Rules")]
    /// <summary>ห้องเคลียร์แล้วหยุดปล่อย ศัตรูที่เหลือหายหมดตามต้นฉบับ</summary>
    [SerializeField] private bool stopWhenRoomCleared = true;

    private BaseRoom room;
    private float timer;
    private int spawned;

    private void Awake()
    {
        room = GetComponentInParent<BaseRoom>();
    }

    private void OnEnable()
    {
        timer = firstDelay;
        spawned = 0;
    }

    private void Update()
    {
        if (rollerPrefab == null || holePixelX == null || holePixelX.Length == 0)
            return;

        if (stopWhenRoomCleared && room != null && room.IsCleared)
            return;

        if (maxRollers > 0 && spawned >= maxRollers)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = Random.Range(minInterval, maxInterval);

        Spawn();
    }

    private void Spawn()
    {
        float pixelX = holePixelX[Random.Range(0, holePixelX.Length)];

        Instantiate(
            rollerPrefab,
            BaseStageGeometry.ToWorld(pixelX, holePixelY),
            Quaternion.identity
        );

        spawned++;
    }

    private void OnDrawGizmosSelected()
    {
        if (holePixelX == null)
            return;

        // วาดช่องปล่อยให้เห็นตอนจัดฉาก
        foreach (float pixelX in holePixelX)
        {
            Gizmos.DrawWireSphere(
                BaseStageGeometry.ToWorld(pixelX, holePixelY),
                0.18f
            );
        }
    }
}
