using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Scene")]
    public Transform boardRoot;
    public GameObject breakEffectPrefab;

    [Header("Piece Visual Prefabs")]
    public GameObject redPieceVisualPrefab;
    public GameObject yellowPieceVisualPrefab;
    public GameObject bluePieceVisualPrefab;

    [Header("Editor Placed Masks")]
    public Transform editorSolidMaskRoot;
    public bool useEditorPlacedMasks = true;
    public bool createMasksInEditorAutomatically = true;
    public int editorMaskWidth = 6;
    public int editorMaskHeight = 6;
    public float editorMaskSpacing = 0.64f;
    public float editorBottomMaskHeight = 0.72f;
    public float editorBottomMaskYOffset = -0.34f;

    [Header("Materials")]
    public Material redMaterial;
    public Material yellowMaterial;
    public Material blueMaterial;
    public Material tileBackMaterial;
    public Material blockSideMaterial;
    public Material blockEdgeMaterial;
    public Material blockHighlightMaterial;
    public Material solidWaterMaskMaterial;
    public Material boardWallMaterial;
    public Material holeMaterial;
    public Material pathMaterial;

    [Header("Water Simulation")]
    public GridWaterSimulator waterSimulator;
    public WaterKitBridge waterKitBridge;

    [Header("V24 Solid Water Mask")]
    [Tooltip("켜면 마스크가 보이지 않지만 뒤쪽 물은 가립니다. 일반 투명색보다 이 방식이 맞습니다.")]
    public bool useInvisibleDepthMask = true;

    [Tooltip("마스크 가로/세로 크기입니다. 1.0이면 칸 경계선 기준, 더 키우면 빈틈을 더 많이 덮습니다.")]
    public float solidMaskScaleMultiplier = 1.00f;

    [Tooltip("마스크 Z 위치입니다. 물보다 앞, 블럭보다 뒤에 있어야 합니다.")]
    public float solidMaskZOffset = -0.09f;

    [Tooltip("마스크 두께입니다.")]
    public float solidMaskThickness = 0.050f;

    [Header("V25 Board Water Walls")]
    [Tooltip("퍼즐판 좌우/하단에 실제 벽을 생성해서 벽 밖으로 물이 보이지 않게 합니다.")]
    public bool createBoardWaterWalls = false;

    [Tooltip("벽 두께입니다.")]
    public float boardWallThickness = 0.16f;

    [Tooltip("벽의 Z 위치입니다. 물보다 앞, 블럭과 비슷한 위치에 있어야 합니다.")]
    public float boardWallZOffset = -0.32f;

    [Tooltip("벽의 Z 두께입니다.")]
    public float boardWallDepth = 0.22f;

    [Tooltip("물 가로폭이 벽 안쪽보다 살짝 작아지도록 하는 여백입니다.")]
    public float boardWaterInnerPadding = 0.035f;

    [Tooltip("벽이 퍼즐판보다 위아래로 조금 더 길어지는 정도입니다.")]
    public float boardWallExtraHeight = 0.08f;

    [Header("Runtime")]
    public bool inputLocked;

    private LevelData level;
    private Match3Tile[,] grid;
    private bool[,] openedHoles;
    private GameObject[,] holeObjects;
    private GameObject[,] openCellWaterObjects;
    private GameObject[,] solidWaterMaskObjects;
    private GameObject bottomBoardWaterMaskObject;
    private GameObject boardWaterWallsRoot;
    private Material openCellWaterMaterial;

    private bool busy;
    private bool drainPathAlreadyOpened;

    private int totalInitialPieces;
    private int clearedPieces;

    public int TotalInitialPieces => totalInitialPieces;
    public int ClearedPieces => clearedPieces;


    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (!createMasksInEditorAutomatically)
            return;

        if (boardRoot == null)
            return;

        // 에디터에서 값을 수정했을 때도 마스크가 바로 보이도록 갱신한다.
        CreateEditorPlacedMasksPreview();
    }

    [ContextMenu("Create / Refresh Editor Solid Water Masks")]
    public void CreateEditorPlacedMasksPreview()
    {
        if (Application.isPlaying)
            return;

        if (boardRoot == null)
            return;

        EnsureEditorMaskRoot();

        int width = Mathf.Max(1, editorMaskWidth);
        int height = Mathf.Max(1, editorMaskHeight);
        float spacing = Mathf.Max(0.05f, editorMaskSpacing);

        float xOffset = (width - 1) * spacing * 0.5f;
        float yOffset = (height - 1) * spacing * 0.5f;

        Material maskMat = blockSideMaterial != null ? blockSideMaterial : solidWaterMaskMaterial;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string maskName = $"Solid Cell Water Mask {x},{y}";
                Transform existing = editorSolidMaskRoot.Find(maskName);
                GameObject mask = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);

                mask.name = maskName;
                mask.transform.SetParent(editorSolidMaskRoot, true);

                Vector3 cellWorldPos = boardRoot.position + new Vector3(
                    x * spacing - xOffset,
                    y * spacing - yOffset,
                    0f
                );

                mask.transform.position = cellWorldPos + new Vector3(0f, 0f, solidMaskZOffset);
                mask.transform.localScale = new Vector3(
                    spacing * solidMaskScaleMultiplier,
                    spacing * solidMaskScaleMultiplier,
                    solidMaskThickness
                );

                Renderer renderer = mask.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = maskMat;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                Collider collider = mask.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
            }
        }

