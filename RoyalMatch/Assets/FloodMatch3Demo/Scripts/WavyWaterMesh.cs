using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WavyWaterMesh : MonoBehaviour
{
    [Header("Shape")]
    public float width = 5.55f;
    public float bottomY = 0.42f;
    public float height = 0.1f;
    public int xSegments = 36;
    public int ySegments = 8;

    [Header("Wave")]
    public float waveAmplitude = 0.10f;
    public float waveFrequency = 2.8f;
    public float waveSpeed = 3.2f;
    public float secondaryWaveAmplitude = 0.045f;
    public float secondaryWaveFrequency = 5.1f;
    public float secondaryWaveSpeed = -2.0f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private void Awake()
    {
        BuildMesh();
    }

    private void Update()
    {
        UpdateMesh();
    }

    public void SetHeight(float newHeight)
    {
        height = Mathf.Max(0.02f, newHeight);
        UpdateMesh();
    }

    public float GetAverageTopY()
    {
        return bottomY + height;
    }

    private void BuildMesh()
    {
        if (xSegments < 2)
            xSegments = 2;

        if (ySegments < 1)
            ySegments = 1;

        mesh = new Mesh();
        mesh.name = "Runtime Wavy Water Mesh";

        vertices = new Vector3[(xSegments + 1) * (ySegments + 1)];
        triangles = new int[xSegments * ySegments * 6];

        int ti = 0;

        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i = y * (xSegments + 1) + x;

                triangles[ti++] = i;
                triangles[ti++] = i + xSegments + 1;
                triangles[ti++] = i + 1;

                triangles[ti++] = i + 1;
                triangles[ti++] = i + xSegments + 1;
                triangles[ti++] = i + xSegments + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (mesh == null || vertices == null)
            return;

        float time = Application.isPlaying ? Time.time : 0f;
        float left = -width * 0.5f;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = y / (float)ySegments;

            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;
                float px = left + width * u;

                float topWave =
                    Mathf.Sin((u * Mathf.PI * 2f * waveFrequency) + time * waveSpeed) * waveAmplitude +
                    Mathf.Sin((u * Mathf.PI * 2f * secondaryWaveFrequency) + time * secondaryWaveSpeed) * secondaryWaveAmplitude;

                // 아래쪽은 고정, 위로 갈수록 출렁임이 강해지게 한다.
                float waveWeight = Mathf.SmoothStep(0f, 1f, v);
                float py = bottomY + height * v + topWave * waveWeight;

                int i = y * (xSegments + 1) + x;
                vertices[i] = new Vector3(px, py, 0f);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
