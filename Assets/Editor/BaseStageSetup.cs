using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// สร้างฉาก Base-1 ทั้งด่านจากข้อมูลใน ROM ต้นฉบับ
///
/// ตำแหน่งของทุกอย่างคัดมาจาก level_2_enemy_screen_00 ถึง 05
/// และจังหวะปล่อยทหารมาจาก lvl_2_enemy_gen_screen_00 ถึง 04
/// เลขพิกเซลในไฟล์นี้คือเลขเดียวกับใน ROM ไม่ได้กะเอา
///
/// กดเมนูซ้ำได้ตลอด มันจะล้างของเดิมแล้วสร้างใหม่หมด
/// </summary>
public static class BaseStageSetup
{
    private const string ScenePath = "Assets/Scenes/Base-1.unity";
    private const string PrefabFolder = "Assets/Prefab/Base";
    private const string ExplosionPath = "Assets/Ref/Explosion.prefab";

    private const string StageRootName = "Base 1";

    // ---------- ลำดับการวาด ----------
    private const int OrderBackdrop = -20;
    private const int OrderCore = 0;
    private const int OrderBoss = 1;
    private const int OrderSoldier = 2;
    private const int OrderField = 5;
    private const int OrderProjectile = 6;
    private const int OrderPlayer = 10;
    private const int OrderBullet = 11;
    private const int OrderItem = 12;
    private const int OrderBlackout = 100;

    private static GameObject explosionPrefab;
    private static GameObject bulletPrefab;
    private static GameObject enemyShotPrefab;
    private static GameObject grenadePrefab;
    private static GameObject rollerPrefab;
    private static GameObject fireRingPrefab;
    private static GameObject soldierPrefab;
    private static GameObject weaponItemPrefab;

    [MenuItem("Tools/Contra/Base 1/2. Build Scene")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(BaseStageArt.ArtFolder))
        {
            Debug.LogError(
                "ยังไม่มีสไปรท์ สั่ง Tools > Contra > Base 1 > " +
                "1. Generate Placeholder Art ก่อน"
            );

            return;
        }

        EnsurePrefabFolder();

        explosionPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);

        if (explosionPrefab == null)
        {
            Debug.LogWarning(
                "หา Explosion prefab ไม่เจอที่ " + ExplosionPath +
                " ฉากจะสร้างได้แต่ไม่มีเอฟเฟกต์ระเบิด"
            );
        }

        // ถามก่อนทิ้งงานที่ยังไม่เซฟในฉากที่เปิดอยู่ กดยกเลิกได้
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath, OpenSceneMode.Single
        );

        ClearScene(scene);

        // สร้างพรีแฟบหลังเปิดฉากแล้ว ออบเจกต์ชั่วคราวจะได้ไม่ไปค้างในฉากอื่น
        BuildPrefabs();

        SetUpCamera();

        GameObject root = new GameObject(StageRootName);
        BaseStage stage = root.AddComponent<BaseStage>();

        BasePlayer player = BuildPlayer();
        SpriteRenderer blackout = BuildBlackout();

        List<BaseRoom> rooms = new List<BaseRoom>
        {
            BuildRoom0(root.transform),
            BuildRoom1(root.transform),
            BuildRoom2(root.transform),
            BuildRoom3(root.transform),
            BuildRoom4(root.transform),
            BuildBossRoom(root.transform)
        };

        SerializedObject stageData = new SerializedObject(stage);

        stageData.FindProperty("player").objectReferenceValue = player;
        stageData.FindProperty("blackoutOverlay").objectReferenceValue = blackout;

        SerializedProperty roomList = stageData.FindProperty("rooms");
        roomList.arraySize = rooms.Count;

        for (int i = 0; i < rooms.Count; i++)
        {
            roomList.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        }

        stageData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("สร้างฉาก Base-1 เสร็จแล้ว รวม " + rooms.Count + " ห้อง");
    }

    private static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefab"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab");
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefab", "Base");
        }
    }

    private static void ClearScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------- กล้อง ----------

    private static void SetUpCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();

        camera.orthographic = true;

        // จอ NES สูง 240 px ที่ 24 px ต่อหน่วย = 10 หน่วย ครึ่งนึงคือ 5
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        cameraObject.AddComponent<AudioListener>();

        // ล็อกกรอบภาพเป็น 4:3 แบบทีวียุคนั้น จอ 16:10 จะได้ไม่เห็นกว้างเกินจริง
        cameraObject.AddComponent<ScreenAspect>();
    }

    // ---------- พรีแฟบ ----------

    private static void BuildPrefabs()
    {
        bulletPrefab = SavePrefab(MakeBullet(), "Base_Bullet");
        enemyShotPrefab = SavePrefab(MakeEnemyShot(), "Base_EnemyShot");
        grenadePrefab = SavePrefab(MakeGrenade(), "Base_Grenade");
        rollerPrefab = SavePrefab(MakeRoller(), "Base_Roller");
        fireRingPrefab = SavePrefab(MakeFireRing(), "Base_FireRing");
        weaponItemPrefab = SavePrefab(MakeWeaponItem(), "Base_WeaponItem");

        // ทหารต้องรู้จักกระสุนกับไอเท็ม เลยต้องสร้างทีหลัง
        soldierPrefab = SavePrefab(MakeSoldier(), "Base_Soldier");
    }

    private static GameObject SavePrefab(GameObject source, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(source, path);

        Object.DestroyImmediate(source);

        return saved;
    }

    private static GameObject MakeBullet()
    {
        GameObject go = NewSprite("Base_Bullet", "Base_Bullet", OrderBullet);

        AddTriggerCircle(go, 0.1f);
        AddKinematicBody(go);

        BaseBullet bullet = go.AddComponent<BaseBullet>();

        SerializedObject data = new SerializedObject(bullet);

        // ระยะบินตายตัว ยืนยิงแล้วถึงแถวแกนกลางพอดี
        // ก้มจะสั้นลง 0.44 กระโดดจะยาวขึ้นถึง 1.6 เพราะจุดออกตัวต่างกัน
        data.FindProperty("travelDistance").floatValue = 3.8f;
        data.FindProperty("speed").floatValue = 8.4f;
        data.FindProperty("damage").intValue = 1;
        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static GameObject MakeEnemyShot()
    {
        GameObject go = NewSprite(
            "Base_EnemyShot", "Base_EnemyShot", OrderProjectile
        );

        AddTriggerCircle(go, 0.14f);
        AddKinematicBody(go);

        BaseProjectile shot = go.AddComponent<BaseProjectile>();

        SerializedObject data = new SerializedObject(shot);

        // ช้ากว่าที่ตั้งไว้ตอนแรก (7 แล้วลดเป็น 5) จะได้เห็นกระสุนลอยมาแล้วหลบทัน
        data.FindProperty("speed").floatValue = 4f;

        data.FindProperty("aimAtPlayer").boolValue = true;
        data.FindProperty("hitPoints").intValue = 0;
        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static GameObject MakeGrenade()
    {
        GameObject go = NewSprite(
            "Base_Grenade", "Base_EnemyShot", OrderProjectile
        );

        AddTriggerCircle(go, 0.16f);
        AddKinematicBody(go);

        BaseProjectile shot = go.AddComponent<BaseProjectile>();

        SerializedObject data = new SerializedObject(shot);

        // ระเบิดมือไม่วิ่งเป็นเส้นตรง มันถูกโยนเป็นวิถีโค้งแล้วตกลงพื้น
        // ต้นฉบับใช้ set_enemy_falling_arc_pos บวกความเร็วตกทุกเฟรม
        data.FindProperty("speed").floatValue = 3.2f;
        data.FindProperty("launchUpBoost").floatValue = 2.2f;
        data.FindProperty("gravity").floatValue = 5.5f;
        data.FindProperty("aimAtPlayer").boolValue = true;
        data.FindProperty("hitPoints").intValue = 0;
        data.FindProperty("spinSpeed").floatValue = 360f;

        // ตกใส่หัวก็ตายอยู่ดี หมอบไม่ช่วย ต้องเดินหนีออกจากจุดตก
        data.FindProperty("blockedByCrouch").boolValue = false;

        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static GameObject MakeRoller()
    {
        GameObject go = NewSprite("Base_Roller", "Base_Roller", OrderProjectile);

        AddTriggerCircle(go, 0.3f);
        AddKinematicBody(go);

        BaseProjectile roller = go.AddComponent<BaseProjectile>();

        SerializedObject data = new SerializedObject(roller);

        // Darr กลิ้งตรงลงมาจากช่องที่ออก ไม่เลี้ยวตามเรา และยิงแตกได้ 5 นัด
        data.FindProperty("speed").floatValue = 3.4f;
        data.FindProperty("aimAtPlayer").boolValue = false;
        data.FindProperty("hitPoints").intValue = 5;
        data.FindProperty("spinSpeed").floatValue = -540f;

        // ลูกกลิ้งกลิ้งมาตามพื้น หมอบก็โดนทับอยู่ดี ต้องยิงแตกหรือกระโดดข้าม
        data.FindProperty("blockedByCrouch").boolValue = false;

        data.FindProperty("destroyEffectPrefab").objectReferenceValue =
            explosionPrefab;
        data.FindProperty("silentSpawn").boolValue = true;

        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static GameObject MakeFireRing()
    {
        GameObject go = NewSprite(
            "Base_FireRing", "Base_FireRing", OrderProjectile
        );

        AddTriggerCircle(go, 0.38f);
        AddKinematicBody(go);

        BaseProjectile ring = go.AddComponent<BaseProjectile>();

        SerializedObject data = new SerializedObject(ring);

        // วงแหวนไฟของ Garmakilma ยิงแตกได้ 2 นัด ตามค่าในกลอสซารีของ ROM
        data.FindProperty("speed").floatValue = 4.2f;
        data.FindProperty("aimAtPlayer").boolValue = true;
        data.FindProperty("hitPoints").intValue = 2;
        data.FindProperty("spinSpeed").floatValue = 140f;

        // วงแหวนไฟของบอสใหญ่เกินกว่าจะหมอบลอด ต้องยิงแตกหรือหลบออกข้าง
        data.FindProperty("blockedByCrouch").boolValue = false;

        data.FindProperty("destroyEffectPrefab").objectReferenceValue =
            explosionPrefab;
        data.FindProperty("silentSpawn").boolValue = true;

        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static GameObject MakeWeaponItem()
    {
        GameObject go = NewSprite("Base_WeaponItem", "Base_Item", OrderItem);

        AddTriggerCircle(go, 0.32f);
        AddKinematicBody(go);

        go.AddComponent<BaseWeaponItem>();

        return go;
    }

    private static GameObject MakeSoldier()
    {
        GameObject go = NewSprite("Base_Soldier", "Base_Soldier_0", OrderSoldier);

        AddTriggerBox(go, new Vector2(0.5f, 0.85f), new Vector2(0f, 0.42f));
        AddKinematicBody(go);

        BaseSoldier soldier = go.AddComponent<BaseSoldier>();

        SerializedObject data = new SerializedObject(soldier);

        data.FindProperty("bulletPrefab").objectReferenceValue =
            enemyShotPrefab;

        data.FindProperty("grenadePrefab").objectReferenceValue =
            grenadePrefab;

        data.FindProperty("rollerPrefab").objectReferenceValue =
            rollerPrefab;

        data.FindProperty("weaponItemPrefab").objectReferenceValue =
            weaponItemPrefab;

        data.FindProperty("explosionPrefab").objectReferenceValue =
            explosionPrefab;

        data.FindProperty("spriteRenderer").objectReferenceValue =
            go.GetComponent<SpriteRenderer>();

        SerializedProperty frames = data.FindProperty("frames");
        frames.arraySize = 2;
        frames.GetArrayElementAtIndex(0).objectReferenceValue =
            LoadSprite("Base_Soldier_0");
        frames.GetArrayElementAtIndex(1).objectReferenceValue =
            LoadSprite("Base_Soldier_1");

        data.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    // ---------- ผู้เล่น ----------

    private static BasePlayer BuildPlayer()
    {
        GameObject go = new GameObject("Player");

        go.tag = "Player";

        go.transform.position = new Vector3(0f, BaseStageGeometry.FloorY, 0f);

        GameObject visual = NewSprite(
            "Sprite", "Base_Player_Stand", OrderPlayer
        );

        visual.transform.SetParent(go.transform, false);

        AddTriggerBox(go, new Vector2(0.5f, 1.2f), new Vector2(0f, 0.6f));

        go.AddComponent<WeaponSystem>();

        BasePlayer player = go.AddComponent<BasePlayer>();

        SerializedObject data = new SerializedObject(player);

        data.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;

        data.FindProperty("deathEffectPrefab").objectReferenceValue =
            explosionPrefab;

        data.FindProperty("spriteRenderer").objectReferenceValue =
            visual.GetComponent<SpriteRenderer>();

        data.FindProperty("standSprite").objectReferenceValue =
            LoadSprite("Base_Player_Stand");

        data.FindProperty("crouchSprite").objectReferenceValue =
            LoadSprite("Base_Player_Crouch");

        data.FindProperty("shockSprite").objectReferenceValue =
            LoadSprite("Base_Player_Shock");

        data.FindProperty("jumpSprite").objectReferenceValue =
            LoadSprite("Base_Player_Jump");

        data.FindProperty("deadSprite").objectReferenceValue =
            LoadSprite("Base_Player_Dead");

        SerializedProperty walk = data.FindProperty("walkSprites");
        walk.arraySize = 2;
        walk.GetArrayElementAtIndex(0).objectReferenceValue =
            LoadSprite("Base_Player_Walk_0");
        walk.GetArrayElementAtIndex(1).objectReferenceValue =
            LoadSprite("Base_Player_Walk_1");

        SerializedProperty advance = data.FindProperty("advanceSprites");
        advance.arraySize = 2;
        advance.GetArrayElementAtIndex(0).objectReferenceValue =
            LoadSprite("Base_Player_Walk_0");
        advance.GetArrayElementAtIndex(1).objectReferenceValue =
            LoadSprite("Base_Player_Walk_1");

        data.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static SpriteRenderer BuildBlackout()
    {
        GameObject go = NewSprite("Blackout", "Base_Black", OrderBlackout);

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();

        renderer.color = new Color(0f, 0f, 0f, 0f);

        // ภาพต้นฉบับ 8x8 พิกเซลที่ 24 px ต่อหน่วย = หนึ่งในสามหน่วย
        // ขยายให้คลุมจอทุกอัตราส่วน เผื่อไว้เยอะหน่อยไม่เสียหาย
        go.transform.position = new Vector3(0f, 0f, -1f);
        go.transform.localScale = new Vector3(150f, 90f, 1f);

        return renderer;
    }

    // ---------- ห้อง ----------

    /// <summary>ห้องแรก แกนเดียวกลางจอ ระดับเดียวกับปืน ไม่มีป้อมเลย</summary>
    private static BaseRoom BuildRoom0(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 1", parent, false);

        room.AddCore(128f, 103f, 8, false);

        // lvl_2_enemy_gen_screen_00 : $42,$30  $01,$01  $00,$c0
        room.AddSoldiers(WeaponType.Normal, true, new[]
        {
            Wave(BaseSoldier.Kind.Jumping, 0x02, 0x30),
            Wave(BaseSoldier.Kind.Running, 0x01, 0x01),
            Wave(BaseSoldier.Kind.Running, 0x00, 0xc0)
        });

        return room.Finish();
    }

    /// <summary>แกนอยู่ซ้าย ป้อมอยู่ขวา เก็บป้อมก่อนแล้วค่อยจัดการแกน</summary>
    private static BaseRoom BuildRoom1(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 2", parent, false);

        room.AddCore(103f, 103f, 8, false);
        room.AddTurret(151f, 103f, 4, 1);

        // lvl_2_enemy_gen_screen_01 : $46,$30 $81,$50 $01,$10 $00,$30 $00,$10 $01,$e0
        room.AddSoldiers(WeaponType.MachineGun, false, new[]
        {
            Wave(BaseSoldier.Kind.Jumping, 0x06, 0x30),
            Wave(BaseSoldier.Kind.Group,   0x01, 0x50),
            Wave(BaseSoldier.Kind.Running, 0x01, 0x10),
            Wave(BaseSoldier.Kind.Running, 0x00, 0x30),
            Wave(BaseSoldier.Kind.Running, 0x00, 0x10),
            Wave(BaseSoldier.Kind.Running, 0x01, 0xe0)
        });

        return room.Finish();
    }

    /// <summary>
    /// แกนอยู่ต่ำ ต้องหมอบยิง
    /// แกนที่ต่ำขนาดนี้จะไม่ยิงสวนเลย เป็นเงื่อนไขในโค้ดต้นฉบับ
    /// </summary>
    private static BaseRoom BuildRoom2(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 3", parent, false);

        room.AddCore(128f, 112f, 8, false);
        room.AddTurret(103f, 103f, 4, 1);
        room.AddTurret(151f, 103f, 4, 1);

        // lvl_2_enemy_gen_screen_02 : $00,$30  $c5,$a0
        // ห้องนี้ไม่มีทหารแดง ไม่มีอาวุธให้เก็บ ตรงตาม ROM
        room.AddSoldiers(WeaponType.Normal, false, new[]
        {
            Wave(BaseSoldier.Kind.Running,   0x00, 0x30),
            Wave(BaseSoldier.Kind.Grenadier, 0x05, 0xa0)
        });

        return room.Finish();
    }

    /// <summary>ป้อมอยู่เหนือแกน และมีช่องปล่อยลูกกลิ้งใต้แกน</summary>
    private static BaseRoom BuildRoom3(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 4", parent, false);

        room.AddCore(128f, 103f, 8, false);
        room.AddTurret(128f, 87f, 4, 1);
        room.AddRollerGenerator();

        // lvl_2_enemy_gen_screen_03 : $46,$20  $81,$60  $c3,$e1
        room.AddSoldiers(WeaponType.MachineGun, false, new[]
        {
            Wave(BaseSoldier.Kind.Jumping,   0x06, 0x20),
            Wave(BaseSoldier.Kind.Group,     0x01, 0x60),
            Wave(BaseSoldier.Kind.Grenadier, 0x03, 0xe1)
        });

        return room.Finish();
    }

    /// <summary>
    /// Base Core แกนใหญ่ เลือดสองเท่า ล้อมด้วยป้อมสามตัว
    /// ป้อมตัวบนคือตัวที่ฉบับ NES เพิ่มเข้ามา ฉบับตู้มีแค่สองข้าง
    /// </summary>
    private static BaseRoom BuildRoom4(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 5 - Base Core", parent, false);

        room.AddCore(128f, 103f, 16, true);
        room.AddTurret(103f, 103f, 4, 1);
        room.AddTurret(151f, 103f, 4, 1);
        room.AddTurret(128f, 80f, 4, 1);

        // lvl_2_enemy_gen_screen_04
        // $40,$30 $81,$60 $00,$10 $03,$30 $02,$10 $01,$40 $47,$10 $4a,$e0
        //
        // ตัวที่ 4 กับ 5 เป็นทหารวิ่งที่ "โยนระเบิด" (บิต 1-2 = 1) ไม่ใช่ยิงกระสุน
        // และตัวที่ 7 ($47) มีธงแดงก็จริงแต่ไม่เคยได้เป็น เพราะรอบยังเป็นศูนย์อยู่
        // ตัวที่ 8 ($4a) ปิดรอบก่อนเกิด รอบเลยขึ้นเป็น 1 แล้วมันได้เป็นแดงแทน
        room.AddSoldiers(WeaponType.FireBall, false, new[]
        {
            Wave(BaseSoldier.Kind.Jumping, 0x00, 0x30),
            Wave(BaseSoldier.Kind.Group,   0x01, 0x60),
            Wave(BaseSoldier.Kind.Running, 0x00, 0x10),
            Wave(BaseSoldier.Kind.Running, 0x03, 0x30),
            Wave(BaseSoldier.Kind.Running, 0x02, 0x10),
            Wave(BaseSoldier.Kind.Running, 0x01, 0x40),
            Wave(BaseSoldier.Kind.Jumping, 0x07, 0x10),
            Wave(BaseSoldier.Kind.Jumping, 0x0a, 0xe0)
        });

        return room.Finish();
    }

    /// <summary>
    /// ห้องบอส Garmakilma
    /// Shoud 4 ลูก Zark 2 กระบอก ผังตรงตาม level_2_enemy_screen_05
    /// </summary>
    private static BaseRoom BuildBossRoom(Transform parent)
    {
        RoomBuilder room = new RoomBuilder("Room 6 - Garmakilma", parent, true);

        room.AddZark(80f, 96f);
        room.AddZark(176f, 96f);

        room.AddShoud(128f, 96f);
        room.AddShoud(80f, 144f);
        room.AddShoud(128f, 144f);
        room.AddShoud(176f, 144f);

        room.AddBoss(128f, 64f);

        return room.Finish();
    }

    /// <summary>
    /// แปลงหนึ่งคู่ไบต์จากตาราง lvl_2_enemy_gen_screen_xx เป็นระลอกหนึ่งระลอก
    ///
    /// attributes = ไบต์แรก เอาเฉพาะบิต 0-5 (บิต 6-7 เป็นชนิดทหาร)
    ///   บิต 0   = 1 ออกจากซ้าย, 0 ออกจากขวา
    ///   บิต 1-2 = อาวุธของทหารวิ่ง (0 กระสุน, 1 ระเบิด, 2 ลูกกลิ้ง)
    ///   บิต 1   = ทหารกระโดดตัวนี้เป็นแดงได้
    ///
    /// delayByte = ไบต์ที่สอง บิต 7 คือธงปิดรอบ ที่เหลือคือจำนวน tick
    /// หนึ่ง tick = 2 เฟรม เพราะเครื่องปล่อยทำงานเฉพาะเฟรมคี่ จึงหารด้วย 30
    /// </summary>
    private static BaseSoldierGenerator.Wave Wave(BaseSoldier.Kind kind,
        int attributes, int delayByte)
    {
        BaseSoldierGenerator.Wave wave = new BaseSoldierGenerator.Wave();

        wave.kind = kind;
        wave.fromLeft = (attributes & 0x01) != 0;
        wave.delay = (delayByte & 0x7f) / 30f;
        wave.endsRound = (delayByte & 0x80) != 0;

        if (kind == BaseSoldier.Kind.Jumping)
        {
            wave.canBeRed = (attributes & 0x02) != 0;
            wave.weapon = BaseSoldier.Weapon.Bullet;
        }
        else
        {
            wave.weapon = (BaseSoldier.Weapon)((attributes >> 1) & 0x03);
        }

        return wave;
    }

    // ---------- ตัวช่วยสร้างห้อง ----------

    private class RoomBuilder
    {
        private readonly GameObject root;
        private readonly BaseRoom room;
        private readonly bool bossRoom;

        private BaseBossEye boss;

        public RoomBuilder(string name, Transform parent, bool isBossRoom)
        {
            bossRoom = isBossRoom;

            root = new GameObject(name);
            root.transform.SetParent(parent, false);

            room = root.AddComponent<BaseRoom>();

            AddBackdrop();

            if (!isBossRoom)
            {
                AddEnergyField();
            }
        }

        private void AddBackdrop()
        {
            GameObject backdrop = NewSprite(
                "Backdrop", "Base_Backdrop", OrderBackdrop
            );

            backdrop.transform.SetParent(root.transform, false);
        }

        private BaseEnergyField field;

        private void AddEnergyField()
        {
            GameObject go = new GameObject("Energy Field");
            go.transform.SetParent(root.transform, false);

            field = go.AddComponent<BaseEnergyField>();

            List<SpriteRenderer> bars = new List<SpriteRenderer>();

            // เรียงแถบต่อกันให้เต็มความกว้างของทางเดิน
            for (float x = BaseStageGeometry.CorridorLeftPx;
                x <= BaseStageGeometry.CorridorRightPx;
                x += 24f)
            {
                GameObject bar = NewSprite("Bar", "Base_Field", OrderField);

                bar.transform.SetParent(go.transform, false);

                // วางบนพื้นหน้าผนัง ไม่ใช่แปะบนผนัง จะได้อ่านว่าเป็นรั้วขวางทาง
                // ต้องอยู่เหนือหัวผู้เล่นพอสมควร ไม่งั้นแถบจะพาดกลางตัว
                bar.transform.position = BaseStageGeometry.ToWorld(x, 160f);

                bars.Add(bar.GetComponent<SpriteRenderer>());
            }

            SerializedObject data = new SerializedObject(field);
            SerializedProperty list = data.FindProperty("bars");

            list.arraySize = bars.Count;

            for (int i = 0; i < bars.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = bars[i];
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public BaseCore AddCore(float pixelX, float pixelY, int health, bool big)
        {
            GameObject go = NewSprite(
                big ? "Base Core" : "Nuclear Ball",
                "Base_Core_Closed",
                OrderCore
            );

            go.transform.SetParent(root.transform, false);
            go.transform.position = BaseStageGeometry.ToWorld(pixelX, pixelY);

            if (big)
            {
                go.transform.localScale = Vector3.one * 1.6f;
            }

            AddTriggerBox(go, Vector2.one * 0.9f, Vector2.zero);

            BaseCore core = go.AddComponent<BaseCore>();

            SerializedObject data = new SerializedObject(core);

            data.FindProperty("maxHealth").intValue = health;
            data.FindProperty("openDelay").floatValue = 4f;
            data.FindProperty("plated").boolValue = false;
            data.FindProperty("stayOpen").boolValue = true;

            // แกนไม่ยิงสวน เหมือนที่เจอในเกมจริง
            // โค้ดยังอยู่ครบ ติ๊ก Can Return Fire ใน Inspector ถ้าอยากเปิด
            data.FindProperty("canReturnFire").boolValue = false;
            data.FindProperty("fireInterval").floatValue = 0.67f;

            data.FindProperty("projectilePrefab").objectReferenceValue =
                enemyShotPrefab;

            data.FindProperty("explosionPrefab").objectReferenceValue =
                explosionPrefab;

            data.FindProperty("spriteRenderer").objectReferenceValue =
                go.GetComponent<SpriteRenderer>();

            data.FindProperty("closedSprite").objectReferenceValue =
                LoadSprite("Base_Core_Closed");

            data.FindProperty("openSprite").objectReferenceValue =
                LoadSprite("Base_Core_Open");

            data.FindProperty("brokenSprite").objectReferenceValue =
                LoadSprite("Base_Core_Broken");

            data.FindProperty("plateSprite").objectReferenceValue =
                LoadSprite("Base_Core_Plate");

            data.ApplyModifiedPropertiesWithoutUndo();

            return core;
        }

        /// <summary>Shoud ในห้องบอส เลือด 10 เปิดแล้วเปิดเลย</summary>
        public void AddShoud(float pixelX, float pixelY)
        {
            BaseCore shoud = AddCore(pixelX, pixelY, 10, false);

            shoud.gameObject.name = "Shoud";

            SerializedObject data = new SerializedObject(shoud);

            // Shoud ไม่ยิงสวน มันมีหน้าที่เป็นกุญแจปลุกบอสอย่างเดียว
            data.FindProperty("projectilePrefab").objectReferenceValue = null;
            data.FindProperty("openDelay").floatValue = 1.2f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public BaseWallTurret AddTurret(float pixelX, float pixelY, int health,
            int shots)
        {
            GameObject go = NewSprite(
                shots > 1 ? "Zark" : "Gelguge",
                "Base_Turret_Closed",
                OrderCore
            );

            go.transform.SetParent(root.transform, false);
            go.transform.position = BaseStageGeometry.ToWorld(pixelX, pixelY);

            AddTriggerBox(go, Vector2.one * 0.75f, Vector2.zero);

            BaseWallTurret turret = go.AddComponent<BaseWallTurret>();

            SerializedObject data = new SerializedObject(turret);

            data.FindProperty("maxHealth").intValue = health;
            data.FindProperty("shotsPerVolley").intValue = shots;

            // ยิงห่างขึ้นและรอนานขึ้นหลังเปิดฝา ให้มีจังหวะเข้าไปเก็บ
            data.FindProperty("fireInterval").floatValue = 2.4f;
            data.FindProperty("warmUp").floatValue = 0.7f;
            data.FindProperty("closedDuration").floatValue = 2.2f;

            data.FindProperty("projectilePrefab").objectReferenceValue =
                enemyShotPrefab;

            data.FindProperty("explosionPrefab").objectReferenceValue =
                explosionPrefab;

            data.FindProperty("spriteRenderer").objectReferenceValue =
                go.GetComponent<SpriteRenderer>();

            data.FindProperty("closedSprite").objectReferenceValue =
                LoadSprite("Base_Turret_Closed");

            data.FindProperty("openSprite").objectReferenceValue =
                LoadSprite("Base_Turret_Open");

            data.FindProperty("brokenSprite").objectReferenceValue =
                LoadSprite("Base_Turret_Broken");

            data.ApplyModifiedPropertiesWithoutUndo();

            return turret;
        }

        /// <summary>
        /// Zark ป้อมห้องบอส เลือด 8 ยิงทีเดียวสามนัด
        ///
        /// ไม่เล็งผู้เล่นเลย กางลงเป็นพัดตายตัวที่มุม -30, 0, +30 องศา
        /// ตามตาราง wall_cannon_bullet_type_and_angle = มุม 8, 6, 4
        /// </summary>
        public void AddZark(float pixelX, float pixelY)
        {
            BaseWallTurret zark = AddTurret(pixelX, pixelY, 8, 3);

            SerializedObject data = new SerializedObject(zark);

            data.FindProperty("aimsAtPlayer").boolValue = false;
            data.FindProperty("spreadAngle").floatValue = 30f;

            // ต้นฉบับ #$c0 = 192 เฟรม = 3.2 วิ, #$60 = 96 เฟรม = 1.6 วิ
            data.FindProperty("closedDuration").floatValue = 3.2f;
            data.FindProperty("closedDurationStrongWeapon").floatValue = 1.6f;

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public void AddRollerGenerator()
        {
            GameObject go = new GameObject("Roller Generator");

            go.transform.SetParent(root.transform, false);

            BaseRollerGenerator generator =
                go.AddComponent<BaseRollerGenerator>();

            SerializedObject data = new SerializedObject(generator);

            data.FindProperty("rollerPrefab").objectReferenceValue =
                rollerPrefab;

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public void AddBoss(float pixelX, float pixelY)
        {
            GameObject go = NewSprite("Garmakilma", "Base_BossEye_0", OrderBoss);

            go.transform.SetParent(root.transform, false);
            go.transform.position = BaseStageGeometry.ToWorld(pixelX, pixelY);

            AddTriggerBox(go, Vector2.one * 1.5f, Vector2.zero);

            boss = go.AddComponent<BaseBossEye>();

            SerializedObject data = new SerializedObject(boss);

            data.FindProperty("fireRingPrefab").objectReferenceValue =
                fireRingPrefab;

            data.FindProperty("explosionPrefab").objectReferenceValue =
                explosionPrefab;

            data.FindProperty("spriteRenderer").objectReferenceValue =
                go.GetComponent<SpriteRenderer>();

            SerializedProperty frames = data.FindProperty("frames");
            frames.arraySize = 2;
            frames.GetArrayElementAtIndex(0).objectReferenceValue =
                LoadSprite("Base_BossEye_0");
            frames.GetArrayElementAtIndex(1).objectReferenceValue =
                LoadSprite("Base_BossEye_1");

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public void AddSoldiers(WeaponType dropWeapon, bool dropsRapidFire,
            BaseSoldierGenerator.Wave[] waves)
        {
            GameObject go = new GameObject("Soldier Generator");

            go.transform.SetParent(root.transform, false);

            BaseSoldierGenerator generator =
                go.AddComponent<BaseSoldierGenerator>();

            SerializedObject data = new SerializedObject(generator);

            data.FindProperty("soldierPrefab").objectReferenceValue =
                soldierPrefab;

            data.FindProperty("redSoldierWeapon").enumValueIndex =
                (int)dropWeapon;

            data.FindProperty("redSoldierDropsRapidFire").boolValue =
                dropsRapidFire;

            SerializedProperty list = data.FindProperty("waves");
            list.arraySize = waves.Length;

            for (int i = 0; i < waves.Length; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);

                item.FindPropertyRelative("kind").enumValueIndex =
                    (int)waves[i].kind;

                item.FindPropertyRelative("weapon").enumValueIndex =
                    (int)waves[i].weapon;

                item.FindPropertyRelative("fromLeft").boolValue =
                    waves[i].fromLeft;

                item.FindPropertyRelative("delay").floatValue = waves[i].delay;

                item.FindPropertyRelative("endsRound").boolValue =
                    waves[i].endsRound;

                item.FindPropertyRelative("canBeRed").boolValue =
                    waves[i].canBeRed;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public BaseRoom Finish()
        {
            SerializedObject data = new SerializedObject(room);

            data.FindProperty("isBossRoom").boolValue = bossRoom;

            data.FindProperty("energyField").objectReferenceValue = field;
            data.FindProperty("boss").objectReferenceValue = boss;

            data.FindProperty("wallExplosionPrefab").objectReferenceValue =
                explosionPrefab;

            data.ApplyModifiedPropertiesWithoutUndo();

            return room;
        }
    }

    // ---------- ตัวช่วยทั่วไป ----------

    private static GameObject NewSprite(string name, string spriteName,
        int sortingOrder)
    {
        GameObject go = new GameObject(name);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();

        renderer.sprite = LoadSprite(spriteName);
        renderer.sortingOrder = sortingOrder;

        return go;
    }

    private static Sprite LoadSprite(string name)
    {
        string path = BaseStageArt.ArtFolder + "/" + name + ".png";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning("หาสไปรท์ไม่เจอ: " + path);
        }

        return sprite;
    }

    private static void AddTriggerCircle(GameObject go, float radius)
    {
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();

        collider.radius = radius;
        collider.isTrigger = true;
    }

    private static void AddTriggerBox(GameObject go, Vector2 size,
        Vector2 offset)
    {
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();

        collider.size = size;
        collider.offset = offset;
        collider.isTrigger = true;
    }

    /// <summary>
    /// ของที่ขยับต้องมี Rigidbody2D ไม่งั้น OnTriggerEnter2D ไม่ทำงาน
    /// ใช้แบบ Kinematic เพราะเราขยับด้วย transform เอง ไม่ให้ฟิสิกส์ยุ่ง
    /// </summary>
    private static void AddKinematicBody(GameObject go)
    {
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
    }
}