//        CreateOrRefreshBottomBoardMask(width, height, spacing, maskMat);
    }

    private GameObject GetPieceVisualPrefab(PieceShape shape)
    {
        switch (shape)
        {
            case PieceShape.RedSphere:
                return redPieceVisualPrefab;

            case PieceShape.YellowCube:
                return yellowPieceVisualPrefab;

            case PieceShape.BlueCylinder:
                return bluePieceVisualPrefab;

            default:
                return null;
        }
    }

    private void CreateOrRefreshBottomBoardMask(int width, int height, float spacing, Material maskMat)
    {
        if (editorSolidMaskRoot == null || boardRoot == null)
            return;

        Transform existing = editorSolidMaskRoot.Find("Bottom Board Water Mask");
        GameObject bottomMask = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomMask.name = "Bottom Board Water Mask";
        bottomMask.transform.SetParent(editorSolidMaskRoot, true);

        float xOffset = (width - 1) * spacing * 0.5f;
        float yOffset = (height - 1) * spacing * 0.5f;
        float boardWidth = width * spacing;
        float bottomY = boardRoot.position.y - yOffset - spacing * 0.5f;

        bottomMask.transform.position = new Vector3(
            boardRoot.position.x,
            bottomY + editorBottomMaskYOffset,
            boardRoot.position.z + solidMaskZOffset
        );

        bottomMask.transform.localScale = new Vector3(
            boardWidth + 0.20f,
            editorBottomMaskHeight,
            Mathf.Max(0.055f, solidMaskThickness)
        );

        Renderer renderer = bottomMask.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = boardWallMaterial != null ? boardWallMaterial : maskMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = bottomMask.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);
    }

    public void SpawnBoard(LevelData newLevel)
    {
        level = newLevel;
        inputLocked = false;
        busy = false;
        drainPathAlreadyOpened = false;

        if (boardRoot == null)
        {
            GameObject root = new GameObject("Runtime Puzzle Board Root");
            root.transform.position = new Vector3(0f, -2.25f, -2.25f);
            boardRoot = root.transform;
        }

        EnsureMaterials();

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);

        grid = new Match3Tile[level.width, level.height];
        openedHoles = new bool[level.width, level.height];
        holeObjects = new GameObject[level.width, level.height];
        openCellWaterObjects = new GameObject[level.width, level.height];
        solidWaterMaskObjects = new GameObject[level.width, level.height];

        totalInitialPieces = 0;
        clearedPieces = 0;

        CreateHoleBackground();
        CreateSolidWaterMasks();
        SetupBottomBoardWaterMask();
        CreateBoardWaterWalls();
        CreateOpenCellWaterObjects();
        SyncWaterKitBridgeBoundsToBoard();

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                if (TryGetInitialShape(x, y, out PieceShape shape))
                {
                    CreatePiece(x, y, shape, BoardToWorld(x, y));
                    totalInitialPieces++;
                }
                else
                {
                    grid[x, y] = null;
                }
            }
        }

        RefreshHoleStatesFromGrid();
        UpdateWaterSimulatorSolids();
        FloodGameManager.Instance?.OnBoardProgressChanged(clearedPieces, totalInitialPieces);

        Debug.Log($"BoardManager V30: 퍼즐 생성 완료 {level.width} x {level.height} = {totalInitialPieces}개 / 제거 후 낙하 / 리필 없음");
    }

    private bool TryGetInitialShape(int x, int y, out PieceShape shape)
    {
        shape = PieceShape.RedSphere;

        if (level != null && level.useManualLayout && !string.IsNullOrWhiteSpace(level.manualLayout))
        {
            string[] rows = level.manualLayout
                .Replace("\r", "")
                .Split('\n');

            // manualLayout의 첫 줄은 화면 위쪽,
            // y = 0은 보드 아래쪽이므로 뒤집어서 읽는다.
            int rowIndex = rows.Length - 1 - y;

            if (rowIndex < 0 || rowIndex >= rows.Length)
                return false;

            string row = rows[rowIndex].Replace(" ", "").Replace("\t", "");

            if (x < 0 || x >= row.Length)
                return false;

            char c = char.ToUpperInvariant(row[x]);

            if (c == '.')
                return false;

            return TryCharToShape(c, out shape);
        }

        shape = ChooseInitialShape(x, y);
        return true;
    }

    private bool TryCharToShape(char c, out PieceShape shape)
    {
        shape = PieceShape.RedSphere;

        if (c == 'R')
        {
            shape = PieceShape.RedSphere;
            return true;
        }

        if (c == 'Y')
        {
            shape = PieceShape.YellowCube;
            return true;
        }

        if (c == 'B')
        {
            shape = PieceShape.BlueCylinder;
            return true;
        }

        return false;
    }

    public void LockBoard(bool locked)
    {
        inputLocked = locked;
    }

    public void TryMovePieceByDirection(Match3Tile piece, Vector2Int direction)
    {
        if (inputLocked || busy || piece == null)
            return;

        int targetX = piece.x + direction.x;
        int targetY = piece.y + direction.y;

        if (!IsInside(targetX, targetY))
            return;

        Match3Tile target = grid[targetX, targetY];
        if (target == null)
            return;

        StartCoroutine(SwapAndResolve(piece, target));
    }

    private IEnumerator SwapAndResolve(Match3Tile a, Match3Tile b)
    {
        busy = true;
        inputLocked = true;

        yield return SwapPieces(a, b, 0.14f);

        List<Match3Tile> matches = FindAllMatches();

        if (matches.Count == 0)
        {
            yield return SwapPieces(a, b, 0.14f);
        }
        else
        {
            yield return ResolveMatches(matches);
        }

        if (!drainPathAlreadyOpened)
            inputLocked = false;

        busy = false;
    }

    private IEnumerator ResolveMatches(List<Match3Tile> firstMatches)
    {
        List<Match3Tile> matches = firstMatches;

        while (matches.Count > 0)
        {
            int clearCount = matches.Count;
            List<Vector3> clearedPositions = new List<Vector3>();

            foreach (Match3Tile tile in matches)
            {
                if (tile == null)
                    continue;

                Vector3 pos = tile.transform.position;
                clearedPositions.Add(pos);

                if (breakEffectPrefab != null)
                {
                    GameObject fx = Instantiate(breakEffectPrefab, pos + Vector3.back * 0.35f, Quaternion.identity);
                    Destroy(fx, 2f);
                }

                grid[tile.x, tile.y] = null;
                clearedPieces++;

                Destroy(tile.gameObject);
            }

            FloodGameManager.Instance?.OnPiecesCleared(clearCount, clearedPositions);
            FloodGameManager.Instance?.OnBoardProgressChanged(clearedPieces, totalInitialPieces);

            yield return new WaitForSeconds(0.16f);

            // V10 변경점:
            // 아래쪽 퍼즐이 사라지면 위쪽 퍼즐이 아래로 떨어진다.
            // 단, 새 퍼즐은 리필하지 않는다.
            yield return CollapseColumnsWithoutRefill();

            RefreshHoleStatesFromGrid();
            UpdateWaterSimulatorSolids();

            List<Vector2Int> drainPath = FindDrainPath();
            if (drainPath != null && drainPath.Count > 0)
            {
                drainPathAlreadyOpened = true;
                inputLocked = true;
                HighlightDrainPath(drainPath);
                FloodGameManager.Instance?.OnDrainPathOpened(drainPath, GetWorldPath(drainPath));
                yield break;
            }

            matches = FindAllMatches();

            if (matches.Count > 0)
                yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator CollapseColumnsWithoutRefill()
    {
        for (int x = 0; x < level.width; x++)
        {
            List<Match3Tile> columnPieces = new List<Match3Tile>();

            for (int y = 0; y < level.height; y++)
            {
                if (grid[x, y] != null)
                    columnPieces.Add(grid[x, y]);

                grid[x, y] = null;
            }

            for (int y = 0; y < columnPieces.Count; y++)
            {
                Match3Tile tile = columnPieces[y];

                if (tile == null)
                    continue;

                grid[x, y] = tile;
                tile.SetBoardPosition(x, y);

                Vector3 target = BoardToWorld(x, y);
                StartCoroutine(MoveTo(tile.transform, target, 0.20f));
            }
        }

        yield return new WaitForSeconds(0.23f);
    }




    private Match3Tile CreatePiece(int x, int y, PieceShape shape, Vector3 position)
    {
        float s = level != null ? level.pieceSpacing : 0.68f;

        GameObject root = new GameObject($"Floor Block {x},{y} {shape}");
        root.transform.SetParent(boardRoot);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // 클릭/매치 판정용 콜라이더는 루트가 담당
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, -0.08f);
        collider.size = new Vector3(s * 1.08f, s * 1.08f, 0.64f);

        Match3Tile tile = root.AddComponent<Match3Tile>();

        GameObject visualPrefab = GetPieceVisualPrefab(shape);

        // 프리팹이 있으면 프리팹 외형 사용
        if (visualPrefab != null)
        {
            GameObject visual = Instantiate(visualPrefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // 프리팹 안에 Collider가 있으면 클릭 판정이 꼬일 수 있으니 제거
            Collider[] childColliders = visual.GetComponentsInChildren<Collider>();
            for (int i = 0; i < childColliders.Length; i++)
            {
                Destroy(childColliders[i]);
            }
        }
        // 프리팹이 비어 있으면 기존 방식으로 자동 생성
        else
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "No Gap Thick Floor Block Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            body.transform.localScale = new Vector3(s * 1.14f, s * 1.14f, 0.28f);
            body.GetComponent<Renderer>().sharedMaterial = blockSideMaterial;
            Destroy(body.GetComponent<Collider>());

            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "No Gap Raised Block Edge";
            edge.transform.SetParent(root.transform);
            edge.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            edge.transform.localScale = new Vector3(s * 1.10f, s * 1.10f, 0.085f);
            edge.GetComponent<Renderer>().sharedMaterial = blockEdgeMaterial;
            Destroy(edge.GetComponent<Collider>());

            GameObject coloredFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coloredFace.name = "No Gap Colored Floor Surface";
            coloredFace.transform.SetParent(root.transform);
            coloredFace.transform.localPosition = new Vector3(0f, 0f, -0.235f);
            coloredFace.transform.localScale = new Vector3(s * 1.04f, s * 1.04f, 0.075f);
            coloredFace.GetComponent<Renderer>().sharedMaterial = GetMaterial(shape);
            Destroy(coloredFace.GetComponent<Collider>());

            GameObject shine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shine.name = "Block Top Highlight";
            shine.transform.SetParent(root.transform);
            shine.transform.localPosition = new Vector3(-s * 0.18f, s * 0.22f, -0.285f);
            shine.transform.localScale = new Vector3(s * 0.34f, s * 0.075f, 0.026f);
            shine.GetComponent<Renderer>().sharedMaterial = blockHighlightMaterial;
            Destroy(shine.GetComponent<Collider>());

            PrimitiveType primitiveType =
                shape == PieceShape.RedSphere ? PrimitiveType.Sphere :
                shape == PieceShape.YellowCube ? PrimitiveType.Cube :
                PrimitiveType.Cylinder;

            GameObject icon = GameObject.CreatePrimitive(primitiveType);
            icon.name = "Embedded Shape Icon";
            icon.transform.SetParent(root.transform);
            icon.transform.localPosition = new Vector3(0f, -0.02f, -0.35f);

            if (shape == PieceShape.RedSphere)
            {
                icon.transform.localScale = new Vector3(s * 0.45f, s * 0.45f, 0.105f);
            }
            else if (shape == PieceShape.YellowCube)
            {
                icon.transform.localScale = new Vector3(s * 0.39f, s * 0.39f, 0.085f);
                icon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            else
            {
                icon.transform.localScale = new Vector3(s * 0.36f, 0.085f, s * 0.36f);
                icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            icon.GetComponent<Renderer>().sharedMaterial = blockHighlightMaterial;
            Destroy(icon.GetComponent<Collider>());
        }

        tile.Init(x, y, shape);
        grid[x, y] = tile;

        return tile;
    }
    private void CreateHoleBackground()
    {
        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hole.name = $"Drain Cell {x},{y}";
                hole.transform.SetParent(boardRoot);
                hole.transform.position = BoardToWorld(x, y) + new Vector3(0f, 0f, 0.16f);
                hole.transform.localScale = new Vector3(0.68f, 0.68f, 0.06f);
                hole.GetComponent<Renderer>().sharedMaterial = holeMaterial;
                Destroy(hole.GetComponent<Collider>());
                holeObjects[x, y] = hole;
            }
        }
    }



    private void CreateSolidWaterMasks()
    {
        float s = level != null ? level.pieceSpacing : editorMaskSpacing;

        if (solidWaterMaskObjects == null)
            solidWaterMaskObjects = new GameObject[level.width, level.height];

        EnsureEditorMaskRoot();

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                GameObject mask = null;

                // 에디터에 이미 존재하는 마스크가 있으면 런타임에서도 그대로 사용한다.
                if (useEditorPlacedMasks && editorSolidMaskRoot != null)
                {
                    Transform existing = editorSolidMaskRoot.Find($"Solid Cell Water Mask {x},{y}");
                    if (existing != null)
                        mask = existing.gameObject;
                }

                if (mask == null)
                {
                    mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mask.name = $"Solid Cell Water Mask {x},{y}";

                    if (editorSolidMaskRoot != null)
                        mask.transform.SetParent(editorSolidMaskRoot, true);
                    else
                        mask.transform.SetParent(boardRoot, true);
                }

                mask.transform.position = BoardToWorld(x, y) + new Vector3(0f, 0f, solidMaskZOffset);
                mask.transform.localScale = new Vector3(
                    s * solidMaskScaleMultiplier,
                    s * solidMaskScaleMultiplier,
                    solidMaskThickness
                );

                Renderer renderer = mask.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = solidWaterMaskMaterial != null
                        ? solidWaterMaskMaterial
                        : blockSideMaterial;

                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                Collider collider = mask.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                solidWaterMaskObjects[x, y] = mask;
            }
        }
    }

    private void EnsureEditorMaskRoot()
    {
        if (editorSolidMaskRoot != null)
            return;

        GameObject root = GameObject.Find("Editor Solid Water Masks");

        if (root == null)
            root = new GameObject("Editor Solid Water Masks");

        editorSolidMaskRoot = root.transform;
    }

    private void SetupBottomBoardWaterMask()
    {
        if (level == null || boardRoot == null)
            return;

        EnsureEditorMaskRoot();

        if (bottomBoardWaterMaskObject == null && editorSolidMaskRoot != null)
        {
            Transform existing = editorSolidMaskRoot.Find("Bottom Board Water Mask");
            if (existing != null)
                bottomBoardWaterMaskObject = existing.gameObject;
        }

        if (bottomBoardWaterMaskObject == null)
        {
            bottomBoardWaterMaskObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomBoardWaterMaskObject.name = "Bottom Board Water Mask";

            if (editorSolidMaskRoot != null)
                bottomBoardWaterMaskObject.transform.SetParent(editorSolidMaskRoot, true);
            else
                bottomBoardWaterMaskObject.transform.SetParent(boardRoot, true);
        }

        float s = level.pieceSpacing;
        float leftEdge = BoardToWorld(0, 0).x - s * 0.5f;
        float rightEdge = BoardToWorld(level.width - 1, 0).x + s * 0.5f;
        float bottomEdge = BoardToWorld(0, 0).y - s * 0.5f;
        float width = rightEdge - leftEdge;

        bottomBoardWaterMaskObject.transform.position = new Vector3(
            (leftEdge + rightEdge) * 0.5f,
            bottomEdge + editorBottomMaskYOffset,
            boardRoot.position.z + solidMaskZOffset
        );

        bottomBoardWaterMaskObject.transform.localScale = new Vector3(
            width + 0.20f,
            editorBottomMaskHeight,
            Mathf.Max(0.055f, solidMaskThickness)
        );

        Renderer renderer = bottomBoardWaterMaskObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = boardWallMaterial != null
                ? boardWallMaterial
                : blockSideMaterial;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = bottomBoardWaterMaskObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        bottomBoardWaterMaskObject.SetActive(true);
    }

    private void CreateBoardWaterWalls()
    {
        if (!createBoardWaterWalls || level == null || boardRoot == null)
            return;

        float s = level.pieceSpacing;

        float leftEdge = BoardToWorld(0, 0).x - s * 0.5f;
        float rightEdge = BoardToWorld(level.width - 1, 0).x + s * 0.5f;
        float bottomEdge = BoardToWorld(0, 0).y - s * 0.5f;
        float topEdge = BoardToWorld(0, level.height - 1).y + s * 0.5f;

        float centerY = (topEdge + bottomEdge) * 0.5f;
        float height = (topEdge - bottomEdge) + boardWallExtraHeight * 2f;
        float innerWidth = rightEdge - leftEdge;

        boardWaterWallsRoot = new GameObject("Puzzle Board Water Tight Walls");
        boardWaterWallsRoot.transform.SetParent(boardRoot, true);

        CreateBoardWall(
            "Left Puzzle Water Wall",
            new Vector3(leftEdge - boardWallThickness * 0.5f, centerY, boardRoot.position.z + boardWallZOffset),
            new Vector3(boardWallThickness, height, boardWallDepth)
        );

        CreateBoardWall(
            "Right Puzzle Water Wall",
            new Vector3(rightEdge + boardWallThickness * 0.5f, centerY, boardRoot.position.z + boardWallZOffset),
            new Vector3(boardWallThickness, height, boardWallDepth)
        );

        CreateBoardWall(
            "Bottom Puzzle Water Wall",
            new Vector3((leftEdge + rightEdge) * 0.5f, bottomEdge - boardWallThickness * 0.5f, boardRoot.position.z + boardWallZOffset),
            new Vector3(innerWidth + boardWallThickness * 2f, boardWallThickness, boardWallDepth)
        );
    }

    private void CreateBoardWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(boardWaterWallsRoot.transform, true);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = boardWallMaterial != null ? boardWallMaterial : blockSideMaterial;

        Collider collider = wall.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void SyncWaterKitBridgeBoundsToBoard()
    {
        EnsureWaterKitBridge();

        if (waterKitBridge == null || level == null || boardRoot == null)
            return;

        float s = level.pieceSpacing;

        waterKitBridge.boardCellSpacing = s;

        float leftEdge = BoardToWorld(0, 0).x - s * 0.5f;
        float rightEdge = BoardToWorld(level.width - 1, 0).x + s * 0.5f;
        float bottomEdge = BoardToWorld(0, 0).y - s * 0.5f;
        float topEdge = BoardToWorld(0, level.height - 1).y + s * 0.5f;

        float innerWidth = Mathf.Max(0.1f, (rightEdge - leftEdge) - boardWaterInnerPadding * 2f);
        float innerHeight = Mathf.Max(0.1f, topEdge - bottomEdge);

        Vector3 oldCenter = waterKitBridge.mapInnerCenter;
        waterKitBridge.mapInnerWidth = innerWidth;
        waterKitBridge.mapInnerHeight = innerHeight;
        waterKitBridge.mapInnerCenter = new Vector3((leftEdge + rightEdge) * 0.5f, (bottomEdge + topEdge) * 0.5f, oldCenter.z);

        waterKitBridge.mapInnerWidth = innerWidth;
        waterKitBridge.waterDefaultBottomY = topEdge + 0.06f;
        waterKitBridge.waterLowestBottomY = bottomEdge - 0.35f;
        waterKitBridge.upperWaterBottomY = topEdge + 0.06f;
        waterKitBridge.upperWaterSize = new Vector2(innerWidth, waterKitBridge.upperWaterSize.y);

        // 벽과 물의 폭이 안 맞는 경우를 막기 위해 클립 영역도 같은 폭으로 동기화한다.
        Debug.Log($"BoardManager V30: WaterKit 영역 자동 보정 width={innerWidth:0.00}, bottom={bottomEdge:0.00}, top={topEdge:0.00}");
    }

    private void CreateOpenCellWaterObjects()
    {
        if (openCellWaterMaterial == null)
            openCellWaterMaterial = OpenCellWaterVisual.CreateWaterCellMaterial();

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                GameObject waterCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterCell.name = $"Water Filling Missing Floor Cell {x},{y}";
                waterCell.transform.SetParent(boardRoot);

                // 퍼즐 블럭이 사라진 자리를 물이 차지하는 것처럼 크게 표시한다.
                // 바닥이 사라진 구멍 안으로 물이 들어온다는 느낌을 위해 퍼즐 표면 근처에 배치.
                waterCell.transform.position = BoardToWorld(x, y) + new Vector3(0f, 0f, -0.34f);
                waterCell.transform.localScale = new Vector3(0.62f, 0.62f, 0.08f);

                Renderer renderer = waterCell.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = openCellWaterMaterial;

                Collider collider = waterCell.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                ConnectedWaterToHole connectedWater = waterCell.AddComponent<ConnectedWaterToHole>();
                connectedWater.holeWaterSurface = waterCell.transform;
                connectedWater.columnWidth = 0.26f;
                connectedWater.columnDepth = 0.075f;
                connectedWater.zOffset = 0.0f;
                waterCell.SetActive(false);

                openCellWaterObjects[x, y] = waterCell;
            }
        }
    }


    private void RefreshHoleStatesFromGrid()
    {
        if (openedHoles == null)
            return;

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                // V23 핵심:
                // 물이 보여야 하는 기준은 "현재 그 칸에 블럭이 없는가"이다.
                // 위 블럭이 내려와서 다시 그 칸을 차지하면 즉시 마스크가 다시 켜진다.
                bool currentlyOpen = grid[x, y] == null;
                openedHoles[x, y] = currentlyOpen;

                if (holeObjects != null && holeObjects[x, y] != null)
                {
                    holeObjects[x, y].SetActive(!currentlyOpen);

                    if (!currentlyOpen)
                    {
                        holeObjects[x, y].name = $"Blocked Drain Cell {x},{y}";
                        holeObjects[x, y].transform.localScale = new Vector3(0.68f, 0.68f, 0.06f);
                        holeObjects[x, y].GetComponent<Renderer>().sharedMaterial = holeMaterial;
                    }
                }

                // 칸별 물 오브젝트는 사용하지 않는다. 물은 뒤쪽의 큰 물 덩어리 하나만 사용한다.
                if (openCellWaterObjects != null && openCellWaterObjects[x, y] != null)
                    openCellWaterObjects[x, y].SetActive(false);

                // 현재 블럭이 있으면 마스크 ON, 현재 블럭이 없으면 마스크 OFF.
                // 이게 사용자가 원한 방식: 다시 블럭이 내려오면 다시 막힘.
                if (solidWaterMaskObjects != null && solidWaterMaskObjects[x, y] != null)
                    solidWaterMaskObjects[x, y].SetActive(!currentlyOpen);
            }
        }
    }

    private void HighlightDrainPath(List<Vector2Int> path)
    {
        foreach (Vector2Int p in path)
        {
            if (!IsInside(p.x, p.y))
                continue;

            if (holeObjects[p.x, p.y] != null)
                holeObjects[p.x, p.y].GetComponent<Renderer>().sharedMaterial = pathMaterial;
        }
    }



    private void EnsureWaterKitBridge()
    {
        if (waterKitBridge == null)
            waterKitBridge = FindFirstObjectByType<WaterKitBridge>();
    }

    private void EnsureWaterSimulator()
    {
        if (waterSimulator == null)
            waterSimulator = FindFirstObjectByType<GridWaterSimulator>();

        if (waterSimulator == null)
        {
            GameObject simulatorObject = new GameObject("Grid Based Real Water Simulator");
            waterSimulator = simulatorObject.AddComponent<GridWaterSimulator>();
        }

        if (level != null)
            waterSimulator.Setup(this, level.width, level.height, level.pieceSpacing);
    }

    private void UpdateWaterSimulatorSolids()
    {
        if (level == null || grid == null)
            return;

        EnsureWaterSimulator();

        bool[,] solids = new bool[level.width, level.height];

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
                solids[x, y] = grid[x, y] != null;
        }

        if (waterSimulator != null)
            waterSimulator.SetSolids(solids);

        SyncWaterKitBridgeBoundsToBoard();
        EnsureWaterKitBridge();

        if (waterKitBridge != null)
        {
            bool[,] openCells = new bool[level.width, level.height];

            for (int yy = 0; yy < level.height; yy++)
            {
                for (int xx = 0; xx < level.width; xx++)
                    openCells[xx, yy] = !solids[xx, yy];
            }

            waterKitBridge.SetCellWaterByGrid(openCells);
        }
    }

    private List<Vector2Int> FindDrainPath()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[level.width, level.height];
        Vector2Int[,] parent = new Vector2Int[level.width, level.height];

        void TryAddStart(int x, int y)
        {
            if (!IsInside(x, y))
                return;

            if (visited[x, y])
                return;

            if (!openedHoles[x, y])
                return;

            Vector2Int start = new Vector2Int(x, y);
            queue.Enqueue(start);
            visited[x, y] = true;
            parent[x, y] = new Vector2Int(-1, -1);
        }

        for (int x = 0; x < level.width; x++)
            TryAddStart(x, level.height - 1);

        for (int y = 0; y < level.height; y++)
        {
            TryAddStart(0, y);
            TryAddStart(level.width - 1, y);
        }

        if (queue.Count == 0)
            return null;

        int centerLeft = Mathf.Max(0, level.width / 2 - 1);
        int centerRight = Mathf.Min(level.width - 1, level.width / 2);

        Vector2Int found = new Vector2Int(-1, -1);

        Vector2Int[] dirs =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            bool reachedBottomDrain =
                current.y == 0 &&
                current.x >= centerLeft &&
                current.x <= centerRight;

            bool reachedAnyBottom = current.y == 0;

            if (reachedBottomDrain || reachedAnyBottom)
            {
                found = current;
                break;
            }

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;

                if (!IsInside(next.x, next.y))
                    continue;

                if (visited[next.x, next.y])
                    continue;

                if (!openedHoles[next.x, next.y])
                    continue;

                visited[next.x, next.y] = true;
                parent[next.x, next.y] = current;
                queue.Enqueue(next);
            }
        }

        if (found.x < 0)
            return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int p = found;

        while (p.x >= 0)
        {
            path.Add(p);
            p = parent[p.x, p.y];
        }

        path.Reverse();

        Debug.Log("BoardManager V30: 배수구 경로 열림! Path Length = " + path.Count);
        return path;
    }

    private List<Vector3> GetWorldPath(List<Vector2Int> path)
    {
        List<Vector3> result = new List<Vector3>();

        foreach (Vector2Int p in path)
            result.Add(BoardToWorld(p.x, p.y));

        return result;
    }

    private PieceShape ChooseInitialShape(int x, int y)
    {
        int value = (x + y * 2 + (y % 2)) % 3;
        return (PieceShape)value;
    }

    private List<Match3Tile> FindAllMatches()
    {
        HashSet<Match3Tile> result = new HashSet<Match3Tile>();

        for (int y = 0; y < level.height; y++)
        {
            int start = 0;
            while (start < level.width)
            {
                Match3Tile tile = grid[start, y];
                int len = 1;

                while (start + len < level.width &&
                       tile != null &&
                       grid[start + len, y] != null &&
                       grid[start + len, y].shape == tile.shape)
                    len++;

                if (tile != null && len >= 3)
                {
                    for (int i = 0; i < len; i++)
                        result.Add(grid[start + i, y]);
                }

                start += len;
            }
        }

        for (int x = 0; x < level.width; x++)
        {
            int start = 0;
            while (start < level.height)
            {
                Match3Tile tile = grid[x, start];
                int len = 1;

                while (start + len < level.height &&
                       tile != null &&
                       grid[x, start + len] != null &&
                       grid[x, start + len].shape == tile.shape)
                    len++;

                if (tile != null && len >= 3)
                {
                    for (int i = 0; i < len; i++)
                        result.Add(grid[x, start + i]);
                }

                start += len;
            }
        }

        return new List<Match3Tile>(result);
    }

    private IEnumerator SwapPieces(Match3Tile a, Match3Tile b, float duration)
    {
        int ax = a.x;
        int ay = a.y;
        int bx = b.x;
        int by = b.y;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        a.SetBoardPosition(bx, by);
        b.SetBoardPosition(ax, ay);

        Vector3 aTarget = BoardToWorld(bx, by);
        Vector3 bTarget = BoardToWorld(ax, ay);

        Vector3 aStart = a.transform.position;
        Vector3 bStart = b.transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            a.transform.position = Vector3.Lerp(aStart, aTarget, t);
            b.transform.position = Vector3.Lerp(bStart, bTarget, t);
            yield return null;
        }

        a.transform.position = aTarget;
        b.transform.position = bTarget;
    }

    private IEnumerator MoveTo(Transform target, Vector3 end, float duration)
    {
        if (target == null)
            yield break;

        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (target != null)
            target.position = end;
    }

    public Vector3 BoardToWorld(int x, int y)
    {
        float spacing = level.pieceSpacing;
        float xOffset = (level.width - 1) * spacing * 0.5f;
        float yOffset = (level.height - 1) * spacing * 0.5f;

        return boardRoot.position + new Vector3(x * spacing - xOffset, y * spacing - yOffset, 0f);
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < level.width && y < level.height;
    }

    private void EnsureMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (redMaterial == null)
        {
            redMaterial = new Material(shader);
            redMaterial.color = new Color(1f, 0.16f, 0.12f);
        }

        if (yellowMaterial == null)
        {
            yellowMaterial = new Material(shader);
            yellowMaterial.color = new Color(1f, 0.82f, 0.05f);
        }

        if (blueMaterial == null)
        {
            blueMaterial = new Material(shader);
            blueMaterial.color = new Color(0.08f, 0.42f, 1f);
        }

        if (tileBackMaterial == null)
        {
            tileBackMaterial = new Material(shader);
            tileBackMaterial.color = new Color(0.035f, 0.04f, 0.055f);
        }

        if (blockSideMaterial == null)
        {
            blockSideMaterial = new Material(shader);
            blockSideMaterial.color = new Color(0.018f, 0.022f, 0.032f);
        }

        if (blockEdgeMaterial == null)
        {
            blockEdgeMaterial = new Material(shader);
            blockEdgeMaterial.color = new Color(0.09f, 0.10f, 0.12f);
        }

        if (blockHighlightMaterial == null)
        {
            blockHighlightMaterial = new Material(shader);
            blockHighlightMaterial.color = new Color(1f, 1f, 1f, 0.92f);
        }

        if (solidWaterMaskMaterial == null)
        {
            Shader maskShader = Shader.Find("FloodMatch3/Invisible Depth Mask");

            if (useInvisibleDepthMask && maskShader != null)
            {
                solidWaterMaskMaterial = new Material(maskShader);
                solidWaterMaskMaterial.name = "Invisible Solid Water Mask Material";
            }
            else
            {
                solidWaterMaskMaterial = new Material(shader);
                solidWaterMaskMaterial.name = "Visible Solid Water Mask Fallback Material";

                Color c = new Color(0.03f, 0.035f, 0.045f, useInvisibleDepthMask ? 0f : 1f);

                if (solidWaterMaskMaterial.HasProperty("_BaseColor"))
                    solidWaterMaskMaterial.SetColor("_BaseColor", c);
                else if (solidWaterMaskMaterial.HasProperty("_Color"))
                    solidWaterMaskMaterial.SetColor("_Color", c);
            }
        }

        if (boardWallMaterial == null)
        {
            boardWallMaterial = new Material(shader);
            boardWallMaterial.name = "Puzzle Board Water Wall Material";
            boardWallMaterial.color = new Color(0.03f, 0.035f, 0.045f, 1f);
        }

        if (holeMaterial == null)
        {
            holeMaterial = new Material(shader);
            holeMaterial.color = new Color(0.005f, 0.008f, 0.012f);
        }

        if (pathMaterial == null)
        {
            pathMaterial = new Material(shader);
            pathMaterial.color = new Color(0.05f, 0.8f, 1f);
        }
    }

    private Material GetMaterial(PieceShape shape)
    {
        if (shape == PieceShape.RedSphere)
            return redMaterial;

        if (shape == PieceShape.YellowCube)
            return yellowMaterial;

        return blueMaterial;
    }
}
