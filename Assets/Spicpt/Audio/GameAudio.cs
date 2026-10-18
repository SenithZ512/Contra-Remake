using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>เวลาที่แต่ละช่องเริ่มเล่น ใช้หาช่องเก่าสุดกับตัดเสียงที่ยาวเกิน</summary>
    private float[] startTimes;

    private AudioClip lastClip;
    private float lastPlayTime;

    /// <summary>
    /// เพลงพื้นหลังกับเพลงเคลียร์บอส **อยู่นอกวงช่องเอฟเฟกต์**
    ///
    /// สำคัญมาก ถ้าเอาไปปนในวง จะโดนสองอย่างเล่นงาน
    ///   PickChannel     แย่งช่องไปเล่นเสียงปืนทับเพลง
    ///   TrimLongVoices  ตัดเพลงทิ้งที่ 0.35 วิ เพราะมองว่าเป็นเสียงที่ยาวเกิน
    /// </summary>
    private AudioSource music;

    private AudioSource jingle;

    /// <summary>ฉากที่รอเปิดเพลงอยู่ ค้างไว้จนกว่าเพลงเคลียร์บอสจะจบ</summary>
    private string pendingScene;

    private string activeScene;
    private bool resumeAfterJingle;

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
        startTimes = new float[channels.Length];

        for (int i = 0; i < channels.Length; i++)
        {
            AudioSource channel = gameObject.AddComponent<AudioSource>();

            channel.playOnAwake = false;

            // 2D ล้วน ไม่มีตำแหน่งในสเปซ ตรงกับเกมยุคนั้น
            channel.spatialBlend = 0f;

            channels[i] = channel;
        }

        music = CreateLongSource(true);
        jingle = CreateLongSource(false);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // ฉากแรกโหลดไปแล้วตอนนี้ sceneLoaded จะไม่ยิงซ้ำให้ ต้องเปิดเพลงเอง
        activeScene = SceneManager.GetActiveScene().name;
        pendingScene = activeScene;
    }

    /// <summary>ช่องสำหรับเสียงยาว อยู่นอกวงหมุนเวียนและไม่โดนตัดทิ้ง</summary>
    private AudioSource CreateLongSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;

        return source;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

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

    /// <summary>
    /// เรียกตอนบอสตาย เพลงพื้นหลังหยุดทันทีแล้วขึ้นเพลงเคลียร์
    /// ตัดดิบๆ ไม่ต้องหรี่ ตรงกับต้นฉบับที่เพลงหายไปเลยในเฟรมเดียว
    /// </summary>
    public static void BossCleared()
    {
        if (Instance == null || Instance.library == null)
            return;

        Instance.PlayJingle(
            Instance.library.bossClear,
            Instance.library.bossClearVolume
        );
    }

    /// <summary>หยุดเพลงพื้นหลังเฉยๆ เผื่ออยากใช้ตอนจบเกมหรือเข้าเมนู</summary>
    public static void StopMusic()
    {
        if (Instance == null || Instance.music == null)
            return;

        Instance.music.Stop();
        Instance.pendingScene = null;
    }

    private void PlayJingle(AudioClip clip, float volume)
    {
        if (clip == null || jingle == null)
            return;

        if (music != null)
        {
            music.Stop();
        }

        jingle.clip = clip;
        jingle.volume = volume;

        jingle.Play();

        resumeAfterJingle = library.resumeMusicAfterBossClear;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeScene = scene.name;
        pendingScene = scene.name;

        // เปลี่ยนฉากแล้วไม่ต้องกลับไปเล่นเพลงฉากเดิมอีก
        resumeAfterJingle = false;
    }

    private void Update()
    {
        TrimLongVoices();
        UpdateMusic();
    }

    private void UpdateMusic()
    {
        if (music == null)
            return;

        // เพลงเคลียร์บอสมาก่อนเสมอ เพลงฉากรอจนกว่าจะจบ
        if (jingle != null && jingle.isPlaying)
            return;

        if (resumeAfterJingle)
        {
            resumeAfterJingle = false;

            if (pendingScene == null)
            {
                pendingScene = activeScene;
            }
        }

        if (pendingScene == null)
            return;

        AudioClip track = library.GetSceneMusic(pendingScene);

        pendingScene = null;

        if (track == null)
        {
            music.Stop();

            return;
        }

        // ฉากติดกันใช้เพลงเดียวกัน ปล่อยให้เล่นต่อ ไม่ต้องเริ่มใหม่ให้สะดุด
        if (music.clip == track && music.isPlaying)
            return;

        music.clip = track;
        music.volume = library.musicVolume;

        music.Play();
    }

    /// <summary>
    /// ตัดเสียงที่เล่นเกินความยาวที่ตั้งไว้
    ///
    /// ไฟล์เสียงมักมีหางเงียบหรือเสียงก้องต่อท้าย
    /// ยิงรัวๆ แล้วหางพวกนั้นทับกันจนฟังเป็นเสียงหึ่ง ตัดทิ้งไปเลยดีกว่า
    /// </summary>
    private void TrimLongVoices()
    {
        if (channels == null || library.maxVoiceLength <= 0f)
            return;

        for (int i = 0; i < channels.Length; i++)
        {
            if (!channels[i].isPlaying)
                continue;

            if (Time.unscaledTime - startTimes[i] < library.maxVoiceLength)
                continue;

            channels[i].Stop();
        }
    }

    /// <summary>มีกี่เสียงที่ดังอยู่ตอนนี้</summary>
    private int CountBusy()
    {
        int busy = 0;

        for (int i = 0; i < channels.Length; i++)
        {
            if (channels[i].isPlaying)
            {
                busy++;
            }
        }

        return busy;
    }

    /// <summary>
    /// หาช่องว่างก่อน ไม่มีค่อยแย่งช่องที่เล่นมานานที่สุด
    /// เดิมวนไปเรื่อยๆ ซึ่งอาจไปตัดเสียงที่เพิ่งเริ่มทั้งที่มีช่องว่างอยู่
    /// </summary>
    private int PickChannel()
    {
        for (int i = 0; i < channels.Length; i++)
        {
            if (!channels[i].isPlaying)
                return i;
        }

        int oldest = 0;

        for (int i = 1; i < channels.Length; i++)
        {
            if (startTimes[i] < startTimes[oldest])
            {
                oldest = i;
            }
        }

        return oldest;
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

        int index = PickChannel();

        AudioSource channel = channels[index];

        // หลายเสียงพร้อมกันไม่ได้ดังเป็นเท่าตัว แต่ดังพอให้แสบหู
        // หารระดับเสียงตามจำนวนที่ดังอยู่ ยิงรัวแค่ไหนก็ไม่ทะลุ
        int busy = CountBusy();

        if (busy >= library.duckThreshold && library.duckWhenBusy > 0f)
        {
            int over = busy - library.duckThreshold + 1;

            volume /= 1f + library.duckWhenBusy * over;
        }

        channel.clip = clip;
        channel.volume = volume;

        channel.pitch = 1f + Random.Range(
            -library.pitchJitter, library.pitchJitter
        );

        startTimes[index] = Time.unscaledTime;

        channel.Play();
    }
}
