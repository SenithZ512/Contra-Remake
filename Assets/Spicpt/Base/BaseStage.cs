using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวคุมด่านฐานทั้งด่าน
///
/// ด่านนี้ไม่มีกล้องเลื่อนและไม่มีฉากต่อกัน มีแต่ "ห้อง" ที่เปิดทีละห้อง
/// ห้องที่ไม่ได้เล่นอยู่จะถูกปิดทิ้งไว้เฉยๆ ไม่กิน CPU
///
/// ลำดับของห้องเรียงตามลูกที่อยู่ใต้ตัวนี้ ย้ายสลับใน Hierarchy ได้เลย
/// </summary>
public class BaseStage : MonoBehaviour
{
    [Header("Rooms (เรียงตามลำดับลูกใน Hierarchy)")]
    [SerializeField] private List<BaseRoom> rooms = new List<BaseRoom>();

    [Header("Transition (จังหวะเดินเข้าห้องถัดไป)")]
    /// <summary>เดินเข้าจอนานแค่ไหนก่อนตัดเข้าห้องใหม่</summary>
    [SerializeField] private float walkInDuration = 0.9f;

    /// <summary>จอดำคั่นระหว่างห้อง ต้นฉบับเฟดสั้นมาก</summary>
    [SerializeField] private float blackoutDuration = 0.35f;

    /// <summary>เข้าห้องใหม่แล้วตัวละครกระโดดเบาๆ หนึ่งที แบบต้นฉบับ</summary>
    [SerializeField] private bool hopOnEnterRoom = true;

    [Header("Stage Clear (จบด่านแล้วไปต่อ)")]
    /// <summary>
    /// เก็บบอสแล้วกดขึ้นที่ห้องสุดท้าย จะเดินเข้าจอไปด่านถัดไปเลย
    ///
    /// ด่านฐานไม่มีทางให้เดินไปหาทางออกเหมือนด่านข้าง
    /// เลยใช้การกดขึ้นผ่านห้องสุดท้ายเป็นตัวจบด่านแทน ซึ่งเป็นท่าเดียวกับที่ใช้ทั้งด่าน
    /// </summary>
    [SerializeField] private bool goToNextSceneWhenCleared = true;

    [SerializeField] private float clearDelay = 0.4f;

    [SerializeField] private float clearFadeDuration = 0.9f;

    /// <summary>ว่างไว้ = ไปฉากถัดไปตามลำดับใน Build Settings</summary>
    [SerializeField] private string overrideSceneName;

    [Header("References")]
    [SerializeField] private BasePlayer player;
    [SerializeField] private SpriteRenderer blackoutOverlay;

    private int currentIndex = -1;
    private bool transitioning;

    public static BaseStage Instance { get; private set; }

    public BaseRoom CurrentRoom
    {
        get
        {
            if (currentIndex < 0 || currentIndex >= rooms.Count)
                return null;

            return rooms[currentIndex];
        }
    }

    public BasePlayer Player
    {
        get { return player; }
    }

    /// <summary>
    /// ความแรงของปืนที่ผู้เล่นถืออยู่ 0 = อ่อนสุด 3 = แรงสุด
    ///
    /// ต้นฉบับเก็บไว้ที่ PLAYER_WEAPON_STRENGTH แล้วเอาไปปรับความถี่ยิงของศัตรู
    /// ถือปืนแรงมา ทั้งบอสและป้อม Zark จะยิงถี่ขึ้น
    /// เป็นการปรับความยากที่ซ่อนอยู่ ไม่ได้บอกคนเล่น
    /// </summary>
    public static int PlayerWeaponStrength
    {
        get
        {
            BasePlayer current = Instance != null ? Instance.player : null;

            WeaponSystem weapons = current != null
                ? current.GetComponent<WeaponSystem>()
                : null;

            if (weapons == null)
                return 0;

            switch (weapons.CurrentWeapon)
            {
                case WeaponType.MachineGun: return 1;
                case WeaponType.Laser: return 2;
                case WeaponType.FireBall: return 2;
                case WeaponType.SpreadGun: return 3;
                default: return 0;
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        if (player == null)
        {
            player = Object.FindObjectOfType<BasePlayer>();
        }

        // ไม่ได้ลากห้องใส่ไว้ก็เก็บจากลูกให้เอง เรียงตามลำดับใน Hierarchy
        if (rooms.Count == 0)
        {
            rooms.AddRange(GetComponentsInChildren<BaseRoom>(true));
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        foreach (BaseRoom room in rooms)
        {
            if (room != null)
            {
                room.gameObject.SetActive(false);
            }
        }

        SetBlackout(0f);

        EnterRoom(0);
    }

    /// <summary>ผู้เล่นกดขึ้นตอนห้องเคลียร์แล้ว</summary>
    public void RequestAdvance()
    {
        if (transitioning)
            return;

        BaseRoom room = CurrentRoom;

        if (room == null || !room.IsCleared)
            return;

        StartCoroutine(AdvanceRoutine());
    }

    private IEnumerator AdvanceRoutine()
    {
        transitioning = true;

        if (player != null)
        {
            player.BeginWalkIntoScreen(walkInDuration);
        }

        yield return new WaitForSeconds(walkInDuration);

        yield return StartCoroutine(FadeBlackout(0f, 1f, blackoutDuration));

        int next = currentIndex + 1;

        if (next >= rooms.Count)
        {
            Debug.Log("เคลียร์ทุกห้องแล้ว จบด่าน");

            // ห้องสุดท้ายคือ Garmakilma เคลียร์ได้ = เคลียร์บอส
            GameAudio.BossCleared();

            if (goToNextSceneWhenCleared)
            {
                // จอดำอยู่แล้วจากจังหวะเปลี่ยนห้อง ต่อด้วยการโหลดฉากถัดไปได้เลย
                SceneFlow.GoToNext(
                    this,
                    clearDelay,
                    clearFadeDuration,
                    overrideSceneName,
                    false
                );

                yield break;
            }

            transitioning = false;

            yield break;
        }

        EnterRoom(next);

        yield return StartCoroutine(FadeBlackout(1f, 0f, blackoutDuration));

        transitioning = false;
    }

    private void EnterRoom(int index)
    {
        if (currentIndex >= 0 && currentIndex < rooms.Count &&
            rooms[currentIndex] != null)
        {
            rooms[currentIndex].gameObject.SetActive(false);
        }

        currentIndex = index;

        BaseRoom room = CurrentRoom;

        if (room == null)
            return;

        room.gameObject.SetActive(true);
        room.ResetRoom();

        if (player == null)
            return;

        player.ApplyRoomBounds(room);

        // ต้นฉบับใช้ INDOOR_PLAYER_JUMP_FLAG สั่งให้กระโดดทันทีที่ถึงห้องใหม่
        player.EndWalkIntoScreen(hopOnEnterRoom);
    }

    private IEnumerator FadeBlackout(float from, float to, float duration)
    {
        if (blackoutOverlay == null || duration <= 0f)
        {
            SetBlackout(to);

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            SetBlackout(Mathf.Lerp(from, to, elapsed / duration));

            yield return null;
        }

        SetBlackout(to);
    }

    private void SetBlackout(float alpha)
    {
        if (blackoutOverlay == null)
            return;

        Color color = blackoutOverlay.color;
        color.a = alpha;

        blackoutOverlay.color = color;
    }
}
