using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ศัตรูเดินทะลุกันเองได้ ไม่ดันกันไถไปมา
/// แต่ยังชนพื้น ชนผู้เล่น และโดนกระสุนได้ปกติทุกอย่าง
///
/// ใช้ Physics2D.IgnoreCollision แทนการแยก Layer เพราะตัวที่ spawn
/// ระหว่างเล่นก็ได้ผลเหมือนกัน และไม่ต้องไปยุ่งกับ Physics matrix ของโปรเจกต์
/// </summary>
[DisallowMultipleComponent]
public class EnemyNoPush : MonoBehaviour
{
    private static readonly List<EnemyNoPush> active =
        new List<EnemyNoPush>();

    private Collider2D[] bodyColliders;

    private void Awake()
    {
        bodyColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        // ตัวที่เกิดใหม่สั่งไม่ชนกับทุกตัวที่อยู่ก่อนแล้ว
        // คู่เก่าๆ ตั้งค่ากันไว้ตั้งแต่ตอนมันเกิด เลยไม่ต้องวนซ้ำทั้งหมด
        for (int i = active.Count - 1; i >= 0; i--)
        {
            EnemyNoPush other = active[i];

            if (other == null)
            {
                active.RemoveAt(i);
                continue;
            }

            IgnoreBetween(this, other);
        }

        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private static void IgnoreBetween(EnemyNoPush a, EnemyNoPush b)
    {
        foreach (Collider2D first in a.bodyColliders)
        {
            if (first == null)
                continue;

            foreach (Collider2D second in b.bodyColliders)
            {
                if (second == null)
                    continue;

                Physics2D.IgnoreCollision(first, second, true);
            }
        }
    }
}
