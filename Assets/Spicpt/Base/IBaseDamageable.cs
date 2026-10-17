using UnityEngine;

/// <summary>
/// ของในด่านฐานที่ยิงได้
///
/// ไม่ใช้ EnemyHealth ของด่านปกติ เพราะตัวนั้นทำลายออบเจกต์ทิ้งตอนตาย
/// แต่ของในฐานอย่างแกนกลางกับป้อมปืนต้องอยู่ที่เดิม เปลี่ยนเป็นภาพพังแทน
///
/// CanBeHit ใช้กับป้อมที่ปิดฝาอยู่ ยิงใส่ก็ไม่เข้า ต้องรอมันเปิด
/// </summary>
public interface IBaseDamageable
{
    bool CanBeHit { get; }

    void TakeHit(int damage, Vector2 from);
}
