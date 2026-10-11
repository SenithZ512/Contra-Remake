using System.Collections;
using UnityEngine;

/// <summary>
/// เลื่อนวัตถุไปกลับตามแนวที่กำหนด ใช้ได้ทั้งพื้นเลื่อนและลูกไฟ
///
/// กำหนดเป็น "ระยะจากจุดเริ่ม" ไม่ใช่จุดปลายทาง
/// วางของในฉากตรงไหน เส้นทางก็ขยับตามไปเอง ไม่ต้องสร้าง GameObject จุดหมายเพิ่ม
/// </summary>
public class PathMover : MonoBehaviour
{
    public enum PathMode
    {
        /// <summary>วิ่งไปสุดแล้ววิ่งกลับ</summary>
        PingPong,

        /// <summary>วิ่งไปสุดแล้ววาร์ปกลับจุดเริ่ม (อย่าใช้กับพื้นที่ผู้เล่นยืน)</summary>
        Loop
    }

    [Header("Path")]
    /// <summary>ระยะที่จะเลื่อนไป นับจากจุดที่วางไว้ในฉาก</summary>
    [SerializeField] private Vector2 moveBy = new Vector2(4f, 0f);

    [SerializeField] private float speed = 2f;
    [SerializeField] private PathMode mode = PathMode.PingPong;

    /// <summary>หยุดพักตอนถึงปลายทางทั้งสองฝั่ง</summary>
    [SerializeField] private float waitAtEnds = 0.5f;

    /// <summary>
    /// เริ่มที่จุดไหนของเส้นทาง 0 = ต้นทาง 1 = ปลายทาง
    /// วางหลายตัวแล้วตั้งต่างกัน จะได้ไม่ขยับพร้อมกันเป๊ะ
    /// </summary>
    [Range(0f, 1f)]
    [SerializeField] private float startProgress;

    private Vector2 origin;
    private Vector2 destination;

    private void Awake()
    {
        origin = transform.position;
        destination = origin + moveBy;

        transform.position = Vector2.Lerp(origin, destination, startProgress);
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (true)
        {
            yield return StartCoroutine(Travel(destination));

            if (waitAtEnds > 0f)
            {
                yield return new WaitForSeconds(waitAtEnds);
            }

            if (mode == PathMode.Loop)
            {
                transform.position = origin;
                continue;
            }

            yield return StartCoroutine(Travel(origin));

            if (waitAtEnds > 0f)
            {
                yield return new WaitForSeconds(waitAtEnds);
            }
        }
    }

    private IEnumerator Travel(Vector2 goal)
    {
        // ขยับในจังหวะฟิสิกส์ ไม่ใช่ทุกเฟรมวาดภาพ
        // เพราะ MovingPlatform ต้องพาผู้เล่นไปใน FixedUpdate เหมือนกัน
        // ถ้าคนละจังหวะ ระยะที่คำนวณได้จะไม่ตรงกัน ตัวละครจะสั่น
        while ((Vector2)transform.position != goal)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                goal,
                speed * Time.fixedDeltaTime
            );

            yield return new WaitForFixedUpdate();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // ตอนยังไม่รัน origin ยังไม่ถูกตั้ง ใช้ตำแหน่งปัจจุบันแทน
        Vector2 from = Application.isPlaying ? origin : (Vector2)transform.position;
        Vector2 to = from + moveBy;

        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.15f);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
