using UnityEngine;

public class GridWaterSimulator : MonoBehaviour
{
    [Header("References")]
    public BoardManager board;
    public WaterController waterController;

    [Header("Simulation")]
    public int width;
    public int height;
    public float spacing = 0.72f;
    public float maxMass = 1.0f;
    public float sourceRate = 1.35f;
    public float downFlowPerSecond = 7.5f;
    public float sideFlowPerSecond = 2.8f;
    public float upwardOverflowPerSecond = 1.2f;
    public float bottomDrainPerSecond = 1.8f;

    [Header("Visual")]
    public bool showVisuals = false;
    public Material waterMaterial;
    public float visualWidthMultiplier = 0.86f;
    public float visualDepth = 0.085f;
    public float visualZOffset = -0.46f;
    public float visibleThreshold = 0.025f;

    private bool[,] solid;
    private float[,] mass;
    private Transform[,] visuals;
    private Renderer[,] visualRenderers;
    private Transform visualRoot;
    private bool initialized;

    public void Setup(BoardManager targetBoard, int boardWidth, int boardHeight, float boardSpacing)
    {
        board = targetBoard;
        width = boardWidth;
        height = boardHeight;
        spacing = boardSpacing;

        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();

        if (waterMaterial == null)
            waterMaterial = CreateWaterMaterial();

        solid = new bool[width, height];
        mass = new float[width, height];

        CreateVisuals();

        initialized = true;
    }

