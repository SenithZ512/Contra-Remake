using UnityEngine;

/// <summary>
/// ไอเทมนกเหยี่ยว - ล้างศัตรูทั้งจอทันทีที่แตะ
///
/// ต้นฉบับ `destroy_all_enemies` วนทุกช่องศัตรูแล้วสั่งให้เข้าท่าตาย
/// โดยข้ามสามอย่าง
///   - Pill Box Sensor (ตัวที่ยิงแล้วออกไอเทม)
///   - Flying Capsule (แคปซูลที่บินผ่านจอ)
///   - อะไรก็ตามที่เลือดเป็น $f0 ซึ่งแปลว่า "โดนได้แต่ไม่เจ็บ"
///
/// จุดที่หลายคนเข้าใจผิดคือมันไม่ได้ฆ่าบอส
/// เพราะบอสเก็บเลือดจริงไว้คนละที่ พอถูกสั่งเข้าท่าตายมันก็แค่เสียเลือด 1 หน่วย
/// คู่มือเลยเขียนว่า "แทบไม่ทำอะไรกับยานพาหนะและเครื่องจักร"
///
/// เราจำลองด้วยการยิงดาเมจ 1 ใส่ทุกตัว
/// ศัตรูทั่วไปเลือด 1 ก็ตายหมด ส่วนบอสเลือดเยอะก็แค่โดนนัดเดียว ตรงตามต้นฉบับ
/// </summary>
public static class FalconStrike
{
    /// <summary>
    /// ล้างจอ
    /// คืนค่าจำนวนตัวที่โดน เอาไปใช้คิดคะแนนต่อได้
    /// </summary>
    public static int Detonate(int damage, GameObject flashPrefab)
    {
        EnemyHealth[] targets = Object.FindObjectsOfType<EnemyHealth>();

        int hit = 0;

        foreach (EnemyHealth target in targets)
        {
            if (target == null || target.IsDead)
                continue;

            // ปิดฝาอยู่หรือช่วงที่ตีไม่เข้า ข้ามไป ตรงกับเงื่อนไขเลือด $f0
            if (target.IsInvulnerable || target.IgnoreFalcon)
                continue;

            target.TakeDamage(damage);

            hit++;
        }

        if (flashPrefab != null)
        {
            Object.Instantiate(
                flashPrefab,
                Vector3.zero,
                Quaternion.identity
            );
        }

        return hit;
    }
}
