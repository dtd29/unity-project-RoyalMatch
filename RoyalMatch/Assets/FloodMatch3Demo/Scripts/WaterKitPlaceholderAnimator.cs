using UnityEngine;

public class WaterKitPlaceholderAnimator : MonoBehaviour
{
    public bool destroyOnFinish;
    public float lifeTime = 1.0f;
    public float pulseWidth = 0.02f;
    public float waveSpeed = 7.5f;
    public float alphaFrom = 0.7f;
    public float alphaTo = 0.7f;

    private float elapsed;
    private Vector3 baseScale;
    private Renderer cachedRenderer;
    private Material runtimeMaterial;
    private float seed;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        baseScale = transform.localScale;
        cachedRenderer = GetComponentInChildren<Renderer>();
        seed = Random.Range(0f, 100f);

        if (cachedRenderer != null && cachedRenderer.sharedMaterial != null)
        {
            runtimeMaterial = new Material(cachedRenderer.sharedMaterial);
            cachedRenderer.sharedMaterial = runtimeMaterial;
        }
    }

    private void OnEnable()
    {
        elapsed = 0f;
        baseScale = transform.localScale;
    }

    public void ResetBaseScale()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = Time.time * waveSpeed + seed;
        float pulse = Mathf.Sin(t) * pulseWidth;

        transform.localScale = new Vector3(
            Mathf.Max(0.02f, baseScale.x + pulse),
            baseScale.y,
            baseScale.z
        );

        ApplyAlphaSafely();

        if (destroyOnFinish && elapsed >= lifeTime)
            Destroy(gameObject);
    }

    private void ApplyAlphaSafely()
    {
        if (runtimeMaterial == null)
            return;

        float normalized = lifeTime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifeTime);
        float alpha = Mathf.Lerp(alphaFrom, alphaTo, normalized);

        // Game2DWaterKit/URP/Unlit/Water 셰이더는 _Color가 없을 수 있다.
        // material.color는 내부적으로 _Color를 찾기 때문에 절대 직접 호출하지 않는다.
        if (runtimeMaterial.HasProperty(BaseColorId))
        {
            Color color = runtimeMaterial.GetColor(BaseColorId);
            color.a = alpha;
            runtimeMaterial.SetColor(BaseColorId, color);
            return;
        }

        if (runtimeMaterial.HasProperty(ColorId))
        {
            Color color = runtimeMaterial.GetColor(ColorId);
            color.a = alpha;
            runtimeMaterial.SetColor(ColorId, color);
            return;
        }

        // 일부 커스텀 셰이더는 색상 프로퍼티가 아예 없을 수 있다.
        // 이 경우 알파 조절을 건너뛰어 오류를 막는다.
    }
}
