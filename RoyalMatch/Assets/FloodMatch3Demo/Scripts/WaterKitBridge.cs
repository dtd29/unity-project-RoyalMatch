using System.Collections.Generic;
using UnityEngine;

public class WaterKitBridge : MonoBehaviour
{
    [Header("Assign after importing Game 2D Water Kit")]
    [Tooltip("Game 2D Water Kit의 Water 프리팹을 여기에 넣으세요. 비워두면 임시 물 오브젝트가 생성됩니다.")]
    public GameObject upperWaterPrefab;

    [Tooltip("V26부터 사용하지 않습니다. 물줄기/폭포를 새로 만들지 않습니다.")]
    public GameObject waterfallPrefab;

    [Tooltip("V26부터 사용하지 않습니다. 칸마다 물을 만들지 않습니다.")]
    public GameObject cellWaterPrefab;

    [Header("Scene")]
    public Transform waterRoot;
    public BoardManager board;
    public WaterController legacyWaterController;

    [Header("V26 Single Water Body")]
    [Tooltip("켜면 상단 물 오브젝트 하나만 사용합니다. 하단 물 복제, 칸별 물, waterfall 생성은 하지 않습니다.")]
    public bool useSingleWaterBodyOnly = true;

    [Tooltip("물이 열린 칸을 따라 아래로 내려가는 속도입니다.")]
    public float waterFallDownSpeed = 1.15f;

    [Tooltip("블럭이 다시 막히면 물의 아래쪽 경계가 위로 올라오는 속도입니다.")]
    public float waterRetractSpeed = 2.3f;

    [Tooltip("물 오브젝트의 Z 위치입니다. 블럭/마스크 뒤, 배경보다 앞에 있어야 합니다.")]
    public float waterZ = -1.72f;

    [Tooltip("WaterKit 렌더러를 블럭보다 뒤에 그리기 위한 sortingOrder입니다.")]
    public int waterSortingOrder = -100;

    [Tooltip("WaterKit 물 렌더러를 강제로 뒤쪽 sortingOrder로 보정합니다.")]
    public bool forceWaterRenderBehindBlocks = true;

    [Tooltip("물이 들어갈 수 있는 가장 위쪽 기준선입니다.")]
    public float waterDefaultBottomY = 0.42f;

    [Tooltip("물이 내려갈 수 있는 가장 아래쪽 기준선입니다.")]
    public float waterLowestBottomY = -6;

    [Tooltip("열린 칸보다 살짝 더 아래까지 물이 내려가게 하는 값입니다.")]
    public float openCellVerticalPadding = 0.06f;

    [Tooltip("BoardManager가 자동으로 넣어주는 퍼즐 칸 간격입니다.")]
    public float boardCellSpacing = 0.64f;

    [Tooltip("켜면 열린 칸의 중심이 아니라 실제 칸 바닥 경계선까지 물을 내립니다.")]
    public bool flowToOpenCellBottomEdge = true;

    [Tooltip("최종 배수구 경로가 열리면 물이 아래 배수구까지 강제로 내려가게 합니다.")]
    public bool extendWaterToDrainPathOnClear = true;

    [Tooltip("켜면 worldPath의 가장 아래 좌표가 아니라 퍼즐판 최하단 waterLowestBottomY까지 물을 내립니다.")]
    public bool forceFinalWaterToBoardBottom = true;

    [Tooltip("클리어 경로를 따라 내려갈 때 추가로 더 내려가는 값입니다.")]
    public float finalDrainPathExtraDepth = 0.35f;

    [Tooltip("클리어 경로가 열린 뒤 물이 아래로 내려가는 속도입니다.")]
    public float finalDrainFallDownSpeed = 6.0f;

    [Header("V25 Board Bounds")]
    public bool autoFitPrefabBounds = true;
    public bool createWaterClipFrame = false    ;
    public float mapInnerWidth = 3.95f;
    public float mapInnerHeight = 4.65f;
    public Vector3 mapInnerCenter = new Vector3(0f, -0.03f, -2.16f);
    public float clipFrameZ = -1.72f;

    [Header("Water Area")]
    public Vector3 upperWaterPosition = new Vector3(0f, 1.72f, -2.74f);
    public Vector2 upperWaterSize = new Vector2(3.95f, 0.35f);
    public float upperWaterMinHeight = 0.18f;
    public float upperWaterMaxHeight = 3.05f;
    public float upperWaterBottomY = 0.42f;

    [Header("Deprecated")]
    public float waterfallZOffset = -0.50f;
    public float waterfallWidth = 0.28f;
    public float waterfallLifetime = 0.75f;
    public Vector3 cellWaterOffset = new Vector3(0f, 0f, -0.48f);
    public Vector2 cellWaterSize = new Vector2(0.54f, 0.54f);