    public void SetSolids(bool[,] newSolid)
    {
        if (newSolid == null)
            return;

        if (!initialized || newSolid.GetLength(0) != width || newSolid.GetLength(1) != height)
            return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool becomesSolid = newSolid[x, y];

                if (becomesSolid && !solid[x, y] && mass[x, y] > 0f)
                    PushWaterOutOfSolidCell(x, y);

                solid[x, y] = becomesSolid;
            }
        }

        if (showVisuals)
            UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (!initialized || board == null)
            return;

        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();

        float dt = Time.fixedDeltaTime;

        AddWaterFromUpperReservoir(dt);

        // 여러 번 반복해서 한 프레임 안에서도 물이 아래로 더 자연스럽게 내려가게 한다.
        int iterations = 3;
        float step = dt / iterations;

        for (int i = 0; i < iterations; i++)
        {
            FlowDown(step);
            FlowSideways(step);
            FlowOverflowUp(step);
            DrainBottom(step);
        }

        if (showVisuals)
            UpdateVisuals();
    }

    private void AddWaterFromUpperReservoir(float dt)
    {
        if (waterController == null)
            return;

        float topY = waterController.CurrentTopY;

        // 물이 퍼즐 바닥선보다 위에 있을 때만 구멍으로 들어온다.
        if (topY <= waterController.bottomY + 0.08f)
            return;

        // 위쪽 행은 위 물과 직접 닿은 입구.
        int topRow = height - 1;
        int openings = 0;

        for (int x = 0; x < width; x++)
        {
            if (!solid[x, topRow])
                openings++;
        }

        // 좌우 가장자리도 물이 옆으로 흘러 들어갈 수 있는 입구.
        for (int y = 0; y < height; y++)
        {
            Vector3 leftPos = board.BoardToWorld(0, y);
            Vector3 rightPos = board.BoardToWorld(width - 1, y);

            if (leftPos.y <= topY && !solid[0, y])
                openings++;

            if (rightPos.y <= topY && !solid[width - 1, y])
                openings++;
        }

        if (openings <= 0)
            return;

        float addPerOpening = sourceRate * dt / openings;

        for (int x = 0; x < width; x++)
        {
            if (!solid[x, topRow])
                mass[x, topRow] = Mathf.Min(maxMass, mass[x, topRow] + addPerOpening);
        }

        for (int y = 0; y < height; y++)
        {
            Vector3 leftPos = board.BoardToWorld(0, y);
            Vector3 rightPos = board.BoardToWorld(width - 1, y);

            if (leftPos.y <= topY && !solid[0, y])
                mass[0, y] = Mathf.Min(maxMass, mass[0, y] + addPerOpening);

            if (rightPos.y <= topY && !solid[width - 1, y])
                mass[width - 1, y] = Mathf.Min(maxMass, mass[width - 1, y] + addPerOpening);
        }
    }

    private void FlowDown(float dt)
    {
        for (int y = 1; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (solid[x, y] || solid[x, y - 1])
                    continue;

                float available = mass[x, y];
                if (available <= 0f)
                    continue;

                float capacity = maxMass - mass[x, y - 1];
                if (capacity <= 0f)
                    continue;

                float flow = Mathf.Min(available, capacity, downFlowPerSecond * dt);
                mass[x, y] -= flow;
                mass[x, y - 1] += flow;
            }
        }
    }

    private void FlowSideways(float dt)
    {
        float[,] delta = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (solid[x, y] || mass[x, y] <= 0.01f)
                    continue;

                TrySideFlow(x, y, x - 1, y, dt, delta);
                TrySideFlow(x, y, x + 1, y, dt, delta);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                mass[x, y] = Mathf.Clamp(mass[x, y] + delta[x, y], 0f, maxMass * 1.15f);
            }
        }
    }

    private void TrySideFlow(int fromX, int fromY, int toX, int toY, float dt, float[,] delta)
    {
        if (toX < 0 || toX >= width || toY < 0 || toY >= height)
            return;

        if (solid[toX, toY])
            return;

        float difference = mass[fromX, fromY] - mass[toX, toY];

        if (difference <= 0.08f)
            return;

        float desired = difference * 0.35f;
        float capacity = maxMass - mass[toX, toY];
        float flow = Mathf.Min(desired, capacity, sideFlowPerSecond * dt, mass[fromX, fromY]);

        if (flow <= 0f)
            return;

        delta[fromX, fromY] -= flow;
        delta[toX, toY] += flow;
    }

    private void FlowOverflowUp(float dt)
    {
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (solid[x, y] || solid[x, y + 1])
                    continue;

                if (mass[x, y] <= maxMass)
                    continue;

                float overflow = mass[x, y] - maxMass;
                float capacity = maxMass - mass[x, y + 1];
                float flow = Mathf.Min(overflow, capacity, upwardOverflowPerSecond * dt);

                if (flow <= 0f)
                    continue;

                mass[x, y] -= flow;
                mass[x, y + 1] += flow;
            }
        }
    }

    private void DrainBottom(float dt)
    {
        // 최종 배수구 근처 바닥 행으로 물이 도달하면 조금씩 빠져나가게 한다.
        // 클리어 판정 자체는 BoardManager의 열린 경로 검사에서 한다.
        for (int x = 0; x < width; x++)
        {
            if (solid[x, 0])
                continue;

            if (mass[x, 0] <= 0f)
                continue;

            float drain = Mathf.Min(mass[x, 0], bottomDrainPerSecond * dt);
            mass[x, 0] -= drain;
        }
    }

    private void PushWaterOutOfSolidCell(int x, int y)
    {
        float amount = mass[x, y];
        mass[x, y] = 0f;

        // 블럭이 내려오면서 물이 눌리면 위쪽 빈칸으로 밀려나는 느낌.
        for (int yy = y + 1; yy < height; yy++)
        {
            if (!solid[x, yy])
            {
                float capacity = maxMass - mass[x, yy];
                float moved = Mathf.Min(amount, capacity);
                mass[x, yy] += moved;
                amount -= moved;

                if (amount <= 0f)
                    return;
            }
        }

        // 위로 못 가면 좌우로 퍼짐.
        for (int xx = 0; xx < width; xx++)
        {
            if (!solid[xx, y])
            {
                float capacity = maxMass - mass[xx, y];
                float moved = Mathf.Min(amount, capacity);
                mass[xx, y] += moved;
                amount -= moved;

                if (amount <= 0f)
                    return;
            }
        }
    }

    private void CreateVisuals()
    {
        if (visualRoot != null)
            Destroy(visualRoot.gameObject);

        GameObject root = new GameObject("Grid Water Simulator Visuals");
        root.transform.SetParent(transform, false);
        visualRoot = root.transform;

        visuals = new Transform[width, height];
        visualRenderers = new Renderer[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = $"Simulated Real Water Cell {x},{y}";
                cell.transform.SetParent(visualRoot, true);

                Collider collider = cell.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                Renderer renderer = cell.GetComponent<Renderer>();
                renderer.sharedMaterial = waterMaterial;

                visuals[x, y] = cell.transform;
                visualRenderers[x, y] = renderer;

                cell.SetActive(false);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (visuals == null || board == null)
            return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Transform visual = visuals[x, y];

                if (visual == null)
                    continue;

                bool show = !solid[x, y] && mass[x, y] > visibleThreshold;
                visual.gameObject.SetActive(show);

                if (!show)
                    continue;

                Vector3 center = board.BoardToWorld(x, y);
                float fill = Mathf.Clamp01(mass[x, y]);
                float cellHeight = spacing * fill;

                float bottom = center.y - spacing * 0.5f;
                float wave = Mathf.Sin(Time.time * 8f + x * 0.7f + y * 1.1f) * 0.018f;

                visual.position = new Vector3(
                    center.x,
                    bottom + cellHeight * 0.5f + wave,
                    center.z + visualZOffset
                );

                visual.localScale = new Vector3(
                    spacing * visualWidthMultiplier + Mathf.Sin(Time.time * 7f + x) * 0.015f,
                    Mathf.Max(0.03f, cellHeight),
                    visualDepth
                );
            }
        }
    }

    private Material CreateWaterMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Runtime Grid Simulated Water Material";

        Color color = new Color(0.02f, 0.64f, 1f, 0.82f);
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.96f);

        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", 0.96f);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

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
}
