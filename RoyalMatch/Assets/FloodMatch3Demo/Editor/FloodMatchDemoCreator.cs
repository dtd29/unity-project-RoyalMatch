#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class FloodMatchDemoCreator
{
    private const string Root = "Assets/FloodMatch3Demo";
    private const string Generated = Root + "/Generated";
    private const string Materials = Generated + "/Materials";
    private const string Prefabs = Generated + "/Prefabs";
    private const string ScenePath = Generated + "/FloodMatch3Demo.unity";

    [MenuItem("Flood Match 3/Create Demo Scene & Assets")]
    public static void CreateDemo()
    {
        EnsureFolder(Generated);
        EnsureFolder(Materials);
        EnsureFolder(Prefabs);

        Material wall = CreateMaterial("Wall V30", new Color(0.50f, 0.13f, 0.20f, 1f));
        Material dark = CreateMaterial("Dark V30", new Color(0.08f, 0.10f, 0.16f, 1f));
        Material water = CreateMaterial("Water Fill V30", new Color(0.05f, 0.55f, 1f, 1f));
        Material foam = CreateMaterial("Foam V30", Color.white);
        Material pipeOff = CreateMaterial("Pipe Off V30", new Color(0.08f, 0.09f, 0.1f, 1f));
        Material pipeOn = CreateMaterial("Pipe On V30", new Color(0.05f, 0.7f, 1f, 1f));
        Material drainClosed = CreateMaterial("Drain Closed V30", new Color(0.015f, 0.015f, 0.02f, 1f));
        Material drainOpen = CreateMaterial("Drain Open V30", new Color(0.1f, 0.8f, 1f, 1f));
        Material calm = CreateMaterial("Person Calm V30", new Color(0.55f, 0.8f, 0.55f, 1f));
        Material worried = CreateMaterial("Person Worried V30", new Color(1f, 0.78f, 0.25f, 1f));
        Material panic = CreateMaterial("Person Panic V30", new Color(1f, 0.32f, 0.32f, 1f));
        Material red = CreateMaterial("Piece Red V30", new Color(1f, 0.16f, 0.12f, 1f));
        Material yellow = CreateMaterial("Piece Yellow V30", new Color(1f, 0.82f, 0.05f, 1f));
        Material blue = CreateMaterial("Piece Blue V30", new Color(0.08f, 0.42f, 1f, 1f));
        Material tileBack = CreateMaterial("Piece Back V30", new Color(0.035f, 0.04f, 0.055f, 1f));
        Material blockSide = CreateMaterial("Block Side V30", new Color(0.018f, 0.022f, 0.032f, 1f));
        Material blockEdge = CreateMaterial("Block Edge V30", new Color(0.09f, 0.10f, 0.12f, 1f));
        Material blockHighlight = CreateMaterial("Block Highlight V30", new Color(1f, 1f, 1f, 0.92f));
        Material hole = CreateMaterial("Open Hole V30", new Color(0.005f, 0.008f, 0.012f, 1f));
        Material path = CreateMaterial("Connected Path V30", new Color(0.05f, 0.9f, 1f, 1f));

        GameObject breakFx = CreateBreakFxPrefab();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCameraAndLights();

        GameObject systems = new GameObject("Game Systems");
        systems.AddComponent<MobilePortraitSetup>();

        FloodGameManager game = systems.AddComponent<FloodGameManager>();
        game.floodSpeedMultiplier = 2.0f;
        game.finalDrainEffectSeconds = 1.6f;

        GameObject boardRoot = new GameObject("Puzzle Board Root - Path To Drain");
        boardRoot.transform.position = new Vector3(0f, -2.25f, -2.25f);

        BoardManager board = systems.AddComponent<BoardManager>();
        board.boardRoot = boardRoot.transform;
        board.redMaterial = red;
        board.yellowMaterial = yellow;
        board.blueMaterial = blue;
        board.tileBackMaterial = tileBack;
        board.blockSideMaterial = blockSide;
        board.blockEdgeMaterial = blockEdge;
        board.blockHighlightMaterial = blockHighlight;
        board.boardWallMaterial = blockSide;
        board.holeMaterial = hole;
        board.pathMaterial = path;
        board.breakEffectPrefab = breakFx;

        GameObject editorMaskRoot = new GameObject("Editor Solid Water Masks");
        editorMaskRoot.transform.position = Vector3.zero;
        board.editorSolidMaskRoot = editorMaskRoot.transform;
        board.useEditorPlacedMasks = true;
        board.createMasksInEditorAutomatically = true;
        board.editorMaskWidth = 6;
        board.editorMaskHeight = 6;
        board.editorMaskSpacing = 0.64f;
        board.editorBottomMaskHeight = 0.72f;
        board.editorBottomMaskYOffset = -0.34f;
        board.CreateEditorPlacedMasksPreview();

        GameObject waterSimObj = new GameObject("Grid Based Real Water Simulator");
        GridWaterSimulator waterSimulator = waterSimObj.AddComponent<GridWaterSimulator>();
        board.waterSimulator = waterSimulator;

        BoardInputController input = systems.AddComponent<BoardInputController>();
        input.board = board;
        input.targetCamera = cam;

        WaterController waterController = CreateStage(wall, dark, water, foam, pipeOff, pipeOn, drainClosed, drainOpen, out DrainController drain);
        FloodCharacterController person = CreatePerson(calm, worried, panic);

        GameObject waterKitObj = new GameObject("WaterKit Bridge V30");
        WaterKitBridge waterKitBridge = waterKitObj.AddComponent<WaterKitBridge>();
        waterKitBridge.waterZ = -1.72f;
        waterKitBridge.waterSortingOrder = -100;
        waterKitBridge.forceWaterRenderBehindBlocks = true;
        waterKitBridge.createWaterClipFrame = false;
        waterKitBridge.board = board;
        waterKitBridge.legacyWaterController = waterController;
        board.waterKitBridge = waterKitBridge;
        game.waterKitBridge = waterKitBridge;

        Canvas canvas = CreateCanvas();
        CreateBottomGuide(canvas.transform);

        ClearPopupUI popup = CreatePopup(canvas.transform);
        LevelSelectUI levelSelect = CreateLevelSelect(canvas.transform);

        game.board = board;
        game.water = waterController;
        game.person = person;
        game.drain = drain;
        game.popup = popup;
        game.levelSelectUI = levelSelect;

        popup.Hide();
        levelSelect.Hide();

        EditorUtility.SetDirty(game);
        EditorUtility.SetDirty(board);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Flood Match 3",
            "V30 생성 완료!\n\nV30 생성 완료! Editor 메뉴 컴파일 오류를 수정했습니다. Game 2D Water Kit 프리팹 연결용 WaterKitBridge가 포함되어 있습니다.\n\n- 위/좌/우 가장자리에서 아래 최종 배수구까지 열린 구멍 경로가 생기면 클리어\n- 아래 블럭 제거 시 위 블럭 낙하\n- 타이머/매치 수 UI 삭제\n- 게임오버 팝업에 다시하기 버튼 포함",
            "OK"
        );
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = Materials + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateBreakFxPrefab()
    {
        string path = Prefabs + "/BreakEffectV30.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject fx = new GameObject("BreakEffectV30");
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.45f;
        main.startLifetime = 0.5f;
        main.startSpeed = 2.4f;
        main.startSize = 0.13f;
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CreateMaterial("FX White V30", Color.white);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(fx, path);
        Object.DestroyImmediate(fx);
        return prefab;
    }

    private static Camera CreateCameraAndLights()
    {
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5.45f;
        camObj.transform.position = new Vector3(0f, -0.65f, -12f);
        camObj.transform.rotation = Quaternion.identity;
        camObj.transform.LookAt(new Vector3(0f, -0.65f, 0f));

        GameObject dir = new GameObject("Directional Light");
        Light light = dir.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.55f;
        dir.transform.rotation = Quaternion.Euler(25f, -25f, 0f);

        GameObject pointObj = new GameObject("Front Point Light");
        Light point = pointObj.AddComponent<Light>();
        point.type = LightType.Point;
        point.intensity = 2.3f;
        point.range = 14f;
        pointObj.transform.position = new Vector3(0f, 0.6f, -6f);

        RenderSettings.ambientLight = new Color(0.70f, 0.72f, 0.76f);

        return cam;
    }

    private static WaterController CreateStage(
        Material wall,
        Material dark,
        Material waterMat,
        Material foamMat,
        Material pipeOff,
        Material pipeOn,
        Material drainClosed,
        Material drainOpen,
        out DrainController drain
    )
    {
        GameObject stage = new GameObject("Upright Flood Stage V30");

        CreateBox("Back Wall", new Vector3(0f, -0.65f, 0.45f), new Vector3(6.25f, 9.25f, 0.12f), wall, stage.transform);
        CreateBox("Left Wall", new Vector3(-3.22f, -0.65f, -0.05f), new Vector3(0.18f, 9.25f, 0.75f), dark, stage.transform);
        CreateBox("Right Wall", new Vector3(3.22f, -0.65f, -0.05f), new Vector3(0.18f, 9.25f, 0.75f), dark, stage.transform);
        CreateBox("Top Wall", new Vector3(0f, 4.0f, -0.05f), new Vector3(6.25f, 0.18f, 0.75f), dark, stage.transform);
        CreateBox("Bottom Wall", new Vector3(0f, -5.28f, -0.05f), new Vector3(6.25f, 0.18f, 0.75f), dark, stage.transform);

        CreateBox("Puzzle Board Background", new Vector3(0f, -2.25f, -1.05f), new Vector3(5.35f, 5.35f, 0.08f), dark, stage.transform);
        CreateBox("Puzzle Acts As Water Floor Line", new Vector3(0f, 0.42f, -1.15f), new Vector3(5.55f, 0.10f, 0.22f), dark, stage.transform);
        CreateBox("Upper Player Platform", new Vector3(0f, 1.45f, -1.15f), new Vector3(5.2f, 0.18f, 0.22f), dark, stage.transform);

        GameObject waterFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        waterFill.name = "Water Fill Above Puzzle Floor";
        waterFill.transform.position = new Vector3(0f, 0.47f, -2.65f);
        waterFill.transform.localScale = new Vector3(5.55f, 0.10f, 0.08f);
        waterFill.GetComponent<Renderer>().sharedMaterial = waterMat;
        Object.DestroyImmediate(waterFill.GetComponent<Collider>());

        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "White Water Surface Line";
        surface.transform.position = new Vector3(0f, 0.52f, -2.85f);
        surface.transform.localScale = new Vector3(5.4f, 0.08f, 0.08f);
        surface.GetComponent<Renderer>().sharedMaterial = foamMat;
        surface.GetComponent<Renderer>().enabled = false;
        surface.SetActive(false);
        Object.DestroyImmediate(surface.GetComponent<Collider>());

        WaterController water = waterFill.AddComponent<WaterController>();
        water.waterFill = waterFill.transform;
        water.waterSurfaceLine = surface.transform;
        water.hideLegacySurfaceLine = true;
        water.bottomY = 0.42f;
        water.minHeight = 0.10f;
        water.maxHeight = 3.35f;

        GameObject leftPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftPipe.name = "Left Water Pipe";
        leftPipe.transform.position = new Vector3(-2.55f, 3.35f, -1.85f);
        leftPipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        leftPipe.transform.localScale = new Vector3(0.18f, 0.50f, 0.18f);
        Renderer leftPipeRenderer = leftPipe.GetComponent<Renderer>();
        leftPipeRenderer.sharedMaterial = pipeOff;

        GameObject rightPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightPipe.name = "Right Water Pipe";
        rightPipe.transform.position = new Vector3(2.55f, 3.35f, -1.85f);
        rightPipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        rightPipe.transform.localScale = new Vector3(0.18f, 0.50f, 0.18f);
        Renderer rightPipeRenderer = rightPipe.GetComponent<Renderer>();
        rightPipeRenderer.sharedMaterial = pipeOff;

        ParticleSystem leftStream = CreateWaterStream("Left Water Stream", new Vector3(-2.35f, 3.12f, -2.25f), new Vector3(58f, 25f, 0f));
        ParticleSystem rightStream = CreateWaterStream("Right Water Stream", new Vector3(2.35f, 3.12f, -2.25f), new Vector3(58f, -25f, 0f));

        GameObject drainObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drainObj.name = "Final Bottom Drain";
        drainObj.transform.position = new Vector3(0f, -5.02f, -2.1f);
        drainObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        drainObj.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
        Renderer drainRenderer = drainObj.GetComponent<Renderer>();
        drainRenderer.sharedMaterial = drainClosed;

        drainObj.AddComponent<GravityWaterStreamEffect>();
        drain = drainObj.AddComponent<DrainController>();
        drain.leftInflowParticles = leftStream;
        drain.rightInflowParticles = rightStream;
        drain.leftPipeRenderer = leftPipeRenderer;
        drain.rightPipeRenderer = rightPipeRenderer;
        drain.pipeOnMaterial = pipeOn;
        drain.pipeOffMaterial = pipeOff;
        drain.finalDrainRenderer = drainRenderer;
        drain.drainClosedMaterial = drainClosed;
        drain.drainOpenMaterial = drainOpen;

        GameObject matchedObj = new GameObject("Matched And Path Drain Particles");
        matchedObj.transform.position = new Vector3(0f, -2f, -2.7f);
        ParticleSystem matchedPs = matchedObj.AddComponent<ParticleSystem>();
        ConfigureMatchedParticles(matchedPs);
        drain.matchedDrainParticles = matchedPs;

        GameObject finalObj = new GameObject("Final Drain Particles");
        finalObj.transform.position = new Vector3(0f, -4.75f, -2.7f);
        ParticleSystem finalPs = finalObj.AddComponent<ParticleSystem>();
        ConfigureFinalParticles(finalPs);
        finalPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        drain.finalDrainParticles = finalPs;

        return water;
    }

    private static void CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        box.transform.SetParent(parent);
    }

    private static ParticleSystem CreateWaterStream(string name, Vector3 position, Vector3 rotation)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(rotation);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 1.05f;
        main.startSpeed = 3.0f;
        main.startSize = 0.13f;
        main.gravityModifier = 0.35f;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 95;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.08f;

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CreateMaterial("Water Particle V30", new Color(0.1f, 0.85f, 1f));

        return ps;
    }

    private static void ConfigureMatchedParticles(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.55f;
        main.startSpeed = 2.5f;
        main.startSize = 0.08f;
        main.gravityModifier = 0.7f;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.15f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CreateMaterial("Water Particle V30", new Color(0.1f, 0.85f, 1f));
    }

    private static void ConfigureFinalParticles(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.8f;
        main.startSpeed = 2.0f;
        main.startSize = 0.1f;
        main.gravityModifier = 0.5f;
        main.loop = true;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 75;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.35f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CreateMaterial("Water Particle V30", new Color(0.1f, 0.85f, 1f));
    }

    private static FloodCharacterController CreatePerson(Material calm, Material worried, Material panic)
    {
        GameObject person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        person.name = "Anxious Person";
        person.transform.position = new Vector3(-1.55f, 2.55f, -2.2f);
        person.transform.localScale = new Vector3(0.38f, 0.55f, 0.38f);
        Renderer body = person.GetComponent<Renderer>();
        body.sharedMaterial = calm;

        GameObject face = new GameObject("Face Text");
        face.transform.SetParent(person.transform);
        face.transform.localPosition = new Vector3(0f, 0.28f, -0.42f);
        face.transform.localScale = Vector3.one * 0.16f;

        TextMesh text = face.AddComponent<TextMesh>();
        text.text = ":|";
        text.characterSize = 0.34f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.black;

        face.AddComponent<BillboardToCamera>();

        FloodCharacterController controller = person.AddComponent<FloodCharacterController>();
        controller.expressionText = text;
        controller.bodyRenderer = body;
        controller.calmMaterial = calm;
        controller.worriedMaterial = worried;
        controller.panicMaterial = panic;
        // controller.areaCenter = new Vector3(-0.8f, 2.55f, -2.2f);
        // controller.areaSize = new Vector2(3.3f, 0.9f);

        return controller;
    }

    private static Canvas CreateCanvas()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();

        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            Component inputModule = eventSystem.AddComponent(inputSystemModuleType);
            System.Reflection.MethodInfo assignDefaultActions =
                inputSystemModuleType.GetMethod("AssignDefaultActions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (assignDefaultActions != null)
                assignDefaultActions.Invoke(inputModule, null);
        }
        else
        {
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("Main UI Canvas V30");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Text CreateTopBadgeText(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject panel = new GameObject(name + " Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.03f, 0.04f, 0.06f, 0.72f);

        Text text = panel.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return text;
    }

    private static void CreateSmallLabel(Transform parent, string name, string value, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.8f, 0.95f, 1f, 1f);
    }

    private static void CreateBottomGuide(Transform parent)
    {
        GameObject obj = new GameObject("Bottom Drag Guide");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 45);
        rect.sizeDelta = new Vector2(820, 60);

        Text text = obj.AddComponent<Text>();
        text.text = "빈칸을 가장자리에서 아래 배수구까지 연결하세요";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private static ClearPopupUI CreatePopup(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "Clear/GameOver Popup", new Vector2(0.5f, 0.5f), new Vector2(760, 520));
        ClearPopupUI popup = panel.AddComponent<ClearPopupUI>();
        popup.root = panel;

        popup.titleText = CreateCenteredText(panel.transform, "Title", "Stage Clear!", new Vector2(0, 155), new Vector2(680, 90), 48);
        popup.bodyText = CreateCenteredText(panel.transform, "Body", "물이 모두 빠졌습니다.", new Vector2(0, 55), new Vector2(680, 90), 30);

        popup.levelSelectButton = CreateButton(panel.transform, "Level Select Button", "단계 선택", new Vector2(-170, -135), new Vector2(220, 90));
        popup.nextLevelButton = CreateButton(panel.transform, "Next Button", "다음 단계", new Vector2(90, -135), new Vector2(220, 90));
        popup.retryButton = CreateButton(panel.transform, "Retry Button", "다시하기", new Vector2(170, -135), new Vector2(220, 90));

        return popup;
    }

    private static LevelSelectUI CreateLevelSelect(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "Level Select Panel", new Vector2(0.5f, 0.5f), new Vector2(820, 560));
        LevelSelectUI ui = panel.AddComponent<LevelSelectUI>();
        ui.root = panel;

        CreateCenteredText(panel.transform, "Title", "단계 선택", new Vector2(0, 190), new Vector2(650, 80), 48);

        ui.levelButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            float x = -300 + i * 150;
            ui.levelButtons[i] = CreateButton(panel.transform, $"Stage {i + 1} Button", $"Stage {i + 1}", new Vector2(x, -35), new Vector2(130, 130));
        }

        return ui;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.04f, 0.06f, 0.09f, 0.9f);

        return panel;
    }

    private static Text CreateCenteredText(Transform parent, string name, string textValue, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.text = textValue;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.9f, 1f);

        Button button = obj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }
}
#endif
