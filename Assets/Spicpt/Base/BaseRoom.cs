using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// หนึ่งห้องในฐาน
///
/// กติกาของห้องมาจาก ROM ตรงๆ
///   1. แกนกลางซ่อนอยู่หลังแผงเกราะ รอครู่นึงแล้วเปิดออก
///   2. ทหารไหลมาเป็นระลอก หยุดส่งเมื่อครบ 7 รอบ
///   3. พังแกนกลางครบ = กำแพงระเบิด รั้วไฟฟ้าดับ
///   4. กดขึ้นเพื่อเดินเข้าห้องถัดไป
///
/// ตัวนับ "รอบการโจมตี" ใช้ร่วมกันทั้งห้อง แกนกลางรอจนครบรอบถึงจะยิงสวน
/// </summary>
public class BaseRoom : MonoBehaviour
{
    [Header("Room")]
    /// <summary>เป็นห้องบอสไหม ห้องบอสขอบเดินกว้างกว่าและไม่มีรั้วไฟฟ้า</summary>
    [SerializeField] private bool isBossRoom;

    [Header("Attack Rounds")]
    /// <summary>ทหารหยุดส่งหลังครบกี่รอบ ต้นฉบับคือ 7</summary>
    [SerializeField] private int maxAttackRounds = 7;

    [Header("Wall Break")]
    [SerializeField] private GameObject wallExplosionPrefab;

    /// <summary>จุดที่ระเบิดจะเกิดตอนกำแพงพัง ว่างไว้จะใช้กลางห้อง</summary>
    [SerializeField] private Transform wallExplosionPoint;

    [SerializeField] private int wallExplosionCount = 5;
    [SerializeField] private float wallExplosionSpread = 2.5f;
    [SerializeField] private float wallExplosionInterval = 0.08f;

    [Header("References")]
    [SerializeField] private BaseEnergyField energyField;

    /// <summary>
    /// ใส่เฉพาะห้องบอส
    /// พัง Shoud ครบแล้วห้องยังไม่เคลียร์ ต้องปลุกบอสมาสู้ก่อน
    /// </summary>
    [SerializeField] private BaseBossEye boss;

    private readonly List<BaseCore> cores = new List<BaseCore>();

    private int attackRounds;
    private bool cleared;
    private bool redSoldierSpawned;

    public bool IsCleared
    {
        get { return cleared; }
    }

    public bool IsBossRoom
    {
        get { return isBossRoom; }
    }

    /// <summary>จำนวนรอบที่ทหารบุกมาแล้วในห้องนี้</summary>
    public int AttackRounds
    {
        get { return attackRounds; }
    }

    /// <summary>ครบรอบแล้วหรือยัง ใช้ทั้งหยุดส่งทหารและปลุกให้แกนกลางยิงสวน</summary>
    public bool AttackRoundsFinished
    {
        get { return attackRounds >= maxAttackRounds; }
    }

    public int CoresRemaining
    {
        get
        {
            int count = 0;

            foreach (BaseCore core in cores)
            {
                if (core != null && !core.IsDestroyed)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private void Awake()
    {
        CollectCores();
    }

    private void CollectCores()
    {
        cores.Clear();

        cores.AddRange(GetComponentsInChildren<BaseCore>(true));
    }

    /// <summary>เรียกทุกครั้งที่เข้าห้อง ตั้งทุกอย่างกลับไปที่จุดเริ่ม</summary>
    public void ResetRoom()
    {
        CollectCores();

        attackRounds = 0;
        redSoldierSpawned = false;
        cleared = false;

        if (energyField != null)
        {
            energyField.SetActive(!isBossRoom);
        }

        foreach (BaseCore core in cores)
        {
            if (core != null)
            {
                core.ResetCore();
            }
        }
    }

    /// <summary>เครื่องปล่อยทหารเรียกเมื่อส่งครบหนึ่งระลอก</summary>
    public void AddAttackRound()
    {
        attackRounds++;
    }

    /// <summary>
    /// ทหารแดงออกได้ห้องละตัวเดียว และห้ามออกในระลอกแรก
    /// เงื่อนไขนี้อยู่ใน jumping_soldier_routine_00 ของต้นฉบับ
    /// </summary>
    public bool TryClaimRedSoldier()
    {
        if (redSoldierSpawned || attackRounds <= 0)
            return false;

        redSoldierSpawned = true;

        return true;
    }

    /// <summary>แกนกลางเรียกตอนตัวเองพัง</summary>
    public void NotifyCoreDestroyed()
    {
        if (cleared)
            return;

        if (CoresRemaining > 0)
            return;

        // ห้องบอส: Shoud ครบแล้วยังไปไหนไม่ได้ บอสเพิ่งจะโผล่
        if (boss != null)
        {
            boss.Awaken();
            return;
        }

        BreakWall();
    }

    /// <summary>บอสเรียกตอนตัวเองพัง ถึงจะถือว่าห้องนี้ผ่าน</summary>
    public void NotifyBossDefeated()
    {
        if (cleared)
            return;

        BreakWall();
    }

    private void BreakWall()
    {
        cleared = true;

        if (energyField != null)
        {
            energyField.SetActive(false);
        }

        StartCoroutine(WallExplosionRoutine());
    }

    private System.Collections.IEnumerator WallExplosionRoutine()
    {
        if (wallExplosionPrefab == null)
            yield break;

        Vector3 center = wallExplosionPoint != null
            ? wallExplosionPoint.position
            : transform.position + Vector3.up;

        for (int i = 0; i < wallExplosionCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-wallExplosionSpread, wallExplosionSpread),
                Random.Range(-wallExplosionSpread * 0.3f, wallExplosionSpread * 0.3f),
                0f
            );

            Instantiate(wallExplosionPrefab, center + offset, Quaternion.identity);

            yield return new WaitForSeconds(wallExplosionInterval);
        }
    }
}