    [Header("Fallback Materials")]
    public Material fallbackWaterMaterial;
    public Material fallbackWaterfallMaterial;
    public Material clipFrameMaterial;

    private GameObject upperWaterInstance;
    private Transform clipFrameRoot;

    private float currentFlood01;
    private float currentWaterBottomY;
    private float targetWaterBottomY;
    private bool hasOpenCell;
    private bool finalDrainPathOpened;
    private float finalDrainTargetBottomY;

    private void Awake()
    {
        EnsureRoot();
        EnsureMaterials();
        EnsureClipFrame();
        EnsureUpperWater();

        currentWaterBottomY = waterDefaultBottomY;
        targetWaterBottomY = waterDefaultBottomY;
    }

    private void Start()
    {
        if (board == null)
            board = FindFirstObjectByType<BoardManager>();

        if (legacyWaterController == null)
            legacyWaterController = FindFirstObjectByType<WaterController>();

        HideLegacyWaterVisual();
        EnsureClipFrame();
    }

    private void Update()
    {
        UpdateSingleWaterBodyMotion();
    }

    public void ResetBridge()
    {
        EnsureRoot();
        EnsureMaterials();
        EnsureClipFrame();
        EnsureUpperWater();
        HideLegacyWaterVisual();

        currentFlood01 = 0f;
        hasOpenCell = false;
        finalDrainPathOpened = false;
        finalDrainTargetBottomY = waterDefaultBottomY;
        currentWaterBottomY = waterDefaultBottomY;
        targetWaterBottomY = waterDefaultBottomY;

        SetFlood01(0f);
        ApplySingleWaterBody();
    }

    public void SetFlood01(float flood01)
    {
        currentFlood01 = Mathf.Clamp01(flood01);
        EnsureUpperWater();
        EnsureClipFrame();
        ApplySingleWaterBody();
    }

    public float GetUpperWaterTopY()
    {
        float visibleHeight = Mathf.Lerp(upperWaterMinHeight, upperWaterMaxHeight, currentFlood01);
        return upperWaterBottomY + visibleHeight;
    }

    public void SpawnFlowAtHole(Vector3 holeWorldPosition)
    {
        // V26:
        // 더 이상 waterfall이나 별도 물 오브젝트를 생성하지 않는다.
        // 물은 upperWaterInstance 하나만 사용하고, SetCellWaterByGrid가 아래 경계를 갱신한다.
    }

    public void SetCellWater(Vector2Int cell, Vector3 worldPosition, bool active)
    {
        // V26:
        // 칸마다 물을 만들지 않는다.
    }

    public void SetCellWaterByGrid(bool[,] openCells)
    {
        if (openCells == null)
            return;

        UpdateSingleWaterTargetFromGrid(openCells);
    }



    public void SpawnFlowPath(List<Vector3> worldPath)
    {
        // V29:
        // 경로 좌표가 중간까지만 들어오더라도 클리어가 발생했다면
        // 물은 무조건 퍼즐판 최하단 배수구까지 내려가야 한다.
        // 물/폭포 오브젝트는 새로 만들지 않고, 하나의 WaterKit Single Shared Water Body만 확장한다.
        if (!extendWaterToDrainPathOnClear)
            return;

        float target;

        if (forceFinalWaterToBoardBottom)
        {
            target = waterLowestBottomY - finalDrainPathExtraDepth;
        }
        else
        {
            if (worldPath == null || worldPath.Count == 0)
                target = waterLowestBottomY - finalDrainPathExtraDepth;
            else
            {
                float deepest = waterDefaultBottomY;

                foreach (Vector3 p in worldPath)
                    deepest = Mathf.Min(deepest, p.y - finalDrainPathExtraDepth);

                target = deepest;
            }
        }

        finalDrainPathOpened = true;
        finalDrainTargetBottomY = Mathf.Clamp(target, waterLowestBottomY - finalDrainPathExtraDepth, waterDefaultBottomY);
        targetWaterBottomY = finalDrainTargetBottomY;

        // 즉시 한 번 적용해서 클리어 순간에 목표가 반영되게 한다.
        ApplySingleWaterBody();

        Debug.Log($"WaterKitBridge V29: 최종 배수구까지 물 비주얼 확장 bottomY={targetWaterBottomY:0.00}, boardBottom={waterLowestBottomY:0.00}");
    }

