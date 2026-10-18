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

    /// <summary>เพลงประจำฉากหนึ่งฉาก จับคู่ด้วยชื่อฉากตรงๆ</summary>
    [System.Serializable]
    public class SceneMusic
    {
        /// <summary>ชื่อฉากแบบไม่มี .unity เช่น Forest, Base-1, WaterFall</summary>
        public string sceneName;

        public AudioClip track;
    }

    [Header("Player Weapons")]
    public AudioClip playerNormal;
    public AudioClip playerMachineGun;
    public AudioClip playerSpread;
    public AudioClip playerLaser;
    public AudioClip playerFireball;

    [Header("Enemy")]
    public AudioClip enemyGun;
    public AudioClip enemyHit;

    [Header("Music (เพลงพื้นหลัง)")]
    /// <summary>
    /// เพลงประจำแต่ละฉาก ฉากที่ไม่อยู่ในตารางนี้จะเงียบ
    ///
    /// เล่นวนไม่รู้จบ และ **ไม่ถูกตัดด้วย Max Voice Length**
    /// เพราะอยู่คนละช่องกับเสียงเอฟเฟกต์ ไม่งั้นเพลงจะโดนตัดที่ 0.35 วิ
    /// </summary>
    public SceneMusic[] sceneMusic;

    /// <summary>เพลงสั้นตอนเคลียร์บอส ต้นฉบับคือ Triumphal Return</summary>
    public AudioClip bossClear;

    /// <summary>
    /// เล่นเพลงฉากต่อหลังเพลงเคลียร์บอสจบ
    ///
    /// ต้นฉบับไม่เล่นต่อ เพราะพอเพลงจบก็ตัดไปด่านใหม่เลย
    /// ของเรามีช่วงให้เดินไปทางออกหลังบอสตาย ถ้าไม่เล่นต่อจะเงียบยาว
    /// ปิดได้ถ้าอยากให้เงียบแบบต้นฉบับ
    /// </summary>
    public bool resumeMusicAfterBossClear = true;

    [Header("Mix")]
    public float playerVolume = 0.55f;
    public float enemyVolume = 0.4f;

    /// <summary>เพลงต้องเบากว่าเอฟเฟกต์ ไม่งั้นจะกลบเสียงปืนจนฟังไม่ออก</summary>
    [Range(0f, 1f)]
    public float musicVolume = 0.35f;

    [Range(0f, 1f)]
    public float bossClearVolume = 0.7f;

    /// <summary>
    /// เสียงเดิมจะไม่เล่นซ้ำถ้าห่างกันไม่ถึงเท่านี้
    /// กันปืนกระจายที่ยิงทีละ 5 นัดเล่นเสียงซ้อนกัน 5 ชั้น
    /// </summary>
    public float minRepeatInterval = 0.05f;

    /// <summary>สุ่มระดับเสียงนิดหน่อย ยิงรัวๆ จะได้ไม่ฟังดูเป็นเครื่องจักร</summary>
    public float pitchJitter = 0.06f;

    /// <summary>เล่นพร้อมกันได้กี่เสียง เกินกว่านี้จะไปทับช่องที่เก่าสุด</summary>
    public int channelCount = 8;

    [Header("Trim (ตัดเสียงส่วนเกิน)")]
    /// <summary>
    /// ตัดเสียงทิ้งถ้าเล่นเกินกี่วินาที 0 = ไม่ตัด
    ///
    /// ไฟล์เสียงที่อัดมามักมีหางเงียบหรือเสียงก้องต่อท้าย
    /// พอยิงรัวๆ หางพวกนี้จะทับกันจนฟังเป็นเสียงหึ่ง
    /// ตัดที่ความยาวจริงของเสียงปืนก็พอ ที่เหลือไม่ต้อง
    /// </summary>
    public float maxVoiceLength = 0.35f;

    /// <summary>
    /// เบาเสียงลงเมื่อมีหลายเสียงดังพร้อมกัน
    ///
    /// สองเสียงพร้อมกันไม่ได้ดังเป็นสองเท่า แต่ดังขึ้นพอให้แสบหู
    /// ตัวนี้หารระดับเสียงตามจำนวนที่ดังอยู่ ยิงรัวแค่ไหนก็ไม่ทะลุ
    /// 0 = ไม่ลดเลย 1 = ลดเต็มที่
    /// </summary>
    [Range(0f, 1f)]
    public float duckWhenBusy = 0.6f;

    /// <summary>เริ่มลดเสียงเมื่อมีเสียงดังพร้อมกันเกินกี่เสียง</summary>
    public int duckThreshold = 2;

    /// <summary>ไม่เจอชื่อฉากในตาราง = ฉากนั้นไม่มีเพลง ไม่ใช่ความผิดพลาด</summary>
    public AudioClip GetSceneMusic(string scene)
    {
        if (sceneMusic == null || string.IsNullOrEmpty(scene))
            return null;

        for (int i = 0; i < sceneMusic.Length; i++)
        {
            if (sceneMusic[i] != null && sceneMusic[i].sceneName == scene)
                return sceneMusic[i].track;
        }

        return null;
    }

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
