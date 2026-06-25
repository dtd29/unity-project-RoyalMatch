using UnityEngine;

public class ConnectedWaterToHole : MonoBehaviour
{
    [Header("References")]
    public Transform holeWaterSurface;
    public Transform connectionColumn;
    public WaterController waterController;

    [Header("Visual")]
    public float columnWidth = 0.24f;
    public float columnDepth = 0.07f;
    public float zOffset = -0.02f;
    public float topOverlap = 0.10f;
    public float bottomOverlap = 0.08f;
    public float swayAmount = 0.025f;
    public float widthPulse = 0.025f;
    public float waveSpeed = 8f;

    private Vector3 surfaceBaseScale;
    private Vector3 surfaceBasePosition;
    private float seed;

    private void Awake()
    {
        if (holeWaterSurface == null)
            holeWaterSurface = transform;

        surfaceBaseScale = holeWaterSurface.localScale;
        surfaceBasePosition = holeWaterSurface.position;
        seed = Random.Range(0f, 100f);

        EnsureColumn();
    }

    private void OnEnable()
    {
        if (holeWaterSurface == null)
            holeWaterSurface = transform;

        surfaceBaseScale = holeWaterSurface.localScale;
        surfaceBasePosition = holeWaterSurface.position;

        EnsureColumn();

        if (connectionColumn != null)
            connectionColumn.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (connectionColumn != null)
            connectionColumn.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (waterController == null)
            waterController = FindFirstObjectByType<WaterController>();

        AnimateHoleSurface();
        UpdateConnectionColumn();
    }

    private void EnsureColumn()
    {
        if (connectionColumn != null)
            return;

        GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cube);
        column.name = "Connected Water From Upper Flood To Missing Floor";
        column.transform.SetParent(transform.parent, true);

        Collider collider = column.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = column.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = OpenCellWaterVisual.CreateWaterCellMaterial();

        connectionColumn = column.transform;
    }

    private void AnimateHoleSurface()
    {
        if (holeWaterSurface == null)
            return;

        float t = Time.time * waveSpeed + seed;
        float pulse = Mathf.Sin(t) * widthPulse;

        holeWaterSurface.localScale = new Vector3(
            surfaceBaseScale.x + pulse,
            surfaceBaseScale.y + Mathf.Sin(t * 1.3f) * widthPulse * 0.55f,
            surfaceBaseScale.z
        );

        Vector3 pos = surfaceBasePosition;
        pos.y += Mathf.Sin(t * 1.6f) * 0.025f;
        holeWaterSurface.position = pos;
    }

    private void UpdateConnectionColumn()
    {
        if (connectionColumn == null)
            return;

        if (waterController == null)
        {
            connectionColumn.gameObject.SetActive(false);
            return;
        }

        float topY = waterController.CurrentTopY + topOverlap;
        float bottomY = transform.position.y + bottomOverlap;

        if (topY <= bottomY + 0.05f)
        {
            connectionColumn.gameObject.SetActive(false);
            return;
        }

        connectionColumn.gameObject.SetActive(true);

        float height = topY - bottomY;
        float t = Time.time * waveSpeed + seed;
        float sway = Mathf.Sin(t) * swayAmount;
        float pulse = Mathf.Sin(t * 1.4f) * widthPulse;

        Vector3 pos = transform.position;
        pos.x += sway;
        pos.y = bottomY + height * 0.5f;
        pos.z += zOffset - 0.50f;

        connectionColumn.position = pos;
        connectionColumn.localScale = new Vector3(
            Mathf.Max(0.04f, columnWidth + pulse),
            height,
            columnDepth
        );
    }
}
