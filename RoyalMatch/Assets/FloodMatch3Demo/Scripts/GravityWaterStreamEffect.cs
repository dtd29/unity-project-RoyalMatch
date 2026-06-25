using System.Collections.Generic;
using UnityEngine;

public class GravityWaterStreamEffect : MonoBehaviour
{
    public Material waterParticleMaterial;
    public Material waterColumnMaterial;

    public float streamDuration = 0.75f;
    public float particleSize = 0.09f;
    public int burstCount = 28;

    public void PlayFlowToHole(Vector3 targetPosition, float waterTopY)
    {
        float startY = Mathf.Max(waterTopY, targetPosition.y + 0.75f);

        // V15: 연결된 실제 물 덩어리 표현은 GridWaterSimulator가 담당한다.
        // 여기서는 튀는 물방울만 보조로 사용한다.
        Vector3 start = new Vector3(targetPosition.x, startY, targetPosition.z - 0.72f);

        GameObject streamObject = new GameObject("Gravity Water Particles To Hole");
        streamObject.transform.position = start;
        streamObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ps = streamObject.AddComponent<ParticleSystem>();

        // AddComponent 직후 playOnAwake 때문에 재생 중일 수 있으므로 설정 전 완전 정지.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ConfigureParticleSystem(ps, targetPosition, startY);

        ps.Play();
        ps.Emit(burstCount);

        Destroy(streamObject, streamDuration + 1.25f);
    }

    public void PlayFlowPath(List<Vector3> pathPositions, float waterTopY)
    {
        if (pathPositions == null)
            return;

        for (int i = 0; i < pathPositions.Count; i++)
            PlayFlowToHole(pathPositions[i], waterTopY);
    }

    private void CreateFlowColumn(Vector3 targetPosition, float startY)
    {
        float endY = targetPosition.y + 0.12f;
        float height = Mathf.Max(0.18f, startY - endY);
        Vector3 center = new Vector3(targetPosition.x, endY + height * 0.5f, targetPosition.z - 0.50f);

        GameObject columnObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        columnObject.name = "Flowing Water Sheet Into Missing Floor";
        columnObject.transform.position = center;
        columnObject.transform.localScale = new Vector3(0.30f, height, 0.060f);

        Collider collider = columnObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = columnObject.GetComponent<Renderer>();

        if (waterColumnMaterial == null)
            waterColumnMaterial = WaterFlowColumn.CreateDefaultMaterial();

        renderer.sharedMaterial = waterColumnMaterial;

        WaterFlowColumn flow = columnObject.AddComponent<WaterFlowColumn>();
        flow.visual = columnObject.transform;
        flow.lifeTime = streamDuration + 0.2f;
        flow.swayAmount = 0.035f;
        flow.widthPulse = 0.025f;
    }

    private void ConfigureParticleSystem(ParticleSystem ps, Vector3 targetPosition, float startY)
    {
        if (ps == null)
            return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float fallDistance = Mathf.Max(0.5f, startY - targetPosition.y);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = streamDuration;
        main.startLifetime = Mathf.Clamp(fallDistance / 4.2f, 0.35f, 1.2f);
        main.startSpeed = Mathf.Clamp(fallDistance * 1.7f, 1.2f, 5.5f);
        main.startSize = particleSize;
        main.gravityModifier = 1.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 0.10f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var color = ps.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.05f, 0.55f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.65f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        if (waterParticleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            waterParticleMaterial = new Material(shader);
            waterParticleMaterial.name = "Runtime Gravity Water Particle Material";
            waterParticleMaterial.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        }

        renderer.sharedMaterial = waterParticleMaterial;
        renderer.sortingOrder = 10;
    }
}
