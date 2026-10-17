using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// เครื่องปล่อยทหารประจำห้อง ถอดมาจาก indoor_soldier_gen_routine_01
///
/// กฎที่สำคัญและมองไม่เห็นถ้าไม่เปิดโค้ดดู
///
///   1. เครื่องนี้ทำงานเฉพาะเฟรมคี่ ไบต์หน่วงเวลาในตารางจึงนับเป็น 1/30 วินาที
///      ไม่ใช่ 1/60 ตามที่คนส่วนใหญ่เข้าใจ ทุกอย่างช้ากว่าที่คิดเท่าตัว
///
///   2. ถ้ามีคนโยนระเบิดอยู่ในจอ เครื่องหยุดส่งทหารทันที ไม่ว่าถึงคิวหรือยัง
///      ปล่อยมันไว้นาน ห้องก็ค้างอยู่อย่างนั้น
///
///   3. ออกจากฝั่งไหนไม่ได้สลับไปมา แต่กำหนดไว้ในบิต 0 ของทุกระลอก
///      และจุดเกิดอยู่ "ในจอ" ที่พิกเซล 88 กับ 168 ไม่ใช่นอกขอบจอ
///
///   4. ระลอกที่ปิดรอบจะนับรอบ "ก่อน" ปล่อยทหาร ลำดับนี้สำคัญ
///      เพราะทหารแดงห้ามออกตอนรอบยังเป็นศูนย์ ตัวไหนได้เป็นแดงจึงเปลี่ยนไป
/// </summary>
public class BaseSoldierGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct Wave
    {
        public BaseSoldier.Kind kind;

        /// <summary>อาวุธของทหารวิ่ง จากบิต 1-2 ของ ENEMY_ATTRIBUTES</summary>
        public BaseSoldier.Weapon weapon;

        /// <summary>ออกจากฝั่งซ้ายไหม จากบิต 0 ของ ENEMY_ATTRIBUTES</summary>
        public bool fromLeft;

        /// <summary>หน่วงกี่วินาทีหลังปล่อยตัวนี้ (ไบต์ในตาราง หารด้วย 30)</summary>
        public float delay;

        /// <summary>ระลอกนี้ปิดหนึ่งรอบ (บิต 7 ของไบต์หน่วงเวลา)</summary>
        public bool endsRound;

        /// <summary>ตัวนี้มีสิทธิ์เป็นทหารแดงถ้าห้องยังไม่เคยออก</summary>
        public bool canBeRed;
    }

    [Header("Waves (แปลงมาจาก lvl_2_enemy_gen_screen_xx)")]
    [SerializeField]
    private List<Wave> waves = new List<Wave>();

    [Header("Prefabs")]
    [SerializeField] private GameObject soldierPrefab;

    [Header("Spawn (พิกเซลแบบ NES)")]
    /// <summary>จุดเกิดฝั่งซ้าย ต้นฉบับใช้ $58</summary>
    [SerializeField] private float leftSpawnPixelX = 88f;

    /// <summary>จุดเกิดฝั่งขวา ต้นฉบับใช้ $a8</summary>
    [SerializeField] private float rightSpawnPixelX = 168f;

    /// <summary>ความสูงที่ทหารวิ่ง ต้นฉบับตรึงไว้ที่ $6d</summary>
    [SerializeField] private float spawnPixelY = 109f;

    /// <summary>หน่วงก่อนปล่อยตัวแรกของห้อง ต้นฉบับใช้ $40 เฟรม</summary>
    [SerializeField] private float startDelay = 1.07f;

    /// <summary>คนในกลุ่มสี่ยืนเหลื่อมกันเท่าไหร่ ให้ไม่ทับกันสนิท</summary>
    [SerializeField] private float groupSpacing = 0.5f;

    [Header("Red Soldier Drop (ของห้องนี้)")]
    [SerializeField] private WeaponType redSoldierWeapon = WeaponType.MachineGun;

    [SerializeField] private bool redSoldierDropsRapidFire;

    private BaseRoom room;
    private int index;
    private float timer;
    private bool finished;

    private void Awake()
    {
        room = GetComponentInParent<BaseRoom>();
    }

    private void OnEnable()
    {
        index = 0;
        timer = startDelay;
        finished = false;

        BaseSoldier.ResetGlobalState();
    }

    private void OnDisable()
    {
        // ออกจากห้องแล้วทหารที่ค้างอยู่ต้องหายไปด้วย
        // ต้นฉบับก็ล้างศัตรูทั้งจอตอนเปลี่ยนห้องเหมือนกัน
        foreach (BaseSoldier soldier in
            GetComponentsInChildren<BaseSoldier>(true))
        {
            Destroy(soldier.gameObject);
        }

        BaseSoldier.ResetGlobalState();
    }

    private void Update()
    {
        if (finished || waves.Count == 0 || soldierPrefab == null)
            return;

        if (room != null && (room.IsCleared || room.AttackRoundsFinished))
        {
            finished = true;
            return;
        }

        // มีคนโยนระเบิดอยู่ในจอ ทุกอย่างหยุดรอจนกว่าจะเก็บมันได้
        if (BaseSoldier.GrenadierCount > 0)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        SpawnCurrentWave();
    }

    private void SpawnCurrentWave()
    {
        Wave wave = waves[index];

        // นับรอบก่อนปล่อย ไม่ใช่หลังปล่อย
        // เพราะทหารแดงเช็คว่ารอบต้องไม่เป็นศูนย์ ลำดับนี้ชี้ว่าใครได้เป็นแดง
        if (wave.endsRound && room != null)
        {
            room.AddAttackRound();
        }

        Spawn(wave);

        timer = wave.delay;

        index = (index + 1) % waves.Count;
    }

    private void Spawn(Wave wave)
    {
        float pixelX = wave.fromLeft ? leftSpawnPixelX : rightSpawnPixelX;

        Vector2 origin = BaseStageGeometry.ToWorld(pixelX, spawnPixelY);

        // ทหารแดงออกได้ห้องละตัว และห้ามออกตอนรอบยังเป็นศูนย์
        bool red = wave.canBeRed && room != null && room.TryClaimRedSoldier();

        int count = wave.kind == BaseSoldier.Kind.Group ? 4 : 1;

        for (int i = 0; i < count; i++)
        {
            float back = wave.fromLeft ? -groupSpacing : groupSpacing;

            GameObject spawn = Instantiate(
                soldierPrefab,
                origin + Vector2.right * (back * i),
                Quaternion.identity,
                transform
            );

            BaseSoldier soldier = spawn.GetComponent<BaseSoldier>();

            if (soldier == null)
                continue;

            soldier.Setup(
                wave.kind,
                wave.fromLeft,
                red,
                wave.weapon,
                i,
                redSoldierWeapon,
                redSoldierDropsRapidFire
            );
        }
    }
}
