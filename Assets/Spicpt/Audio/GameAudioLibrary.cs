using UnityEngine;

/// <summary>
/// คลังเสียงของเกม เก็บเป็นไฟล์ asset ไม่ใช่ออบเจกต์ในฉาก
///
/// ที่ต้องเป็น asset เพราะออบเจกต์ในฉากหายได้ง่ายมาก
/// กด Play แล้วออกโดยไม่เซฟ ของที่สร้างไว้ก่อนกดก็หายหมด
/// ส่วนไฟล์ asset อยู่ในโปรเจกต์จริง ไม่ขึ้นกับว่าฉากถูกเซฟหรือเปล่า
///
/// และไม่ต้องไปวางในทุกฉากด้วย GameAudio โหลดไฟล์นี้เองตอนเกมเริ่ม
/// </summary>
[CreateAssetMenu(
    fileName = "GameAudioLibrary",
    menuName = "Contra/Game Audio Library"
)]
public class GameAudioLibrary : ScriptableObject
{
    /// <summary>ต้องอยู่ในโฟลเดอร์ Resources ถึงจะโหลดตอนรันได้</summary>
    public const string ResourcePath = "GameAudioLibrary";

    [Header("Player Weapons")]
    public AudioClip playerNormal;
    public AudioClip playerMachineGun;
    public AudioClip playerSpread;
    public AudioClip playerLaser;
    public AudioClip playerFireball;

    [Header("Enemy")]
    public AudioClip enemyGun;
    public AudioClip enemyHit;

    [Header("Mix")]
    public float playerVolume = 0.55f;
    public float enemyVolume = 0.4f;

    /// <summary>
    /// เสียงเดิมจะไม่เล่นซ้ำถ้าห่างกันไม่ถึงเท่านี้
    /// กันปืนกระจายที่ยิงทีละ 5 นัดเล่นเสียงซ้อนกัน 5 ชั้น
    /// </summary>
    public float minRepeatInterval = 0.05f;

    /// <summary>สุ่มระดับเสียงนิดหน่อย ยิงรัวๆ จะได้ไม่ฟังดูเป็นเครื่องจักร</summary>
    public float pitchJitter = 0.06f;

    /// <summary>เล่นพร้อมกันได้กี่เสียง เกินกว่านี้จะไปทับช่องที่เก่าสุด</summary>
    public int channelCount = 8;

    public AudioClip GetWeaponClip(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.MachineGun: return playerMachineGun;
            case WeaponType.SpreadGun: return playerSpread;
            case WeaponType.Laser: return playerLaser;
            case WeaponType.FireBall: return playerFireball;
            default: return playerNormal;
        }
    }
}
