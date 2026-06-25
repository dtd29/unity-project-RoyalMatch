using UnityEngine;

public class WaterFlowColumn : MonoBehaviour
{
    public Transform visual;
    public float lifeTime = 0.8f;
    public float swayAmount = 0.035f;
    public float widthPulse = 0.035f;
    public float flowSpeed = 9f;

    private float elapsed;
    private Vector3 baseScale;
    private Vector3 basePosition;
    private Renderer cachedRenderer;

    private void Awake()
    {
        if (visual == null)
            visual = transform;

        baseScale = visual.localScale;
        basePosition = visual.position;
        cachedRenderer = visual.GetComponent<Renderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(elapsed / lifeTime);

        float wave = Mathf.Sin(Time.time * flowSpeed + transform.GetInstanceID() * 0.017f);
        float pulse = Mathf.Sin(Time.time * flowSpeed * 1.7f) * widthPulse;

        visual.localScale = new Vector3(
            Mathf.Max(0.025f, baseScale.x + pulse),
            baseScale.y,
            baseScale.z
        );

        visual.position = basePosition + new Vector3(wave * swayAmount, 0f, 0f);

        if (cachedRenderer != null)
        {
            Color c = cachedRenderer.material.color;
            c.a = Mathf.Lerp(0.72f, 0f, normalized);
            cachedRenderer.material.color = c;

            if (cachedRenderer.material.HasProperty("_BaseColor"))
                cachedRenderer.material.SetColor("_BaseColor", c);
        }

        if (elapsed >= lifeTime)
            Destroy(gameObject);
    }

    public static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Runtime Flowing Water Column Material";

        Color color = new Color(0.08f, 0.72f, 1f, 0.72f);
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

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
}
