using UnityEngine;

public class OpenCellWaterVisual : MonoBehaviour
{
    public float pulseScale = 0.035f;
    public float waveSpeed = 7.0f;
    public float bobAmount = 0.025f;

    private Vector3 baseLocalScale;
    private Vector3 basePosition;
    private float seed;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
        basePosition = transform.position;
        seed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        baseLocalScale = transform.localScale;
        basePosition = transform.position;
    }

    private void Update()
    {
        float t = Time.time * waveSpeed + seed;
        float pulse = Mathf.Sin(t) * pulseScale;

        transform.localScale = new Vector3(
            baseLocalScale.x + pulse,
            baseLocalScale.y + Mathf.Sin(t * 1.3f) * pulseScale * 0.55f,
            baseLocalScale.z
        );

        Vector3 pos = basePosition;
        pos.y += Mathf.Sin(t * 1.6f) * bobAmount;
        transform.position = pos;
    }

    public static Material CreateWaterCellMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Runtime Missing Floor Cell Water Material";
        mat.color = new Color(0.04f, 0.72f, 1f, 0.86f);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.04f, 0.72f, 1f, 0.86f));

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.98f);

        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", 0.98f);

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
