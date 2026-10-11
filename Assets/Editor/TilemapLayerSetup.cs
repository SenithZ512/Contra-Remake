using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// จัด Tilemap ให้เป็น 3 ชั้นแบบ Contra ต้นฉบับ แล้วใส่ component ให้ครบ
///
///   Grid Ground    พื้นตัน เหยียบได้ ลอดไม่ได้
///   ├─ OneWay_Tiles   พื้นลอดได้ กระโดดทะลุขึ้น กดลง+X ลอดลง
///   └─ BG_Tiles       ฉากหลัง ไม่มี collider
///
/// กดซ้ำได้เรื่อยๆ ของที่มีอยู่แล้วจะถูกตั้งค่าทับให้ถูกต้อง ไม่สร้างซ้ำ
/// </summary>
public static class TilemapLayerSetup
{
    private const string GroundLayerName = "groundLayer";
    private const string SolidGroundTag = "SolidGround";

    /// <summary>
    /// ติดให้แผ่น collider ของชั้น OneWay_Tiles เท่านั้น
    /// ใช้แยกว่ากระสุนศัตรูควรทะลุผ่าน หรือควรระเบิดใส่
    /// </summary>
    private const string OneWayPlatformTag = "OneWayPlatform";

    private const string OneWayName = "OneWay_Tiles";
    private const string BackgroundName = "BG_Tiles";

    /// <summary>ชั้นพื้นตัน กด ลง+X ลอดลงไม่ได้ ใช้ทำพื้นด่านหลักกับขอบเหว</summary>
    private const string SolidName = "Solid_Tiles";

    /// <summary>ลูกที่ถือแผ่น collider บางๆ ของพื้นลอดได้</summary>
    private const string OneWayEdgeName = "OneWayEdge";

    private const int GroundSortingOrder = 2;

    /// <summary>
    /// ยกผิว collider ขึ้นให้ตรงกับยอดหญ้า
    /// ก้อนสูง 1.205 แต่ช่องสูง 1 รูปเลยล้นขึ้นไปข้างละ 0.1
    /// ถ้าไม่ยก ตัวละครจะยืนจมหญ้าลงไปเท่านั้น
    /// </summary>
    private const float ColliderSurfaceOffsetY = 0.05f;

    /// <summary>
    /// มุมผิวที่ยังกันไม่ให้ผ่าน 180 = กันทั้งครึ่งบนรวมด้านข้างพอดีเส้น
    /// ลดเหลือ 140 เพื่อให้เดินทะลุหน้าผาของพื้นต่างระดับได้ แต่ยังยืนบนได้
    /// </summary>
    private const float SurfaceArc = 170f;

    /// <summary>
    /// ความหนาของแผ่น collider พื้น
    /// ต้องบางกว่าความสูงตัวละครมากๆ ไม่งั้นตัวจะเข้าไปค้างข้างใน
    /// </summary>
    private const float EdgeThickness = 0.1f;
    private const int BackgroundSortingOrder = -10;

    /// <summary>ตกให้พ้นพื้นก่อน collision จะกลับมา ไม่งั้นเด้งขึ้นไปยืนที่เดิม</summary>
    private const float DropThroughDuration = 0.5f;

    [MenuItem("Tools/Contra/Setup Tilemap Layers")]
    public static void Run()
    {
        Grid grid = FindGrid();

        if (grid == null)
        {
            Debug.LogError(
                "หา Grid ในฉากไม่เจอ สร้างก่อนด้วย 2D Object > Tilemap > Rectangular"
            );

            return;
        }

        Tilemap solid = grid.GetComponent<Tilemap>();

        if (solid == null)
        {
            solid = FindChildTilemap(grid);
        }

        if (solid == null)
        {
            Debug.LogError(
                "Grid \"" + grid.name + "\" ยังไม่มี Tilemap ของพื้นตันเลย"
            );

            return;
        }

        SetupSolidGround(solid.gameObject);

        GameObject oneWay = GetOrCreateChild(grid.transform, OneWayName);
        SetupOneWayGround(oneWay);

        GameObject solidTiles = GetOrCreateChild(grid.transform, SolidName);
        SetupTileLayer(solidTiles, SolidGroundTag);

        GameObject background = GetOrCreateChild(grid.transform, BackgroundName);
        SetupBackground(background);

        FixDropThroughDuration();

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);

