using System.Collections;
using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("Legacy Visible Water")]
    public Transform waterFill;
    public Transform waterSurfaceLine;

    [Header("3D Water")]
    public WavyWaterMesh wavyWaterMesh;
    public Material waterMeshMaterial;

    [Header("Flood Area")]
    public float bottomY = 0.42f;
    public float minHeight = 0.10f;
    public float maxHeight = 3.35f;

    [Header("Visual")]
    public bool hideLegacySurfaceLine = true;
    public float surfaceBobAmount = 0.06f;
    public float surfaceBobSpeed = 7f;

    private float currentFlood01;
    private float visualOffset;
    private float currentHeight;
    private Coroutine drainRoutine;
    private Coroutine pulseRoutine;

    public float CurrentTopY => bottomY + currentHeight;

    private void Awake()
    {
        EnsureWavyWater();
    }

    private void Update()
    {
        EnsureWavyWater();

        if (hideLegacySurfaceLine && waterSurfaceLine != null)
        {
            Renderer lineRenderer = waterSurfaceLine.GetComponent<Renderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            waterSurfaceLine.gameObject.SetActive(false);
        }

        if (!hideLegacySurfaceLine && waterSurfaceLine != null)
        {
            Vector3 scale = waterSurfaceLine.localScale;
            scale.x = 5.4f + Mathf.Sin(Time.time * surfaceBobSpeed) * 0.08f;
            waterSurfaceLine.localScale = scale;

            Vector3 pos = waterSurfaceLine.position;
            pos.y = CurrentTopY + Mathf.Sin(Time.time * surfaceBobSpeed * 1.25f) * surfaceBobAmount;
            waterSurfaceLine.position = pos;
        }
    }

    public void ResetWater()
    {
        EnsureWavyWater();

        if (drainRoutine != null)
            StopCoroutine(drainRoutine);

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        currentFlood01 = 0f;
        visualOffset = 0f;
        SetFlood01(0f);
    }

    public void SetFlood01(float flood01)
    {
        EnsureWavyWater();

        currentFlood01 = Mathf.Clamp01(flood01);

        float height = Mathf.Lerp(minHeight, maxHeight, currentFlood01) - visualOffset;
        height = Mathf.Max(minHeight, height);

        ApplyHeight(height);
    }

    public void PulseLowerWater()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(PulseLowerRoutine());
    }

    private IEnumerator PulseLowerRoutine()
    {
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            visualOffset = Mathf.Lerp(0.35f, 0f, elapsed / duration);
            SetFlood01(currentFlood01);
            yield return null;
        }

        visualOffset = 0f;
        pulseRoutine = null;
    }

    public void StartDrainAll(float duration = 1.6f)
    {
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);

        drainRoutine = StartCoroutine(DrainAllRoutine(duration));
    }

    private IEnumerator DrainAllRoutine(float duration)
    {
        float startHeight = currentHeight;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            ApplyHeight(Mathf.Lerp(minHeight, startHeight, t));
            yield return null;
        }

        ApplyHeight(minHeight);
        drainRoutine = null;
    }

    private void ApplyHeight(float height)
    {
        currentHeight = Mathf.Max(minHeight, height);

        if (wavyWaterMesh != null)
        {
            wavyWaterMesh.bottomY = bottomY;
            wavyWaterMesh.SetHeight(currentHeight);
        }

        // 기존 큐브 물은 충돌/백업용으로만 남기고 렌더러는 숨긴다.
        if (waterFill != null)
        {
            Vector3 scale = waterFill.localScale;
            scale.y = currentHeight;
            waterFill.localScale = scale;

            Vector3 pos = waterFill.position;
            pos.y = bottomY + currentHeight * 0.5f;
            waterFill.position = pos;

            Renderer r = waterFill.GetComponent<Renderer>();
            if (r != null)
                r.enabled = false;
        }

        if (hideLegacySurfaceLine && waterSurfaceLine != null)
        {
            Renderer lineRenderer = waterSurfaceLine.GetComponent<Renderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            waterSurfaceLine.gameObject.SetActive(false);
        }

        if (!hideLegacySurfaceLine && waterSurfaceLine != null)
        {
            Vector3 pos = waterSurfaceLine.position;
            pos.y = bottomY + currentHeight;
            waterSurfaceLine.position = pos;
        }
    }

    private void EnsureWavyWater()
    {
        if (wavyWaterMesh != null)
            return;

        float width = 5.55f;
        float z = -2.65f;
        float x = 0f;

        if (waterFill != null)
        {
            width = waterFill.localScale.x;
            z = waterFill.position.z;
            x = waterFill.position.x;
        }

        GameObject meshObject = new GameObject("Wavy 3D Water Mesh");
        meshObject.transform.position = new Vector3(x, 0f, z - 0.08f);

        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        MeshFilter filter = meshObject.AddComponent<MeshFilter>();

        if (waterMeshMaterial == null)
            waterMeshMaterial = CreateWaterMaterial();

        renderer.sharedMaterial = waterMeshMaterial;

        wavyWaterMesh = meshObject.AddComponent<WavyWaterMesh>();
        wavyWaterMesh.width = width;
        wavyWaterMesh.bottomY = bottomY;
        wavyWaterMesh.height = minHeight;
        wavyWaterMesh.waveAmplitude = 0.10f;
        wavyWaterMesh.secondaryWaveAmplitude = 0.045f;

        ApplyHeight(Mathf.Max(minHeight, currentHeight));
    }

    private Material CreateWaterMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Runtime Wavy 3D Water Material";
        mat.color = new Color(0.05f, 0.55f, 1f, 0.72f);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.05f, 0.55f, 1f, 0.72f));

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.92f);

        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", 0.92f);

        // URP/Standard 양쪽에서 최대한 투명하게 보이도록 설정
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
