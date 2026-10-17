using System.Collections;
using UnityEngine;

/// <summary>
/// ระเบิดต่อเนื่องหลายลูกแบบสุ่มตำแหน่ง ใช้ตอนบอสตาย
///
/// ต้องแยกเป็นออบเจกต์ของตัวเอง เพราะ EnemyHealth ทำลายศัตรูทิ้งทันทีที่ตาย
/// ถ้าเขียนคอรูทีนไว้บนตัวบอส มันจะถูกทำลายไปก่อนระเบิดลูกที่สอง
/// </summary>
public class ExplosionBurst : MonoBehaviour
{
    [Header("Burst")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int count = 12;
    [SerializeField] private float spread = 2f;
    [SerializeField] private float interval = 0.1f;

    /// <summary>
    /// สั่งระเบิดชุดใหญ่ที่ตำแหน่งหนึ่ง
    /// เรียกได้จากออบเจกต์ที่กำลังจะโดนทำลาย ชุดระเบิดจะอยู่ต่อเอง
    /// </summary>
    public static void Spawn(Vector3 position, GameObject explosionPrefab,
        int count, float spread, float interval)
    {
        if (explosionPrefab == null || count <= 0)
            return;

        GameObject host = new GameObject("Explosion Burst");

        host.transform.position = position;

        ExplosionBurst burst = host.AddComponent<ExplosionBurst>();

        burst.explosionPrefab = explosionPrefab;
        burst.count = count;
        burst.spread = spread;
        burst.interval = interval;
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f
            );

            Instantiate(
                explosionPrefab,
                transform.position + offset,
                Quaternion.identity
            );

            yield return new WaitForSeconds(interval);
        }

        Destroy(gameObject);
    }
}