        Debug.Log(
            "จัด Tilemap 3 ชั้นเสร็จแล้ว\n" +
            "  " + solid.name + " = พื้นตัน (Tag " + SolidGroundTag + ")\n" +
            "  " + OneWayName + " = พื้นลอดได้ ทาแถวบนสุดแถวเดียวพอ\n" +
            "  " + BackgroundName + " = ฉากหลัง ไม่มี collider ทาดินหนาๆ ตรงนี้"
        );
    }

    // ---------- แต่ละชั้น ----------

    private static void SetupSolidGround(GameObject target)
    {
        SetLayerAndTag(target, GroundLayerName, SolidGroundTag);

        TilemapRenderer renderer = Require<TilemapRenderer>(target);
        renderer.sortingOrder = GroundSortingOrder;

        if (HasManualEdge(target))
            return;

        BuildCompositeCollider(target, false);

        // พื้นตันต้องตันจริง ถ้าใส่ effector ด้านข้างจะทะลุ
        // แล้วตัวละครเดินเข้าไปติดอยู่ในเนื้อดิน
        RemoveIfPresent<PlatformEffector2D>(target);
    }

    /// <summary>
    /// ทำแผ่น collider บางๆ ไว้เองแล้วก็อย่าไปสร้างทับ
    /// collider เต็มช่องหนา 1 หน่วย = เท่าความสูงตัวละคร เข้าไปค้างข้างในได้
    /// </summary>
    private static bool HasManualEdge(GameObject target)
    {
        if (target.transform.Find(OneWayEdgeName) == null)
            return false;

        Debug.Log(
            "ข้าม collider ของ " + target.name +
            " เพราะมีลูก \"" + OneWayEdgeName + "\" คุมอยู่แล้ว"
        );

        RemoveIfPresent<TilemapCollider2D>(target);
        RemoveIfPresent<CompositeCollider2D>(target);
        RemoveIfPresent<PlatformEffector2D>(target);
        RemoveIfPresent<Rigidbody2D>(target);

        return true;
    }

    private static void SetupOneWayGround(GameObject target)
    {
        Require<Tilemap>(target);

        // Layer เดียวกับพื้นตัน เพราะ mask ใน PlayerController ชี้มาที่นี่ทั้งคู่
        // สิ่งที่แยกว่าลอดได้หรือไม่ได้คือ Tag ไม่ใช่ Layer
        SetLayerAndTag(target, GroundLayerName, "Untagged");

        TilemapRenderer renderer = Require<TilemapRenderer>(target);
        renderer.sortingOrder = GroundSortingOrder;

        if (HasManualEdge(target))
            return;

        BuildCompositeCollider(target, true);

        SetupEffector(target);
    }

    // ---------- สร้างแผ่น collider จากก้อนที่ทาไว้จริง ----------

    [MenuItem("Tools/Contra/Clear All Tiles")]
    public static void ClearAllTiles()
    {
        Grid grid = FindGrid();

        if (grid == null)
        {
            Debug.LogError("หา Grid ในฉากไม่เจอ");
            return;
        }

        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>(true);

        // ลบแล้วกู้ไม่ได้ด้วยการกดเมนูซ้ำ ต้องถามก่อนเสมอ
        bool confirmed = EditorUtility.DisplayDialog(
            "ลบก้อนทั้งหมด",
            "จะลบก้อนที่ทาไว้ในทุกชั้นของ Grid นี้ กด Ctrl+Z คืนได้ แต่ถ้าเซฟฉากไปแล้วจะคืนไม่ได้",
            "ลบเลย",
            "ยกเลิก"
        );

        if (!confirmed)
            return;

        foreach (Tilemap map in maps)
        {
            Undo.RegisterCompleteObjectUndo(map, "Clear Tiles");

            map.ClearAllTiles();

            ClearEdges(map.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);

        Debug.Log("ลบก้อนทั้งหมดแล้ว " + maps.Length + " ชั้น");
    }

    [MenuItem("Tools/Contra/Rebuild Ground Colliders")]
    public static void RebuildAllEdges()
    {
        Grid grid = FindGrid();

        if (grid == null)
        {
            Debug.LogError("หา Grid ในฉากไม่เจอ");
            return;
        }

        int total = 0;

        foreach (Tilemap tilemap in grid.GetComponentsInChildren<Tilemap>(true))
        {
            // ชั้นฉากหลังไม่ต้องมี collider
            if (tilemap.name == BackgroundName)
            {
                ClearEdges(tilemap.gameObject);
                continue;
            }

            total += RebuildEdges(grid, tilemap);
        }

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);

        Debug.Log("สร้างแผ่นพื้นใหม่ " + total + " แผ่น ตามก้อนที่ทาไว้");
    }

    /// <summary>
    /// ไล่ทีละแถว หาช่วงที่ "มีก้อน แต่ข้างบนว่าง" = ผิวที่เหยียบได้
    /// แล้ววางแผ่นบางๆ ทับช่วงนั้น ช่วงละแผ่น
    /// </summary>
    private static int RebuildEdges(Grid grid, Tilemap tilemap)
    {
        string edgeTag = GetEdgeTag(tilemap.name);

        ClearEdges(tilemap.gameObject);

        tilemap.CompressBounds();

        BoundsInt bounds = tilemap.cellBounds;
        Vector3 cell = grid.cellSize;

        int made = 0;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            int runStart = int.MinValue;

            // วน xMax ด้วย เพื่อปิดช่วงสุดท้ายให้เรียบร้อย
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                bool isSurface =
                    x < bounds.xMax &&
                    tilemap.HasTile(new Vector3Int(x, y, 0)) &&
                    !tilemap.HasTile(new Vector3Int(x, y + 1, 0));

                if (isSurface)
                {
                    if (runStart == int.MinValue)
                    {
                        runStart = x;
                    }

                    continue;
                }

                if (runStart == int.MinValue)
                    continue;

                CreateEdge(
                    tilemap.gameObject, runStart, x - 1, y,
                    cell, made, edgeTag);

                runStart = int.MinValue;
                made++;
            }
        }

        return made;
    }

    private static void CreateEdge(
        GameObject parent,
        int x0,
        int x1,
        int row,
        Vector3 cell,
        int index,
        string edgeTag)
    {
        string name = index == 0
            ? OneWayEdgeName
            : OneWayEdgeName + " (" + index + ")";

        GameObject edge = new GameObject(name);

        Undo.RegisterCreatedObjectUndo(edge, "Create " + name);

        edge.transform.SetParent(parent.transform, false);
        edge.layer = LayerMask.NameToLayer(GroundLayerName);

        edge.tag = edgeTag;

        float width = (x1 - x0 + 1) * cell.x;
        float centerX = (x0 + x1 + 1) * 0.5f * cell.x;

        // ผิวบนของแถวนี้ บวกระยะยกให้ตรงยอดหญ้า
        float surfaceY = (row + 1) * cell.y + ColliderSurfaceOffsetY;

        edge.transform.localPosition = new Vector3(
            centerX,
            surfaceY - EdgeThickness * 0.5f,
            0f
        );

        BoxCollider2D box = edge.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, EdgeThickness);
        box.usedByEffector = true;

        SetupEffector(edge);
    }

    /// <summary>
    /// Tag ของแผ่น collider ตัดสินพฤติกรรม 3 อย่างพร้อมกัน
    ///   SolidGround     = กด ลง+X ลอดไม่ได้ และกระสุนศัตรูระเบิดใส่
    ///   OneWayPlatform  = ลอดได้ และกระสุนทะลุผ่านเสมอ
    ///   Untagged        = ลอดได้ แต่ลูกระเบิดบอสยังแตกใส่
    /// </summary>
    private static string GetEdgeTag(string tilemapName)
    {
        if (tilemapName == OneWayName)
            return OneWayPlatformTag;

        if (tilemapName == SolidName)
            return SolidGroundTag;

        return "Untagged";
    }

    private static void ClearEdges(GameObject parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.transform.GetChild(i);

            if (child.name.StartsWith(OneWayEdgeName))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // ของเก่าที่เคยใช้ collider เต็มช่อง
        RemoveIfPresent<TilemapCollider2D>(parent);
        RemoveIfPresent<CompositeCollider2D>(parent);
        RemoveIfPresent<PlatformEffector2D>(parent);
        RemoveIfPresent<Rigidbody2D>(parent);
    }

    private static void SetupEffector(GameObject target)
    {
        PlatformEffector2D effector = Require<PlatformEffector2D>(target);

        effector.useOneWay = true;

        // ไม่ติ๊กอันนี้ เท้าจะลอดก้อนแรกไปได้ แต่ไหล่ไปเกี่ยวขอบก้อนถัดไป
        effector.useOneWayGrouping = true;

        effector.surfaceArc = SurfaceArc;
        effector.rotationalOffset = 0f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    /// <summary>ชั้นไทล์ที่ใช้แผ่น collider บางๆ Tag เป็นตัวบอกว่าลอดได้หรือไม่ได้</summary>
    private static void SetupTileLayer(GameObject target, string tag)
    {
        Require<Tilemap>(target);

        SetLayerAndTag(target, GroundLayerName, tag);

        TilemapRenderer renderer = Require<TilemapRenderer>(target);
        renderer.sortingOrder = GroundSortingOrder;

        ClearEdges(target);
    }

    private static void SetupBackground(GameObject target)
    {
        Require<Tilemap>(target);

        SetLayerAndTag(target, "Default", "Untagged");

        TilemapRenderer renderer = Require<TilemapRenderer>(target);
        renderer.sortingOrder = BackgroundSortingOrder;

        // ฉากหลังห้ามมี collider เด็ดขาด ไม่งั้นดินที่วาดประดับจะกลายเป็นกำแพง
        RemoveIfPresent<TilemapCollider2D>(target);
        RemoveIfPresent<CompositeCollider2D>(target);
        RemoveIfPresent<PlatformEffector2D>(target);
        RemoveIfPresent<Rigidbody2D>(target);
    }

    // ---------- ชิ้นส่วน ----------

    private static void BuildCompositeCollider(
        GameObject target,
        bool usedByEffector)
    {
        // ต้องมี Rigidbody ก่อน ไม่งั้น Composite จะแถมมาให้เป็น Dynamic
        // แล้วพื้นทั้งด่านจะร่วงหายตอนกด Play
        Rigidbody2D body = Require<Rigidbody2D>(target);
        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = Require<CompositeCollider2D>(target);

        // พื้นตันใช้ Polygons (ก้อนตัน กระสุนเร็วๆ ไม่ทะลุ)
        // พื้นลอดได้ต้องใช้ Outlines เท่านั้น ถ้าเป็น Polygons ตัวจะจมเข้าไปในเนื้อ
        // แล้วโดนดันออกข้างสวนกับ PlatformEffector กระโดดทะลุขึ้นไม่ได้
        composite.geometryType = usedByEffector
            ? CompositeCollider2D.GeometryType.Outlines
            : CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = false;
        composite.usedByEffector = usedByEffector;

        TilemapCollider2D collider = Require<TilemapCollider2D>(target);
        collider.usedByComposite = true;

        // ตั้งให้เฉพาะตอนที่ยังไม่เคยปรับ จะได้ไม่ทับค่าที่จูนเองไว้ใน Inspector
        if (Mathf.Approximately(collider.offset.y, 0f))
        {
            collider.offset =
                new Vector2(collider.offset.x, ColliderSurfaceOffsetY);
        }

        composite.GenerateGeometry();
    }

    private static Grid FindGrid()
    {
        if (Selection.activeGameObject != null)
        {
            Grid selected =
                Selection.activeGameObject.GetComponentInParent<Grid>();

            if (selected != null)
                return selected;
        }

        return Object.FindObjectOfType<Grid>();
    }

    private static Tilemap FindChildTilemap(Grid grid)
    {
        foreach (Tilemap tilemap in grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (tilemap.name == OneWayName || tilemap.name == BackgroundName)
                continue;

            return tilemap;
        }

        return null;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.gameObject;

        GameObject child = new GameObject(name);

        Undo.RegisterCreatedObjectUndo(child, "Create " + name);

        child.transform.SetParent(parent, false);

        return child;
    }

    private static T Require<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component == null)
        {
            component = Undo.AddComponent<T>(target);
        }

        return component;
    }

    private static void RemoveIfPresent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();

        if (component != null)
        {
            Undo.DestroyObjectImmediate(component);
        }
    }

    private static void SetLayerAndTag(
        GameObject target,
        string layerName,
        string tag)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer < 0)
        {
            Debug.LogWarning("ไม่มี Layer ชื่อ \"" + layerName + "\"");
        }
        else
        {
            target.layer = layer;
        }

        target.tag = tag;
    }

    private static void FixDropThroughDuration()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        PlayerController controller = player.GetComponent<PlayerController>();

        if (controller == null)
            return;

        SerializedObject serialized = new SerializedObject(controller);

        SerializedProperty property =
            serialized.FindProperty("dropThroughDuration");

        if (property == null)
            return;

        if (property.floatValue >= DropThroughDuration)
            return;

        property.floatValue = DropThroughDuration;

        serialized.ApplyModifiedProperties();

        Debug.Log(
            "ปรับ Drop Through Duration เป็น " + DropThroughDuration +
            " วิ ให้ตกพ้นพื้นทัน"
        );
    }
}