    private void UpdateSingleWaterTargetFromGrid(bool[,] openCells)
    {
        if (finalDrainPathOpened)
        {
            targetWaterBottomY = finalDrainTargetBottomY;
            return;
        }

        if (board == null)
            board = FindFirstObjectByType<BoardManager>();

        int width = openCells.GetLength(0);
        int height = openCells.GetLength(1);

        hasOpenCell = false;

        float deepestOpenY = waterDefaultBottomY;
        Vector2Int deepestCell = new Vector2Int(-1, -1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!openCells[x, y])
                    continue;

                hasOpenCell = true;

                if (board == null)
                    continue;

                Vector3 world = board.BoardToWorld(x, y);

                // 중요:
                // 열린 칸의 중심이 아니라, 그 칸의 바닥보다 더 아래까지 물을 내린다.
                float cellBottomY = world.y - boardCellSpacing * 0.5f;
                float targetY = cellBottomY - openCellVerticalPadding;

                if (targetY < deepestOpenY)
                {
                    deepestOpenY = targetY;
                    deepestCell = new Vector2Int(x, y);
                }
            }
        }

        if (!hasOpenCell)
        {
            targetWaterBottomY = waterDefaultBottomY;
        }
        else
        {
            // 중요:
            // clamp 때문에 3번째 칸 근처에서 막힐 수 있으니 아래쪽 여유를 더 준다.
            float minBottom = waterLowestBottomY - 1.0f;
            targetWaterBottomY = Mathf.Clamp(deepestOpenY, minBottom, waterDefaultBottomY);
        }

        Debug.Log(
            $"[Water Debug] deepestCell={deepestCell}, " +
            $"deepestOpenY={deepestOpenY:0.00}, " +
            $"targetWaterBottomY={targetWaterBottomY:0.00}, " +
            $"waterLowestBottomY={waterLowestBottomY:0.00}"
        );
    }

    private void UpdateSingleWaterBodyMotion()
    {
        float speed = targetWaterBottomY < currentWaterBottomY
            ? (finalDrainPathOpened ? finalDrainFallDownSpeed : waterFallDownSpeed)
            : waterRetractSpeed;
        currentWaterBottomY = Mathf.MoveTowards(currentWaterBottomY, targetWaterBottomY, speed * Time.deltaTime);
        ApplySingleWaterBody();
    }

    private void ApplySingleWaterBody()
    {
        if (upperWaterInstance == null)
            return;

        float topY = GetUpperWaterTopY();

        // 수위가 낮아져서 아래쪽보다 낮아지면 최소 두께만 유지한다.
        float bottomY = Mathf.Min(currentWaterBottomY, topY - 0.04f);
        float height = Mathf.Max(0.04f, topY - bottomY);

        Debug.Log(
            $"[Water Apply] topY={topY:0.00}, " +
            $"bottomY={bottomY:0.00}, " +
            $"height={height:0.00}, " +
            $"currentWaterBottomY={currentWaterBottomY:0.00}, " +
            $"targetWaterBottomY={targetWaterBottomY:0.00}"
        );

        upperWaterInstance.transform.position = new Vector3(
            mapInnerCenter.x,
            bottomY + height * 0.5f,
            waterZ
        );

        Vector3 targetSize = new Vector3(mapInnerWidth, height, 0.12f);

        if (autoFitPrefabBounds)
            FitObjectRendererBoundsToSize(upperWaterInstance, targetSize);
        else
            upperWaterInstance.transform.localScale = targetSize;

        ConfigureWaterRenderers(upperWaterInstance);
    }

    private void ConfigureWaterRenderers(GameObject obj)
    {
        if (!forceWaterRenderBehindBlocks || obj == null)
            return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingOrder = waterSortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void EnsureRoot()
    {
        if (waterRoot != null)
            return;

        GameObject root = GameObject.Find("WaterKit Bridge Runtime Root");
        if (root == null)
            root = new GameObject("WaterKit Bridge Runtime Root");

        waterRoot = root.transform;
    }

    private void EnsureUpperWater()
    {
        if (upperWaterInstance != null)
            return;

        upperWaterInstance = CreateObjectFromPrefabOrFallback(
            upperWaterPrefab,
            "Single Water Body Placeholder",
            PrimitiveType.Cube,
            fallbackWaterMaterial
        );

        upperWaterInstance.name = "WaterKit Single Shared Water Body";
        upperWaterInstance.transform.SetParent(waterRoot, true);
        ConfigureWaterRenderers(upperWaterInstance);

        WaterKitPlaceholderAnimator animator = upperWaterInstance.GetComponent<WaterKitPlaceholderAnimator>();
        if (animator == null)
            animator = upperWaterInstance.AddComponent<WaterKitPlaceholderAnimator>();

        animator.destroyOnFinish = false;
        animator.pulseWidth = 0.006f;
        animator.alphaFrom = 0.70f;
        animator.alphaTo = 0.70f;

        ApplySingleWaterBody();
    }

    private GameObject CreateObjectFromPrefabOrFallback(GameObject prefab, string fallbackName, PrimitiveType fallbackPrimitive, Material fallbackMaterial)
    {
        GameObject obj;

        if (prefab != null)
        {
            obj = Instantiate(prefab);
            obj.name = prefab.name + " Runtime Instance";
            return obj;
        }

        obj = GameObject.CreatePrimitive(fallbackPrimitive);
        obj.name = fallbackName;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = fallbackMaterial;

        return obj;
    }

    private void FitObjectRendererBoundsToSize(GameObject obj, Vector3 targetSize)
    {
        if (obj == null)
            return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            obj.transform.localScale = targetSize;
            return;
        }

        obj.transform.localScale = Vector3.one;

        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            obj.transform.localScale = targetSize;
            return;
        }

        Vector3 size = bounds.size;

        float sx = size.x <= 0.0001f ? 1f : targetSize.x / size.x;
        float sy = size.y <= 0.0001f ? 1f : targetSize.y / size.y;
        float sz = size.z <= 0.0001f ? 1f : targetSize.z / size.z;

        if (float.IsInfinity(sz) || float.IsNaN(sz) || sz > 10f)
            sz = 1f;

        obj.transform.localScale = new Vector3(sx, sy, sz);

        WaterKitPlaceholderAnimator animator = obj.GetComponent<WaterKitPlaceholderAnimator>();
        if (animator != null)
            animator.ResetBaseScale();
    }

    private void EnsureClipFrame()
    {
        if (!createWaterClipFrame)
        {
            if (clipFrameRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(clipFrameRoot.gameObject);
                else
                    DestroyImmediate(clipFrameRoot.gameObject);

                clipFrameRoot = null;
            }

            GameObject existing = GameObject.Find("Water Visual Clip Frame V30");

            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing);
                else
                    DestroyImmediate(existing);
            }

            return;
        }

        if (clipFrameRoot != null)
            return;

        EnsureMaterials();

        GameObject root = new GameObject("Water Visual Clip Frame V30");
        root.transform.SetParent(waterRoot != null ? waterRoot : null, true);
        clipFrameRoot = root.transform;

        float outerWidth = mapInnerWidth + 4.0f;
        float outerHeight = mapInnerHeight + 4.0f;
        // float sideThickness = 2.0f;
        // float capThickness = 2.0f;

        //CreateClipBlock("Left Water Mask", new Vector3(mapInnerCenter.x - mapInnerWidth * 0.5f - sideThickness * 0.5f, mapInnerCenter.y, clipFrameZ), new Vector3(sideThickness, outerHeight, 0.12f));
        //CreateClipBlock("Right Water Mask", new Vector3(mapInnerCenter.x + mapInnerWidth * 0.5f + sideThickness * 0.5f, mapInnerCenter.y, clipFrameZ), new Vector3(sideThickness, outerHeight, 0.12f));
        //CreateClipBlock("Top Water Mask", new Vector3(mapInnerCenter.x, mapInnerCenter.y + mapInnerHeight * 0.5f + capThickness * 0.5f, clipFrameZ), new Vector3(outerWidth, capThickness, 0.12f));
        //CreateClipBlock("Bottom Water Mask", new Vector3(mapInnerCenter.x, mapInnerCenter.y - mapInnerHeight * 0.5f - capThickness * 0.5f, clipFrameZ), new Vector3(outerWidth, capThickness, 0.12f));
    }

    private void CreateClipBlock(string name, Vector3 position, Vector3 scale)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(clipFrameRoot, true);
        block.transform.position = position;
        block.transform.localScale = scale;

        Collider collider = block.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = clipFrameMaterial;
    }

    private void EnsureMaterials()
    {
        if (fallbackWaterMaterial == null)
            fallbackWaterMaterial = CreateWaterMaterial("Fallback Single Water Material", new Color(0.04f, 0.64f, 1f, 0.72f));

        if (fallbackWaterfallMaterial == null)
            fallbackWaterfallMaterial = CreateWaterMaterial("Deprecated Waterfall Material", new Color(0.15f, 0.85f, 1f, 0.68f));

        if (clipFrameMaterial == null)
            clipFrameMaterial = CreateOpaqueMaterial("Water Clip Frame Material", new Color(0.54f, 0.20f, 0.28f, 1f));
    }

    private Material CreateWaterMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.95f);

        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", 0.95f);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private Material CreateOpaqueMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        return mat;
    }

    private void HideLegacyWaterVisual()
    {
        if (legacyWaterController == null)
            return;

        if (legacyWaterController.waterFill != null)
        {
            Renderer renderer = legacyWaterController.waterFill.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        if (legacyWaterController.waterSurfaceLine != null)
        {
            Renderer lineRenderer = legacyWaterController.waterSurfaceLine.GetComponent<Renderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            legacyWaterController.waterSurfaceLine.gameObject.SetActive(false);
        }

        if (legacyWaterController.wavyWaterMesh != null)
            legacyWaterController.wavyWaterMesh.gameObject.SetActive(false);
    }
}
