using UnityEngine;

/// <summary>
/// ตัวเล่นเสียงกลางของเกม
///
/// **ไม่ต้องวางในฉาก** มันเกิดเองตอนเกมเริ่มแล้วอยู่ข้ามฉากไปเลย
/// เสียงทั้งหมดอ่านจากไฟล์ GameAudioLibrary ในโฟลเดอร์ Resources
///
/// ที่ทำแบบนี้เพราะถ้าเป็นออบเจกต์ในฉาก มันหายง่ายมาก
/// กด Play แล้วออกโดยไม่เซฟก็หายแล้ว และต้องไปวางซ้ำทุกฉากด้วย
///
/// สองเรื่องที่ต้องมี ไม่งั้นเสียงจะพัง
///
///   1. **กันเสียงซ้อน** ปืนกระจายยิงทีละ 5 นัด ถ้าเล่นเสียงทุกนัด
///      จะได้เสียงเดียวกันซ้อน 5 ชั้น ดังกว่าปกติ 5 เท่าและเพี้ยน
///
///   2. **จำกัดจำนวนช่อง** ใช้ AudioSource หมุนเวียนเป็นวง
///      ไม่ใช่ PlayClipAtPoint ที่สร้างออบเจกต์ใหม่ทุกครั้งแล้วทิ้ง
///
/// เสียงเป็นแบบ 2D ทั้งหมด ไม่มีซ้ายขวาตามตำแหน่ง ซึ่งตรงกับเกมยุค NES
/// </summary>
public class GameAudio : MonoBehaviour
{
    private GameAudioLibrary library;
    private AudioSource[] channels;
    private int nextChannel;

    private AudioClip lastClip;
    private float lastPlayTime;

    public static GameAudio Instance { get; private set; }

    /// <summary>
    /// เกิดเองตอนเกมเริ่ม ไม่ว่าจะเปิดฉากไหน
    /// ไม่ต้องมีใครวางไว้ในฉาก และลืมวางไม่ได้ด้วย
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Spawn()
    {
        if (Instance != null)
            return;

        GameAudioLibrary found =
            Resources.Load<GameAudioLibrary>(GameAudioLibrary.ResourcePath);

        if (found == null)
        {
            Debug.LogWarning(
                "ไม่เจอคลังเสียง สั่ง Tools > Contra > Audio > Setup Game Audio " +
                "เพื่อสร้างให้ก่อน เกมยังเล่นได้แต่จะเงียบ"
            );

            return;
        }

        GameObject host = new GameObject("Game Audio");

        DontDestroyOnLoad(host);

        GameAudio audio = host.AddComponent<GameAudio>();

        audio.Setup(found);
    }

    private void Setup(GameAudioLibrary source)
    {
        Instance = this;
        library = source;

        channels = new AudioSource[Mathf.Max(1, library.channelCount)];

        for (int i = 0; i < channels.Length; i++)
        {
            AudioSource channel = gameObject.AddComponent<AudioSource>();

            channel.playOnAwake = false;

            // 2D ล้วน ไม่มีตำแหน่งในสเปซ ตรงกับเกมยุคนั้น
            channel.spatialBlend = 0f;

            channels[i] = channel;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---------- ทางเรียกจากภายนอก ----------

    public static void PlayerShot(WeaponType weapon)
    {
        if (Instance == null || Instance.library == null)
            return;

        Instance.Play(
            Instance.library.GetWeaponClip(weapon),
            Instance.library.playerVolume
        );
    }

    public static void EnemyShot()
    {
        if (Instance == null || Instance.library == null)
            return;

        Instance.Play(Instance.library.enemyGun, Instance.library.enemyVolume);
    }

    public static void EnemyHit()
    {
        if (Instance == null || Instance.library == null)
            return;

        Instance.Play(Instance.library.enemyHit, Instance.library.enemyVolume);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || channels == null)
            return;

        // เสียงเดิมเพิ่งเล่นไป ไม่ต้องซ้ำ
        if (clip == lastClip &&
            Time.unscaledTime - lastPlayTime < library.minRepeatInterval)
        {
            return;
        }

        lastClip = clip;
        lastPlayTime = Time.unscaledTime;

        AudioSource channel = channels[nextChannel];

        nextChannel = (nextChannel + 1) % channels.Length;

        channel.clip = clip;
        channel.volume = volume;

        channel.pitch = 1f + Random.Range(
            -library.pitchJitter, library.pitchJitter
        );

        channel.Play();
    }
}
